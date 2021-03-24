// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Elfie.Model;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ValueTracking
{
    [ExportWorkspaceService(typeof(IValueTrackingService)), Shared]
    internal partial class ValueTrackingService : IValueTrackingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ValueTrackingService()
        {
        }

        public async Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(
            TextSpan selection,
            Document document,
            CancellationToken cancellationToken)
        {
            var progressTracker = new ValueTrackingProgressCollector();
            await TrackValueSourceAsync(selection, document, progressTracker, cancellationToken).ConfigureAwait(false);
            return progressTracker.GetItems();
        }

        public async Task TrackValueSourceAsync(
            TextSpan selection,
            Document document,
            ValueTrackingProgressCollector progressCollector,
            CancellationToken cancellationToken)
        {
            var (symbol, node) = await GetSelectedSymbolAsync(selection, document, cancellationToken).ConfigureAwait(false);

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
                if (declaringSyntaxReferences.Any(r => r.Span.IntersectsWith(selection)))
                {
                    // Add all initializations of the symbol. Those are not caught in 
                    // the reference finder but should still show up in the tree
                    foreach (var syntaxRef in declaringSyntaxReferences)
                    {
                        var location = Location.Create(syntaxRef.SyntaxTree, syntaxRef.Span);
                        await progressCollector.TryReportAsync(solution, location, symbol, cancellationToken).ConfigureAwait(false);
                    }

                    await TrackVariableSymbolAsync(symbol, document, progressCollector, cancellationToken).ConfigureAwait(false);
                }
                // The selection is not on a declaration, check that the node
                // is on the left side of an assignment. If so, populate so we can
                // track the RHS values that contribute to this value
                else if (syntaxFacts.IsLeftSideOfAnyAssignment(node))
                {
                    await AddItemsFromAssignmentAsync(document, node, progressCollector, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(
            ValueTrackedItem previousTrackedItem,
            CancellationToken cancellationToken)
        {
            var progressTracker = new ValueTrackingProgressCollector();
            await TrackValueSourceAsync(previousTrackedItem, progressTracker, cancellationToken).ConfigureAwait(false);
            return progressTracker.GetItems();
        }

        public async Task TrackValueSourceAsync(
            ValueTrackedItem previousTrackedItem,
            ValueTrackingProgressCollector progressCollector,
            CancellationToken cancellationToken)
        {
            progressCollector.Parent = previousTrackedItem;

            switch (previousTrackedItem.Symbol)
            {
                case ILocalSymbol:
                case IPropertySymbol:
                case IFieldSymbol:
                    {
                        // The "output" is a variable assignment, track places where it gets assigned
                        await TrackVariableSymbolAsync(previousTrackedItem.Symbol, previousTrackedItem.Document, progressCollector, cancellationToken).ConfigureAwait(false);
                    }
                    break;

                case IParameterSymbol parameterSymbol:
                    {
                        // The "output" is method calls, so track where this method is invoked for the parameter
                        await TrackParameterSymbolAsync(parameterSymbol, previousTrackedItem.Document, progressCollector, cancellationToken).ConfigureAwait(false);
                    }
                    break;

                case IMethodSymbol methodSymbol:
                    {
                        // The "output" is from a method, meaning it has a return our out param that is used. Track those 
                        await TrackMethodSymbolAsync(methodSymbol, previousTrackedItem.Document, progressCollector, cancellationToken).ConfigureAwait(false);
                    }
                    break;
            }
        }

        private static async Task TrackVariableSymbolAsync(ISymbol symbol, Document document, ValueTrackingProgressCollector progressCollector, CancellationToken cancellationToken)
        {
            var findReferenceProgressCollector = new FindReferencesProgress(progressCollector);
            await SymbolFinder.FindReferencesAsync(
                                    symbol,
                                    document.Project.Solution,
                                    findReferenceProgressCollector,
                                    documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TrackParameterSymbolAsync(
            IParameterSymbol parameterSymbol,
            Document document,
            ValueTrackingProgressCollector progressCollector,
            CancellationToken cancellationToken)
        {
            var containingMethod = (IMethodSymbol)parameterSymbol.ContainingSymbol;
            var findReferenceProgressCollector = new FindReferencesProgress(progressCollector);
            await SymbolFinder.FindReferencesAsync(
                containingMethod,
                document.Project.Solution,
                findReferenceProgressCollector,
                documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TrackMethodSymbolAsync(IMethodSymbol methodSymbol, Document document, ValueTrackingProgressCollector progressCollector, CancellationToken cancellationToken)
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
                    var sourceDoc = document.Project.Solution.GetRequiredDocument(location.SourceTree);
                    var syntaxFacts = sourceDoc.GetRequiredLanguageService<ISyntaxFactsService>();
                    var returnStatements = node.DescendantNodesAndSelf().Where(n => syntaxFacts.IsReturnStatement(n));
                    var semanticModel = await sourceDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var returnStatement in returnStatements)
                    {
                        var expression = syntaxFacts.GetExpressionOfReturnStatement(returnStatement);
                        if (expression is null)
                        {
                            continue;
                        }

                        var operation = semanticModel.GetOperation(expression, cancellationToken);
                        if (operation is null)
                        {
                            continue;
                        }

                        await TrackExpressionAsync(operation, sourceDoc, progressCollector, cancellationToken).ConfigureAwait(false);
                    }
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

                    await TrackVariableSymbolAsync(outOrRefParam, document, progressCollector, cancellationToken).ConfigureAwait(false);
                }
            }

            // TODO check for Task
            static bool HasAValueReturn(IMethodSymbol methodSymbol)
                => methodSymbol.ReturnType.SpecialType != SpecialType.System_Void;

            static bool HasAnOutOrRefParam(IMethodSymbol methodSymbol)
                => methodSymbol.Parameters.Any(p => p.IsRefOrOut());
        }

        private static async Task TrackExpressionAsync(IOperation expressionOperation, Document document, ValueTrackingProgressCollector progressCollector, CancellationToken cancellationToken)
        {
            if (expressionOperation.Children.Any())
            {
                foreach (var childOperation in expressionOperation.Children)
                {
                    await AddOperationAsync(childOperation).ConfigureAwait(false);
                }
            }
            else
            {
                await AddOperationAsync(expressionOperation).ConfigureAwait(false);
            }

            async Task AddOperationAsync(IOperation operation)
            {
                var semanticModel = operation.SemanticModel;
                if (semanticModel is null)
                {
                    return;
                }

                var symbolInfo = semanticModel.GetSymbolInfo(operation.Syntax, cancellationToken);
                if (symbolInfo.Symbol is null)
                {
                    if (operation is ILiteralOperation literalOperation)
                    {
                        await progressCollector.TryReportAsync(document.Project.Solution,
                            operation.Syntax.GetLocation(),
                            literalOperation.Type,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                    }

                    return;
                }

                await progressCollector.TryReportAsync(document.Project.Solution,
                    operation.Syntax.GetLocation(),
                    symbolInfo.Symbol,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
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

            return (selectedSymbol, selectedNode);
        }


        private static async Task AddItemsFromAssignmentAsync(Document document, SyntaxNode lhsNode, ValueTrackingProgressCollector progressCollector, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var operation = semanticModel.GetOperation(lhsNode);
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

            var rhsOperation = assignmentOperation.Value;
            await TrackExpressionAsync(rhsOperation, document, progressCollector, cancellationToken).ConfigureAwait(false);
        }
    }
}
