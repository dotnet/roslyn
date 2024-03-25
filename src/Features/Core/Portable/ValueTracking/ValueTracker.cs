// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ValueTracking;

internal static partial class ValueTracker
{
    public static async Task TrackValueSourceAsync(
        TextSpan selection,
        Document document,
        ValueTrackingProgressCollector progressCollector,
        CancellationToken cancellationToken)
    {
        var (symbol, node) = await GetSelectedSymbolAsync(selection, document, cancellationToken).ConfigureAwait(false);
        var operationCollector = new OperationCollector(progressCollector, document.Project.Solution);

        if (symbol
            is IPropertySymbol
            or IFieldSymbol
            or ILocalSymbol
            or IParameterSymbol)
        {
            RoslynDebug.AssertNotNull(node);

            var solution = document.Project.Solution;
            var declaringSyntaxReferences = symbol.DeclaringSyntaxReferences;
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // If the selection is within a declaration of the symbol, we want to include
            // all declarations and assignments of the symbol
            if (declaringSyntaxReferences.Any(static (r, selection) => r.Span.IntersectsWith(selection), selection))
            {
                // Add all initializations of the symbol. Those are not caught in 
                // the reference finder but should still show up in the tree
                foreach (var syntaxRef in declaringSyntaxReferences)
                {
                    var location = Location.Create(syntaxRef.SyntaxTree, syntaxRef.Span);
                    await progressCollector.TryReportAsync(solution, location, symbol, cancellationToken).ConfigureAwait(false);
                }

                await TrackVariableReferencesAsync(symbol, operationCollector, cancellationToken).ConfigureAwait(false);
            }
            // The selection is not on a declaration, check that the node
            // is on the left side of an assignment. If so, populate so we can
            // track the RHS values that contribute to this value
            else if (syntaxFacts.IsLeftSideOfAnyAssignment(node))
            {
                await AddItemsFromAssignmentAsync(document, node, operationCollector, cancellationToken).ConfigureAwait(false);
            }
            // Not on the left part of an assignment? Then just add an item with the statement
            // and the symbol. It should be the top item, and children will find the sources
            // of the value. A good example is a return statement, such as "return $$x",
            // where $$ is the cursor position. The top item should have the return statement for
            // context, and the remaining items should expand into the assignments of x
            else
            {
                await progressCollector.TryReportAsync(document.Project.Solution, node.GetLocation(), symbol, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public static async Task TrackValueSourceAsync(
        Solution solution,
        ValueTrackedItem previousTrackedItem,
        ValueTrackingProgressCollector progressCollector,
        CancellationToken cancellationToken)
    {
        progressCollector.Parent = previousTrackedItem;
        var operationCollector = new OperationCollector(progressCollector, solution);
        var symbol = await GetSymbolAsync(previousTrackedItem, solution, cancellationToken).ConfigureAwait(false);

        switch (symbol)
        {
            case ILocalSymbol:
            case IPropertySymbol:
            case IFieldSymbol:
                {
                    // The "output" is a variable assignment, track places where it gets assigned and defined
                    await TrackVariableDefinitionsAsync(symbol, operationCollector, cancellationToken).ConfigureAwait(false);
                    await TrackVariableReferencesAsync(symbol, operationCollector, cancellationToken).ConfigureAwait(false);
                }

                break;

            case IParameterSymbol parameterSymbol:
                {
                    var previousSymbol = await GetSymbolAsync(previousTrackedItem.Parent, solution, cancellationToken).ConfigureAwait(false);

                    // If the current parameter is a parameter symbol for the previous tracked method it should be treated differently.
                    // For example: 
                    // string PrependString(string pre, string s) => pre + s;
                    //        ^--- previously tracked          ^---- current parameter being tracked
                    //
                    // In this case, s is being tracked because it contributed to the return of the method. We only
                    // want to track assignments to s that could impact the return rather than tracking the same method
                    // twice.
                    var isParameterForPreviousTrackedMethod = previousSymbol?.Equals(parameterSymbol.ContainingSymbol, SymbolEqualityComparer.Default) == true;

                    // For Ref or Out parameters, they contribute data across method calls through assignments
                    // within the method. No need to track returns
                    // Ex: TryGetValue("mykey", out var [|v|])
                    // [|v|] is the interesting part, we don't care what the method returns
                    var isRefOrOut = parameterSymbol.IsRefOrOut();

                    // Always track the parameter assignments as variables, in case they are assigned anywhere in the method
                    await TrackVariableReferencesAsync(parameterSymbol, operationCollector, cancellationToken).ConfigureAwait(false);

                    var trackMethod = !(isParameterForPreviousTrackedMethod || isRefOrOut);
                    if (trackMethod)
                    {
                        await TrackParameterSymbolAsync(parameterSymbol, operationCollector, cancellationToken).ConfigureAwait(false);
                    }
                }

                break;

            case IMethodSymbol methodSymbol:
                {
                    // The "output" is from a method, meaning it has a return or out param that is used. Track those 
                    await TrackMethodSymbolAsync(methodSymbol, operationCollector, cancellationToken).ConfigureAwait(false);
                }

                break;
        }
    }

    private static async Task AddItemsFromAssignmentAsync(Document document, SyntaxNode lhsNode, OperationCollector collector, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var operation = semanticModel.GetOperation(lhsNode, cancellationToken);
        if (operation is null)
        {
            return;
        }

        IAssignmentOperation? assignmentOperation = null;

        while (assignmentOperation is null
            && operation is not null)
        {
            assignmentOperation = operation as IAssignmentOperation;
            operation = operation.Parent;
        }

        if (assignmentOperation is null)
        {
            return;
        }

        await collector.VisitAsync(assignmentOperation, cancellationToken).ConfigureAwait(false);
    }

    private static async Task TrackVariableReferencesAsync(ISymbol symbol, OperationCollector collector, CancellationToken cancellationToken)
    {
        var findReferenceProgressCollector = new FindReferencesProgress(collector);
        await SymbolFinder.FindReferencesAsync(
                                symbol,
                                collector.Solution,
                                findReferenceProgressCollector,
                                documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
    }

    private static async Task TrackParameterSymbolAsync(
        IParameterSymbol parameterSymbol,
        OperationCollector collector,
        CancellationToken cancellationToken)
    {
        var containingSymbol = parameterSymbol.ContainingSymbol;
        var findReferenceProgressCollector = new FindReferencesProgress(collector);
        await SymbolFinder.FindReferencesAsync(
            containingSymbol,
            collector.Solution,
            findReferenceProgressCollector,
            documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
    }

    private static async Task TrackMethodSymbolAsync(IMethodSymbol methodSymbol, OperationCollector collector, CancellationToken cancellationToken)
    {
        var hasAnyOutData = HasAValueReturn(methodSymbol) || HasAnOutOrRefParam(methodSymbol);
        if (!hasAnyOutData)
        {
            // With no out data, there's nothing to do here
            return;
        }

        // TODO: Use DFA to find meaningful returns? https://github.com/dotnet/roslyn-analyzers/blob/9e5f533cbafcc5579e4d758bc9bde27b7611ca54/docs/Writing%20dataflow%20analysis%20based%20analyzers.md 
        if (HasAValueReturn(methodSymbol))
        {
            foreach (var location in methodSymbol.GetDefinitionLocationsToShow())
            {
                if (location.SourceTree is null)
                {
                    continue;
                }

                var node = location.FindNode(cancellationToken);
                var sourceDoc = collector.Solution.GetRequiredDocument(location.SourceTree);
                var syntaxFacts = sourceDoc.GetRequiredLanguageService<ISyntaxFactsService>();
                var semanticModel = await sourceDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var operation = semanticModel.GetOperation(node, cancellationToken);

                // In VB the parent node contains the operation (IBlockOperation) instead of the one returned
                // by the symbol location.
                if (operation is null && node.Parent is not null)
                {
                    operation = semanticModel.GetOperation(node.Parent, cancellationToken);
                }

                if (operation is null)
                {
                    continue;
                }

                await collector.VisitAsync(operation, cancellationToken).ConfigureAwait(false);
            }
        }

        if (HasAnOutOrRefParam(methodSymbol))
        {
            foreach (var outOrRefParam in methodSymbol.Parameters.Where(p => p.IsRefOrOut()))
            {
                if (!outOrRefParam.IsFromSource())
                {
                    continue;
                }

                await TrackVariableReferencesAsync(outOrRefParam, collector, cancellationToken).ConfigureAwait(false);
            }
        }

        // TODO check for Task
        static bool HasAValueReturn(IMethodSymbol methodSymbol)
        {
            return methodSymbol.ReturnType.SpecialType != SpecialType.System_Void;
        }

        static bool HasAnOutOrRefParam(IMethodSymbol methodSymbol)
        {
            return methodSymbol.Parameters.Any(static p => p.IsRefOrOut());
        }
    }

    private static async Task<(ISymbol?, SyntaxNode?)> GetSelectedSymbolAsync(TextSpan textSpan, Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var selectedNode = root.FindNode(textSpan);

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var selectedSymbol =
            semanticModel.GetSymbolInfo(selectedNode, cancellationToken).Symbol
            ?? semanticModel.GetDeclaredSymbol(selectedNode, cancellationToken);

        if (selectedSymbol is null)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // If the node is an argument it's possible that it's just 
            // an identifier in the expression. If so, then grab the symbol
            // for that node instead of the argument. 
            // EX: MyMethodCall($$x, y) should get the identifier x and
            // the symbol for that identifier
            if (syntaxFacts.IsArgument(selectedNode))
            {
                selectedNode = syntaxFacts.GetExpressionOfArgument(selectedNode)!;
                selectedSymbol = semanticModel.GetSymbolInfo(selectedNode, cancellationToken).Symbol;
            }
        }

        return (selectedSymbol, selectedNode);
    }

    private static async Task TrackVariableDefinitionsAsync(ISymbol symbol, OperationCollector collector, CancellationToken cancellationToken)
    {
        foreach (var definitionLocation in symbol.Locations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (definitionLocation is not { SourceTree: not null })
            {
                continue;
            }

            var node = definitionLocation.FindNode(cancellationToken);
            var document = collector.Solution.GetRequiredDocument(node.SyntaxTree);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var operation = semanticModel.GetOperation(node, cancellationToken);

            var declarators = operation switch
            {
                IVariableDeclaratorOperation variableDeclarator => [variableDeclarator],
                IVariableDeclarationOperation variableDeclaration => variableDeclaration.Declarators,
                _ => []
            };

            foreach (var declarator in declarators)
            {
                var initializer = declarator.GetVariableInitializer();
                if (initializer is null)
                {
                    continue;
                }

                await collector.VisitAsync(initializer, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task<ISymbol?> GetSymbolAsync(ValueTrackedItem? item, Solution solution, CancellationToken cancellationToken)
    {
        if (item is null)
        {
            return null;
        }

        var document = solution.GetRequiredDocument(item.DocumentId);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return item.SymbolKey.Resolve(semanticModel.Compilation, cancellationToken: cancellationToken).Symbol;
    }
}
