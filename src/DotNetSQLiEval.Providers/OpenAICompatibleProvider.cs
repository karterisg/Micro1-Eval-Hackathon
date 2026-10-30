using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotNetSQLiEval.Core.Models;

namespace DotNetSQLiEval.Providers;

public class OpenAICompatibleProvider
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;

    public OpenAICompatibleProvider(string apiKey, string baseUrl, string model)
    {
        _baseUrl = baseUrl;
        _model = model;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<ModelResponse> RunAsync(EvalTask task, RunConfig config, int trial)
    {
        var body = new
        {
            model = _model,
            messages = new[] { new { role = "user", content = task.Prompt } },
            temperature = config.Temperature,
            max_tokens = config.MaxTokens
        };

        var json = JsonSerializer.Serialize(body);
        var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        try
        {
            var resp = await _http.SendAsync(request);
            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return new ModelResponse { TaskId = task.Id, TrialNumber = trial, Error = $"HTTP {resp.StatusCode}: {raw}" };

            using var doc = JsonDocument.Parse(raw);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            return new ModelResponse { TaskId = task.Id, TrialNumber = trial, RawOutput = content };
        }
        catch (Exception ex)
        {
            return new ModelResponse { TaskId = task.Id, TrialNumber = trial, Error = ex.Message };
        }
    }
}
