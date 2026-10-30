using DotNetSQLiEval.Core.Models;

namespace DotNetSQLiEval.Engine;

public static class Grader
{
    public static GradingResult Grade(EvalTask task, ModelResponse response)
    {
        var result = new GradingResult
        {
            TaskId = task.Id,
            ModelId = response.ModelId,
            TrialNumber = response.TrialNumber
        };

        if (response.Error != null)
        {
            result.Verdict = "ERROR";
            result.Notes.Add($"Model call failed: {response.Error}");
            return result;
        }

        string output = response.RawOutput.ToLowerInvariant();

        // 1. Vulnerability classification (40 points)
        bool classifiedVulnerable = output.Contains("vulnerable") && !output.Contains("not vulnerable") && !output.Contains("safe from");
        bool classifiedSafe = output.Contains("safe") || output.Contains("not vulnerable");

        result.CorrectClassification = task.IsVulnerable ? classifiedVulnerable : (classifiedSafe && !classifiedVulnerable);
        int classificationScore = result.CorrectClassification ? 40 : 0;

        // 2. Required keywords present (30 points)
        var missingKeywords = task.Grading.RequiredKeywords
            .Where(kw => !output.Contains(kw.ToLowerInvariant()))
            .ToList();
        result.HasRequiredKeywords = missingKeywords.Count == 0;
        int keywordScore = result.HasRequiredKeywords ? 30 : (int)(30.0 * (task.Grading.RequiredKeywords.Count - missingKeywords.Count) / Math.Max(1, task.Grading.RequiredKeywords.Count));

        // 3. Forbidden keywords absent (15 points)
        var foundForbidden = task.Grading.ForbiddenKeywords
            .Where(kw => output.Contains(kw.ToLowerInvariant()))
            .ToList();
        result.HasForbiddenKeywords = foundForbidden.Count > 0;
        int forbiddenScore = result.HasForbiddenKeywords ? 0 : 15;

        if (result.HasForbiddenKeywords)
            result.Notes.Add($"Forbidden keywords found: {string.Join(", ", foundForbidden)}");

        // 4. Remediation keywords (15 points)
        var missingRemediation = task.Grading.RemediationKeywords
            .Where(kw => !output.Contains(kw.ToLowerInvariant()))
            .ToList();
        result.HasRemediationKeywords = missingRemediation.Count == 0;
        int remediationScore = result.HasRemediationKeywords ? 15 : (int)(15.0 * (task.Grading.RemediationKeywords.Count - missingRemediation.Count) / Math.Max(1, task.Grading.RemediationKeywords.Count));

        if (missingKeywords.Count > 0)
            result.Notes.Add($"Missing required keywords: {string.Join(", ", missingKeywords)}");
        if (missingRemediation.Count > 0)
            result.Notes.Add($"Missing remediation keywords: {string.Join(", ", missingRemediation)}");

        result.Score = classificationScore + keywordScore + forbiddenScore + remediationScore;
        result.Verdict = result.Score >= 70 ? "PASS" : result.Score >= 40 ? "PARTIAL" : "FAIL";

        return result;
    }
}
