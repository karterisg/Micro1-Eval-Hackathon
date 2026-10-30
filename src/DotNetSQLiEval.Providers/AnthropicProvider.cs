using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetSQLiEval.Core.Models;

namespace DotNetSQLiEval.Providers;

public class AnthropicProvider
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public AnthropicProvider(string apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient { BaseAddress = new Uri("https://api.anthropic.com"), Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<ModelResponse> RunAsync(EvalTask task, RunConfig config, int trial)
    {
        var response = new ModelResponse
        {
            TaskId = task.Id,
            ModelId = config.ModelId,
            TrialNumber = trial,
            RunAt = DateTime.UtcNow
        };

        try
        {
            var body = new
            {
                model = config.ModelId,
                max_tokens = config.MaxTokens,
                temperature = config.Temperature,
                messages = new[] { new { role = "user", content = task.Prompt } }
            };

            var httpResponse = await _http.PostAsJsonAsync("/v1/messages", body);
            var json = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                response.Error = $"HTTP {(int)httpResponse.StatusCode}: {json}";
                Console.WriteLine($"    [API ERROR] {response.Error}");
                return response;
            }

            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            var usage = doc.RootElement.GetProperty("usage");
            response.InputTokens = usage.GetProperty("input_tokens").GetInt32();
            response.OutputTokens = usage.GetProperty("output_tokens").GetInt32();
            response.RawOutput = content;
        }
        catch (Exception ex)
        {
            response.Error = ex.Message;
            Console.WriteLine($"    [EXCEPTION] {ex.Message}");
        }

        return response;
    }
}
