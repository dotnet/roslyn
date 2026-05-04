using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using LspVGrepTool.Execution;
using LspVGrepTool.Infrastructure;
using LspVGrepTool.Models;

namespace LspVGrepTool.Algorithms;

internal sealed class FindInterfaceImplementationRoslynAlgorithm : QueryAlgorithm<FindInterfaceImplementationQuery>
{
    public override string Name => "roslyn-find-implementations";

    public override string QueryType => QueryTypes.FindInterfaceImplementation;

    protected override async Task<AlgorithmExecutionResult> ExecuteTypedAsync(
        FindInterfaceImplementationQuery query,
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

        var results = await SymbolFinder.FindImplementationsAsync(
            targetSymbol, workspace.Solution, cancellationToken: cancellationToken);

        var displayName = targetSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        return FormatResults(workspace, targetSymbol, results.OfType<INamedTypeSymbol>(),
            $"called SymbolFinder.FindImplementationsAsync({displayName})");
    }

    private AlgorithmExecutionResult FormatResults(
        WorkspaceLoadResult workspace, INamedTypeSymbol target, IEnumerable<INamedTypeSymbol> symbols, string summary)
    {
        var matches = new List<string>();
        foreach (var symbol in symbols)
        {
            foreach (var location in symbol.Locations.Where(loc => loc.IsInSource))
            {
                var span = location.GetLineSpan();
                matches.Add($"{symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)} - {span.Path}({span.StartLinePosition.Line + 1},{span.StartLinePosition.Character + 1})");
            }
        }

        var distinct = matches.Distinct(StringComparer.Ordinal).OrderBy(m => m, StringComparer.Ordinal).ToList();
        var lines = new List<string>
        {
            $"Loaded {workspace.TargetKind}: {workspace.TargetPath}",
            $"Target: {target.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)} ({target.TypeKind})"
        };
        lines.AddRange(distinct.Count == 0 ? ["No implementations found."] : distinct);

        return new AlgorithmExecutionResult(Name, AlgorithmOutcome.Succeeded, string.Join(Environment.NewLine, lines), summary);
    }
}
