namespace LspVGrepTool.Models;

internal sealed record ToolReport(
    string Directory,
    string? RoslynTargetPath,
    string? RoslynTargetKind,
    TimeSpan? RoslynLoadTime,
    IReadOnlyList<QueryExecutionReport> Queries);

internal sealed record QueryExecutionReport(
    string Type,
    IReadOnlyDictionary<string, string> Fields,
    IReadOnlyList<AlgorithmExecutionResult> Algorithms);

internal enum AlgorithmOutcome
{
    Succeeded,
    Failed
}

internal sealed record AlgorithmExecutionResult(
    string AlgorithmName,
    AlgorithmOutcome Outcome,
    string ResponseText,
    string Summary = "",
    TimeSpan ElapsedTime = default)
{
    public int CharacterCount => ResponseText.Length;
    public int LineCount => ResponseText.Split('\n').Length;
}
