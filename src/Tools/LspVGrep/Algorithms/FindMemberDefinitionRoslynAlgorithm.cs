using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using LspVGrepTool.Execution;
using LspVGrepTool.Models;

namespace LspVGrepTool.Algorithms;

internal sealed class FindMemberDefinitionRoslynAlgorithm : QueryAlgorithm<FindMemberDefinitionQuery>
{
    public override string Name => "roslyn-find-member-declarations";

    public override string QueryType => QueryTypes.FindMemberDefinition;

    protected override async Task<AlgorithmExecutionResult> ExecuteTypedAsync(
        FindMemberDefinitionQuery query,
        QueryExecutionContext context,
        CancellationToken cancellationToken)
    {
        var workspace = await context.GetWorkspaceAsync(cancellationToken);
        var matches = new List<string>();

        foreach (var project in workspace.Solution.Projects)
        {
            var declarations = await SymbolFinder.FindDeclarationsAsync(
                project,
                query.Name,
                ignoreCase: false,
                SymbolFilter.Member,
                cancellationToken);

            foreach (var symbol in declarations)
            {
                foreach (var location in symbol.Locations.Where(location => location.IsInSource))
                {
                    var span = location.GetLineSpan();
                    var filePath = span.Path;
                    var lineNumber = span.StartLinePosition.Line + 1;
                    var columnNumber = span.StartLinePosition.Character + 1;
                    var displayName = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                    matches.Add($"{displayName} - {filePath}({lineNumber},{columnNumber})");
                }
            }
        }

        var distinctMatches = matches
            .Distinct(StringComparer.Ordinal)
            .OrderBy(match => match, StringComparer.Ordinal)
            .ToList();

        var responseLines = new List<string>
        {
            $"Loaded {workspace.TargetKind}: {workspace.TargetPath}"
        };

        if (distinctMatches.Count == 0)
        {
            responseLines.Add("No matching members found.");
        }
        else
        {
            responseLines.AddRange(distinctMatches);
        }

        return new AlgorithmExecutionResult(
            Name,
            AlgorithmOutcome.Succeeded,
            string.Join(Environment.NewLine, responseLines),
            $"called SymbolFinder.FindDeclarationsAsync(project, '{query.Name}', SymbolFilter.Member)");
    }
}
