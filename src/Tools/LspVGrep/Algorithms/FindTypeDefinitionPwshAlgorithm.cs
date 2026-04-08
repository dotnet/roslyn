using LspVGrepTool.Execution;
using LspVGrepTool.Models;

namespace LspVGrepTool.Algorithms;

internal sealed class FindTypeDefinitionPwshAlgorithm : QueryAlgorithm<FindTypeDefinitionQuery>
{
    public override string Name => "find-type-definition-grep";

    public override string QueryType => QueryTypes.FindTypeDefinition;

    protected override async Task<AlgorithmExecutionResult> ExecuteTypedAsync(
        FindTypeDefinitionQuery query,
        QueryExecutionContext context,
        CancellationToken cancellationToken)
    {
        var summary = $"string search for '(class|record|struct|interface|enum) {query.Name}'";
        var searchResult = await context.SearchTypeDefinitionPwshAsync(query.Name, cancellationToken);

        if (searchResult.CommandMissing)
        {
            return new AlgorithmExecutionResult(
                Name,
                AlgorithmOutcome.Failed,
                "'pwsh' was not available on PATH.",
                summary);
        }

        if (searchResult.ExitCode != 0 && !string.IsNullOrWhiteSpace(searchResult.StandardError))
        {
            return new AlgorithmExecutionResult(
                Name,
                AlgorithmOutcome.Failed,
                searchResult.StandardError.Trim(),
                summary);
        }

        var responseText = string.IsNullOrWhiteSpace(searchResult.StandardOutput)
            ? "No matches found with Select-String."
            : searchResult.StandardOutput.TrimEnd();

        return new AlgorithmExecutionResult(
            Name,
            AlgorithmOutcome.Succeeded,
            responseText,
            summary);
    }
}
