using System.Text.Json;
using DotNetSQLiEval.Core.Models;
using DotNetSQLiEval.Engine;
using DotNetSQLiEval.Providers;

// Mode: regrade (no API calls) or run (API calls)
bool isRegrade = args.Length > 0 && args[0] == "regrade";

string tasksDir = isRegrade
    ? (args.Length > 1 ? args[1] : "tasks/final")
    : (args.Length > 0 ? args[0] : "tasks/final");

string resultsDir = isRegrade
    ? (args.Length > 2 ? args[2] : "results/raw")
    : (args.Length > 1 ? args[1] : "results/raw");

// v4 mode
if (args.Length > 0 && args[0] == "v4")
{
    await DotNetSQLiEval.Cli.ProgramV4.Run(args);
    return;
}

// v3 mode
if (args.Length > 0 && args[0] == "v3")
{
    await DotNetSQLiEval.Cli.ProgramV3.Run(args);
    return;
}

// v2 mode
if (args.Length > 0 && args[0] == "v2")
{
    await DotNetSQLiEval.Cli.ProgramV2.Run(args);
    return;
}

if (isRegrade)
{
    Console.WriteLine($"Re-grading transcripts in {resultsDir} using tasks from {tasksDir}...\n");
    DotNetSQLiEval.Cli.Regrade.Run(tasksDir, resultsDir);
    return;
}

var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
    ?? throw new Exception("Set ANTHROPIC_API_KEY environment variable.");

Directory.CreateDirectory(resultsDir);

var tasks = TaskLoader.LoadFromDirectory(tasksDir);
Console.WriteLine($"Loaded {tasks.Count} tasks from {tasksDir}");

var config = new RunConfig
{
    ModelId = "claude-sonnet-4-5",
    TrialsPerTask = 3,
    Temperature = 0.0,
    MaxTokens = 1024
};

var provider = new AnthropicProvider(apiKey);
var allResponses = new List<ModelResponse>();
var allGrades = new List<GradingResult>();

var opts = new JsonSerializerOptions { WriteIndented = true };

foreach (var task in tasks)
{
    Console.WriteLine($"\n=== Task {task.Id}: {task.Title} ===");

    for (int trial = 1; trial <= config.TrialsPerTask; trial++)
    {
        Console.Write($"  Trial {trial}/{config.TrialsPerTask}... ");

        var response = await provider.RunAsync(task, config, trial);
        allResponses.Add(response);

        var grade = Grader.Grade(task, response);
        allGrades.Add(grade);

        Console.WriteLine($"{grade.Verdict} (score: {grade.Score}/100)");
        if (grade.Notes.Count > 0)
            foreach (var note in grade.Notes)
                Console.WriteLine($"    NOTE: {note}");

        // Save transcript
        var transcriptPath = Path.Combine(resultsDir, $"{task.Id}_trial{trial}.json");
        File.WriteAllText(transcriptPath, JsonSerializer.Serialize(new { response, grade }, opts));

        await Task.Delay(500); // avoid rate limits
    }
}

// Summary report
Console.WriteLine("\n=== SUMMARY ===");
Console.WriteLine($"{"Task",-6} {"Model",-25} {"Pass%",6} {"Avg Score",10}");
Console.WriteLine(new string('-', 55));

foreach (var task in tasks)
{
    var taskGrades = allGrades.Where(g => g.TaskId == task.Id).ToList();
    double passRate = taskGrades.Count(g => g.Verdict == "PASS") * 100.0 / taskGrades.Count;
    double avgScore = taskGrades.Average(g => g.Score);
    Console.WriteLine($"{task.Id,-6} {config.ModelId,-25} {passRate,5:0}% {avgScore,10:0.0}");
}

// Save full results
var summaryPath = Path.Combine(resultsDir, "summary.json");
File.WriteAllText(summaryPath, JsonSerializer.Serialize(new
{
    RunDate = DateTime.UtcNow,
    Model = config.ModelId,
    TrialsPerTask = config.TrialsPerTask,
    Temperature = config.Temperature,
    Grades = allGrades
}, opts));

Console.WriteLine($"\nResults saved to: {resultsDir}");
