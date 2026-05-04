using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using LspVGrepTool.Execution;
using LspVGrepTool.Models;

namespace LspVGrepTool.Algorithms;

internal sealed class FindTypeDefinitionRoslynLspAlgorithm : QueryAlgorithm<FindTypeDefinitionQuery>
{
    public override string Name => "roslyn-find-declarations-with-pattern";

    public override string QueryType => QueryTypes.FindTypeDefinition;

    protected override async Task<AlgorithmExecutionResult> ExecuteTypedAsync(
        FindTypeDefinitionQuery query,
        QueryExecutionContext context,
        CancellationToken cancellationToken)
    {
        var workspace = await context.GetWorkspaceAsync(cancellationToken);
        var summary = $"called SymbolFinder.FindSourceDeclarationsWithPatternAsync(solution, '{query.Name}', SymbolFilter.Type)";

        var declarations = await SymbolFinder.FindSourceDeclarationsWithPatternAsync(
            workspace.Solution,
            query.Name,
            SymbolFilter.Type,
            cancellationToken);

        var matches = new List<string>();
        foreach (var symbol in declarations.OfType<INamedTypeSymbol>())
        {
            foreach (var location in symbol.Locations.Where(loc => loc.IsInSource))
            {
                var span = location.GetLineSpan();
                var displayName = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                matches.Add($"{displayName} - {span.Path}({span.StartLinePosition.Line + 1},{span.StartLinePosition.Character + 1})");
            }
        }

        var distinct = matches
            .Distinct(StringComparer.Ordinal)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();

        var lines = new List<string>
        {
            $"Loaded {workspace.TargetKind}: {workspace.TargetPath}"
        };

        if (distinct.Count == 0)
        {
            lines.Add("No matching types found.");
        }
        else
        {
            lines.AddRange(distinct);
        }

        return new AlgorithmExecutionResult(
            Name,
            AlgorithmOutcome.Succeeded,
            string.Join(Environment.NewLine, lines),
            summary);
    }
}
