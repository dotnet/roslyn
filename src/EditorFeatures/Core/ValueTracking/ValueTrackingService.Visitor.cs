// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.EditAndContinue.TraceLog;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;

namespace Microsoft.CodeAnalysis.ValueTracking
{
    internal partial class ValueTrackingService
    {
        private class OperationCollector
        {
            public ValueTrackingProgressCollector ProgressCollector { get; }
            public Solution Solution { get; }

            public OperationCollector(ValueTrackingProgressCollector progressCollector, Solution solution)
            {
                ProgressCollector = progressCollector;
                Solution = solution;
            }

            public Task VisitAsync(IOperation operation, CancellationToken cancellationToken)
                => operation switch
                {
                    IObjectCreationOperation objectCreationOperation => VisitObjectCreationAsync(objectCreationOperation, cancellationToken),
                    IInvocationOperation invocationOperation => VisitInvocationAsync(invocationOperation, cancellationToken),
                    ILiteralOperation literalOperation => VisitLiteralAsync(literalOperation, cancellationToken),
                    IReturnOperation returnOperation => VisitReturnAsync(returnOperation, cancellationToken),
                    IFieldReferenceOperation fieldReferenceOperation => VisitFieldReferenceAsync(fieldReferenceOperation, cancellationToken),
                    _ => VisitDefaultAsync(operation, cancellationToken)
                };

            private async Task VisitDefaultAsync(IOperation operation, CancellationToken cancellationToken)
            {
                // If the operation has children, always visit the children instead of the root 
                // operation. They are the interesting bits for ValueTracking
                if (operation.Children.Any())
                {
                    foreach (var childOperation in operation.Children)
                    {
                        await VisitAsync(childOperation, cancellationToken).ConfigureAwait(false);
                    }

                    return;
                }

                var semanticModel = operation.SemanticModel;
                if (semanticModel is null)
                {
                    return;
                }

                var symbolInfo = semanticModel.GetSymbolInfo(operation.Syntax, cancellationToken);
                if (symbolInfo.Symbol is null)
                {
                    return;
                }

                await AddOperationAsync(operation, symbolInfo.Symbol, cancellationToken).ConfigureAwait(false);
            }

            private Task VisitFieldReferenceAsync(IFieldReferenceOperation fieldReferenceOperation, CancellationToken cancellationToken)
               => AddOperationAsync(fieldReferenceOperation, fieldReferenceOperation.Member, cancellationToken);

            private Task VisitObjectCreationAsync(IObjectCreationOperation objectCreationOperation, CancellationToken cancellationToken)
                => TrackArgumentsAsync(objectCreationOperation.Arguments, cancellationToken);

            private async Task VisitInvocationAsync(IInvocationOperation invocationOperation, CancellationToken cancellationToken)
            {
                await AddOperationAsync(invocationOperation, invocationOperation.TargetMethod, cancellationToken).ConfigureAwait(false);
                await TrackArgumentsAsync(invocationOperation.Arguments, cancellationToken).ConfigureAwait(false);
            }

            private async Task VisitLiteralAsync(ILiteralOperation literalOperation, CancellationToken cancellationToken)
            {
                if (literalOperation.Type is null)
                {
                    return;
                }

                await AddOperationAsync(literalOperation, literalOperation.Type, cancellationToken).ConfigureAwait(false);
            }

            private async Task VisitReturnAsync(IReturnOperation returnOperation, CancellationToken cancellationToken)
            {
                if (returnOperation.ReturnedValue is null)
                {
                    return;
                }

                await VisitAsync(returnOperation.ReturnedValue, cancellationToken).ConfigureAwait(false);
            }

            private async Task AddOperationAsync(IOperation operation, ISymbol symbol, CancellationToken cancellationToken)
            {
                _ = await ProgressCollector.TryReportAsync(
                        Solution,
                        operation.Syntax.GetLocation(),
                        symbol,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            private async Task TrackArgumentsAsync(ImmutableArray<IArgumentOperation> argumentOperations, CancellationToken cancellationToken)
            {
                var collectorsAndArgumentMap = argumentOperations
                    .Where(ShouldTrack)
                    .Select(argument => (collector: Clone(), argument))
                    .ToImmutableArray();

                var tasks = collectorsAndArgumentMap
                    .Select(pair => pair.collector.VisitAsync(pair.argument.Value, cancellationToken));

                await Task.WhenAll(tasks).ConfigureAwait(false);

                var items = collectorsAndArgumentMap
                    .Select(pair => pair.collector.ProgressCollector)
                    .SelectMany(collector => collector.GetItems())
                    .Reverse(); // ProgressCollector uses a Stack, and we want to maintain the order by arguments, so reverse

                foreach (var item in items)
                {
                    ProgressCollector.Report(item);
                }

                static bool ShouldTrack(IArgumentOperation argumentOperation)
                {
                    return argumentOperation.Parameter.IsRefOrOut()
                        || argumentOperation.Value is IExpressionStatementOperation
                            or IBinaryOperation
                            or IInvocationOperation
                            or IParameterReferenceOperation
                            or ILiteralOperation;
                }
            }

            private OperationCollector Clone()
            {
                var collector = new ValueTrackingProgressCollector();
                collector.Parent = ProgressCollector.Parent;
                return new OperationCollector(collector, Solution);
            }
        }
    }
}
