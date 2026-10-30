using DotNetSQLiEval.Core.Models;

namespace DotNetSQLiEval.Engine;

public static class OutputGrader
{
    public static OutputGradingResult Grade(OutputTask task, string modelRawOutput, string actualOutput, string modelId, int trial)
    {
        // Extract just the prediction — strip markdown fences and extra explanation
        string prediction = ExtractPrediction(modelRawOutput);

        bool correct = Normalize(prediction) == Normalize(actualOutput);

        return new OutputGradingResult
        {
            TaskId = task.Id,
            ModelId = modelId,
            TrialNumber = trial,
            ExpectedOutput = task.ExpectedOutput,
            ActualOutput = actualOutput,
            ModelPrediction = prediction,
            PredictionMatchesActual = correct,
            Verdict = correct ? "PASS" : "FAIL",
            Score = correct ? 100 : 0
        };
    }

    private static string ExtractPrediction(string raw)
    {
        // Remove markdown code fences if present
        var lines = raw.Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .ToList();

        // Drop lines that look like explanation (start with uppercase words followed by colon, or "The output", etc.)
        // Keep only lines that are the actual output
        bool inFence = false;
        var outputLines = new List<string>();
        var fenceLines = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("```"))
            {
                inFence = !inFence;
                continue;
            }
            if (inFence)
                fenceLines.Add(line);
            else
                outputLines.Add(line);
        }

        // If there was a fenced block, use that
        if (fenceLines.Count > 0)
            return string.Join("\n", fenceLines).Trim();

        // Otherwise use full response trimmed
        return raw.Trim();
    }

    private static string Normalize(string s) =>
        s.Replace("\r\n", "\n").Replace("\r", "\n")
         .Replace("\\n", "\n")   // model sometimes writes literal \n
         .Trim();
}
