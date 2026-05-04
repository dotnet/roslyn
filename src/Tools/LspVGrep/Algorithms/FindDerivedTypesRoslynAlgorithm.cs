using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using LspVGrepTool.Execution;
using LspVGrepTool.Models;

namespace LspVGrepTool.Algorithms;

internal sealed class FindDerivedTypesRoslynAlgorithm : QueryAlgorithm<FindDerivedTypesQuery>
{
    public override string Name => "roslyn-find-derived-types";

    public override string QueryType => QueryTypes.FindDerivedTypes;

    protected override async Task<AlgorithmExecutionResult> ExecuteTypedAsync(
        FindDerivedTypesQuery query,
        QueryExecutionContext context,
        CancellationToken cancellationToken)
    {
        var workspace = await context.GetWorkspaceAsync(cancellationToken);

        INamedTypeSymbol? targetSymbol = null;
        foreach (var project in workspace.Solution.Projects)
        {
            var declarations = await SymbolFinder.FindDeclarationsAsync(
                project, query.Name, ignoreCase: false, SymbolFilter.Type, cancellationToken);
            targetSymbol = declarations.OfType<INamedTypeSymbol>().FirstOrDefault();
            if (targetSymbol is not null) break;
        }

        if (targetSymbol is null)
        {
            var notFoundSummary = $"called SymbolFinder.FindDeclarationsAsync for '{query.Name}' — not found";
            return new AlgorithmExecutionResult(Name, AlgorithmOutcome.Succeeded,
                $"Loaded {workspace.TargetKind}: {workspace.TargetPath}\nType '{query.Name}' was not found in the solution.",
                notFoundSummary);
        }

        var displayName = targetSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        var derivedTypes = await SymbolFinder.FindDerivedClassesAsync(
            targetSymbol, workspace.Solution, cancellationToken: cancellationToken);

        var matches = new List<string>();
        foreach (var derived in derivedTypes)
        {
            foreach (var location in derived.Locations.Where(loc => loc.IsInSource))
            {
                var span = location.GetLineSpan();
                matches.Add($"{derived.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)} - {span.Path}({span.StartLinePosition.Line + 1},{span.StartLinePosition.Character + 1})");
            }
        }

        var distinct = matches.Distinct(StringComparer.Ordinal).OrderBy(m => m, StringComparer.Ordinal).ToList();
        var lines = new List<string>
        {
            $"Loaded {workspace.TargetKind}: {workspace.TargetPath}",
            $"Target: {targetSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)} ({targetSymbol.TypeKind})"
        };
        lines.AddRange(distinct.Count == 0 ? ["No derived types found."] : distinct);

        return new AlgorithmExecutionResult(Name, AlgorithmOutcome.Succeeded, string.Join(Environment.NewLine, lines),
            $"called SymbolFinder.FindDerivedClassesAsync({displayName})");
    }
}
