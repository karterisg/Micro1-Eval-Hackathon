namespace DotNetSQLiEval.Core.Models;

public class OutputTask
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public string Concept { get; set; } = "";
    public string Code { get; set; } = "";
    public string ExpectedOutput { get; set; } = "";
    public string Prompt { get; set; } = "";
}

public class OutputGradingResult
{
    public string TaskId { get; set; } = "";
    public string ModelId { get; set; } = "";
    public int TrialNumber { get; set; }
    public string ExpectedOutput { get; set; } = "";
    public string ModelPrediction { get; set; } = "";
    public string ActualOutput { get; set; } = "";
    public bool PredictionMatchesActual { get; set; }
    public string Verdict { get; set; } = "";
    public int Score { get; set; }
}
