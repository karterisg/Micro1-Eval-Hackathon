using System.Diagnostics;

namespace DotNetSQLiEval.Engine;

public static class OutputRunner
{
    private static readonly string RunnerProjectDir =
        Path.Combine(Directory.GetCurrentDirectory(), "runner");

    public static string Run(string code)
    {
        var programPath = Path.Combine(RunnerProjectDir, "Program.cs");
        File.WriteAllText(programPath, code);

        var psi = new ProcessStartInfo("dotnet", "run")
        {
            WorkingDirectory = Path.GetFullPath(RunnerProjectDir),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var proc = Process.Start(psi)!;
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(15000);

        // dotnet build warnings go to stderr; runtime output goes to stdout
        // But some warnings slip into stdout — filter them out
        var lines = stdout.Split('\n')
            .Where(l => !l.Contains(": warning CS") && !l.Contains(": error CS") && !l.Contains("Build FAILED"))
            .Select(l => l.TrimEnd('\r'));

        // If stderr has compile errors, return them so grader can detect failure
        if (stderr.Contains("error CS") || stderr.Contains("Build FAILED"))
            return stderr;

        return string.Join("\n", lines).TrimEnd('\r', '\n');
    }
}
