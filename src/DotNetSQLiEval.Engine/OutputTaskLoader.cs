using System.Text.Json;
using DotNetSQLiEval.Core.Models;

namespace DotNetSQLiEval.Engine;

public static class OutputTaskLoader
{
    public static List<OutputTask> LoadFromDirectory(string directory)
    {
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return Directory.GetFiles(directory, "*.json")
            .OrderBy(f => f)
            .Select(f => JsonSerializer.Deserialize<OutputTask>(File.ReadAllText(f), opts)!)
            .Where(t => t != null)
            .ToList();
    }
}
