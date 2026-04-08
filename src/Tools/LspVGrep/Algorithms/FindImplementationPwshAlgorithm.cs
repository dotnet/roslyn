using LspVGrepTool.Execution;
using LspVGrepTool.Models;

namespace LspVGrepTool.Algorithms;

internal sealed class FindInterfaceImplementationPwshAlgorithm : QueryAlgorithm<FindInterfaceImplementationQuery>
{
    public override string Name => "find-interface-implementation-grep";

    public override string QueryType => QueryTypes.FindInterfaceImplementation;

    protected override async Task<AlgorithmExecutionResult> ExecuteTypedAsync(
        FindInterfaceImplementationQuery query,
        QueryExecutionContext context,
        CancellationToken cancellationToken)
    {
        var summary = $"string search for ': ...{query.Name}'";
        var searchResult = await context.SearchImplementationPwshAsync(query.Name, cancellationToken);

        if (searchResult.CommandMissing)
            return new AlgorithmExecutionResult(Name, AlgorithmOutcome.Failed, "'pwsh' was not available on PATH.", summary);

        if (searchResult.ExitCode != 0 && !string.IsNullOrWhiteSpace(searchResult.StandardError))
            return new AlgorithmExecutionResult(Name, AlgorithmOutcome.Failed, searchResult.StandardError.Trim(), summary);

        var responseText = string.IsNullOrWhiteSpace(searchResult.StandardOutput)
            ? "No implementations found with Select-String."
            : searchResult.StandardOutput.TrimEnd();

        return new AlgorithmExecutionResult(Name, AlgorithmOutcome.Succeeded, responseText, summary);
    }
}
