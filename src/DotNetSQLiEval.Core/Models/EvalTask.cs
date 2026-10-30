namespace DotNetSQLiEval.Core.Models;

public class EvalTask
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Code { get; set; } = "";
    public string Prompt { get; set; } = "";
    public bool IsVulnerable { get; set; }
    public string VulnerabilityType { get; set; } = "";
    public string ReferenceAnswer { get; set; } = "";
    public string Remediation { get; set; } = "";
    public GradingCriteria Grading { get; set; } = new();
    public string Severity { get; set; } = "";
    public string DataFlowExplanation { get; set; } = "";
}

public class GradingCriteria
{
    public bool ExpectVulnerableClassification { get; set; }
    public List<string> RequiredKeywords { get; set; } = [];
    public List<string> ForbiddenKeywords { get; set; } = [];
    public List<string> RemediationKeywords { get; set; } = [];
    public int MaxScore { get; set; } = 100;
}
