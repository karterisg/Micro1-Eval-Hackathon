using System.Text.Json;
using DotNetSQLiEval.Core.Models;

namespace DotNetSQLiEval.Engine;

public static class TaskLoader
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static List<EvalTask> LoadFromDirectory(string directory)
    {
        var tasks = new List<EvalTask>();
        foreach (var file in Directory.GetFiles(directory, "*.json").OrderBy(f => f))
        {
            var json = File.ReadAllText(file);
            var task = JsonSerializer.Deserialize<EvalTask>(json, Opts);
            if (task != null) tasks.Add(task);
        }
        return tasks;
    }
}
