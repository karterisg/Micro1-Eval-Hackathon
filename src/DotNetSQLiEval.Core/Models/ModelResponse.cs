namespace DotNetSQLiEval.Core.Models;

public class ModelResponse
{
    public string TaskId { get; set; } = "";
    public string ModelId { get; set; } = "";
    public string RawOutput { get; set; } = "";
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public int TrialNumber { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string? Error { get; set; }
}

public class GradingResult
{
    public string TaskId { get; set; } = "";
    public string ModelId { get; set; } = "";
    public int TrialNumber { get; set; }
    public bool CorrectClassification { get; set; }
    public bool HasRequiredKeywords { get; set; }
    public bool HasForbiddenKeywords { get; set; }
    public bool HasRemediationKeywords { get; set; }
    public int Score { get; set; }
    public string Verdict { get; set; } = "";
    public List<string> Notes { get; set; } = [];
}
