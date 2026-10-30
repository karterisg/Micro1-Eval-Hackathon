namespace DotNetSQLiEval.Core.Models;

public class RunConfig
{
    public string ModelId { get; set; } = "claude-sonnet-4-5";
    public int TrialsPerTask { get; set; } = 3;
    public double Temperature { get; set; } = 0.0;
    public int MaxTokens { get; set; } = 1024;
}
