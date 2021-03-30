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
using System.Xml.Linq;
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
                        // The "output" is method calls, so track where this method is invoked for the parameter as 
                        // well as assignments inside the method. Both contribute to the final values
                        await TrackVariableSymbolAsync(previousTrackedItem.Symbol, previousTrackedItem.Document, progressCollector, cancellationToken).ConfigureAwait(false);
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
                    var returnStatements = node.DescendantNodesAndSelf().Where(n => syntaxFacts.IsReturnStatement(n)).ToImmutableArray();
                    var semanticModel = await sourceDoc.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    if (returnStatements.IsDefaultOrEmpty)
                    {
                        // If there are no return statements and the method has a return type, then the method body is an expression
                        // and we're interested in parsing that expression
                        var expression = node.DescendantNodesAndSelf().First(syntaxFacts.IsMethodBody);
                        if (expression is null)
                        {
                            return;
                        }

                        var operation = semanticModel.GetOperation(expression, cancellationToken);
                        if (operation is null)
                        {
                            continue;
                        }

                        await TrackExpressionAsync(operation, sourceDoc, progressCollector, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
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
                    await AddOperationAsync(childOperation, document, progressCollector, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await AddOperationAsync(expressionOperation, document, progressCollector, cancellationToken).ConfigureAwait(false);
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

        private static async Task AddOperationAsync(IOperation operation, Document document, ValueTrackingProgressCollector progressCollector, CancellationToken cancellationToken)
        {
            var semanticModel = operation.SemanticModel;
            if (semanticModel is null)
            {
                return;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(operation.Syntax, cancellationToken);
            if (symbolInfo.Symbol is null)
            {
                if (operation is ILiteralOperation { Type: not null } literalOperation)
                {
                    await progressCollector.TryReportAsync(document.Project.Solution,
                        operation.Syntax.GetLocation(),
                        literalOperation.Type!,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            if (operation is IInvocationOperation invocationOperation)
            {
                // If the operation is an invocation, we want to find if invocations are part of the arguments as well 
                // and make sure they are added
                foreach (var argument in invocationOperation.Arguments)
                {
                    if (argument.Value is IInvocationOperation argumentInvocationOperation)
                    {
                        await AddOperationAsync(argumentInvocationOperation, document, progressCollector, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            else if (operation is IReturnOperation { ReturnedValue: not null } returnOperation)
            {
                // For return operations we want to track
                // the value returned in case it has invocations
                // or other items that need to be handled special
                await AddOperationAsync(returnOperation.ReturnedValue, document, progressCollector, cancellationToken).ConfigureAwait(false);
                return;
            }

            await progressCollector.TryReportAsync(document.Project.Solution,
                operation.Syntax.GetLocation(),
                symbolInfo.Symbol,
                cancellationToken: cancellationToken).ConfigureAwait(false);
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

        private static async Task TrackArgumentsAsync(ImmutableArray<IArgumentOperation> argumentOperations, Document document, ValueTrackingProgressCollector progressCollector, CancellationToken cancellationToken)
        {
            var collectorsAndArgumentMap = argumentOperations
                .Select(argument => (collector: CreateCollector(), argument))
                .ToImmutableArray();

            var tasks = collectorsAndArgumentMap
                .Select(pair => TrackExpressionAsync(pair.argument.Value, document, pair.collector, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var items = collectorsAndArgumentMap
                .Select(pair => pair.collector)
                .SelectMany(collector => collector.GetItems())
                .Reverse(); // ProgressCollector uses a Stack, and we want to maintain the order by arguments, so reverse

            foreach (var item in items)
            {
                progressCollector.Report(item);
            }

            ValueTrackingProgressCollector CreateCollector()
            {
                var collector = new ValueTrackingProgressCollector();
                collector.Parent = progressCollector.Parent;
                return collector;
            }
        }
    }
}
