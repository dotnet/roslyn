using LspVGrepTool.Execution;
using LspVGrepTool.Models;

namespace LspVGrepTool.Algorithms;

internal sealed class FindDerivedTypesPwshAlgorithm : QueryAlgorithm<FindDerivedTypesQuery>
{
    public override string Name => "find-derived-types-grep";

    public override string QueryType => QueryTypes.FindDerivedTypes;

    protected override async Task<AlgorithmExecutionResult> ExecuteTypedAsync(
        FindDerivedTypesQuery query,
        QueryExecutionContext context,
        CancellationToken cancellationToken)
    {
        var summary = $"string search for '(class|record|struct) \\w+ ... {query.Name}'";
        var searchResult = await context.SearchDerivedTypesPwshAsync(query.Name, cancellationToken);

        if (searchResult.CommandMissing)
            return new AlgorithmExecutionResult(Name, AlgorithmOutcome.Failed, "'pwsh' was not available on PATH.", summary);

        if (searchResult.ExitCode != 0 && !string.IsNullOrWhiteSpace(searchResult.StandardError))
            return new AlgorithmExecutionResult(Name, AlgorithmOutcome.Failed, searchResult.StandardError.Trim(), summary);

        var responseText = string.IsNullOrWhiteSpace(searchResult.StandardOutput)
            ? "No derived types found with Select-String."
            : searchResult.StandardOutput.TrimEnd();

        return new AlgorithmExecutionResult(Name, AlgorithmOutcome.Succeeded, responseText, summary);
    }
}
