// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
                    IArgumentOperation argumentOperation => ShouldTrackArgument(argumentOperation) ? VisitAsync(argumentOperation.Value, cancellationToken) : Task.CompletedTask,
                    ILocalReferenceOperation or
                        IParameterReferenceOperation or
                        IFieldReferenceOperation or
                        IPropertyReferenceOperation => VisitReferenceAsync(operation, cancellationToken),
                    IAssignmentOperation assignmentOperation => VisitAssignmentOperationAsync(assignmentOperation, cancellationToken),

                    // Default to reporting if there is symbol information available
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

            private Task VisitAssignmentOperationAsync(IAssignmentOperation assignmentOperation, CancellationToken cancellationToken)
                => VisitDefaultAsync(assignmentOperation.Value, cancellationToken);

            private Task VisitObjectCreationAsync(IObjectCreationOperation objectCreationOperation, CancellationToken cancellationToken)
                => TrackArgumentsAsync(objectCreationOperation.Arguments, cancellationToken);

            private async Task VisitInvocationAsync(IInvocationOperation invocationOperation, CancellationToken cancellationToken)
            {
                await AddOperationAsync(invocationOperation, invocationOperation.TargetMethod, cancellationToken).ConfigureAwait(false);
                await TrackArgumentsAsync(invocationOperation.Arguments, cancellationToken).ConfigureAwait(false);
            }

            private Task VisitReferenceAsync(IOperation operation, CancellationToken cancellationToken)
            {
                Debug.Assert(operation is
                    ILocalReferenceOperation or
                    IParameterReferenceOperation or
                    IFieldReferenceOperation or
                    IPropertyReferenceOperation);

                if (IsArgument(operation, out var argumentOperation) && argumentOperation.Parameter is not null)
                {
                    if (argumentOperation.Parameter.IsRefOrOut())
                    {
                        // Always add ref or out parameters to track as assignments since the values count as 
                        // assignments across method calls for the purposes of value tracking.
                        return AddOperationAsync(operation, argumentOperation.Parameter, cancellationToken);
                    }

                    // If the parameter is not a ref or out param, track the reference assignments that count
                    // as input to the argument being passed to the method.
                    return AddReference(operation, cancellationToken);
                }

                if (IsReturn(operation))
                {
                    // If the reference is part of a return operation we want to track where the values come from
                    // since they contribute to the "output" of the method and are relavent for value tracking.
                    return AddReference(operation, cancellationToken);
                }

                return Task.CompletedTask;

                Task AddReference(IOperation operation, CancellationToken cancellationToken)
                    => operation switch
                    {
                        IParameterReferenceOperation parameterReference => AddOperationAsync(operation, parameterReference.Parameter, cancellationToken),
                        IFieldReferenceOperation fieldReferenceOperation => AddOperationAsync(operation, fieldReferenceOperation.Member, cancellationToken),
                        IPropertyReferenceOperation propertyReferenceOperation => AddOperationAsync(operation, propertyReferenceOperation.Member, cancellationToken),
                        ILocalReferenceOperation localReferenceOperation => AddOperationAsync(operation, localReferenceOperation.Local, cancellationToken),
                        _ => Task.CompletedTask
                    };
            }

            private static bool IsArgument(IOperation? operation, [NotNullWhen(returnValue: true)] out IArgumentOperation? argumentOperation)
            {
                while (operation is not null)
                {
                    if (operation is IArgumentOperation tmpArgumentOperation)
                    {
                        argumentOperation = tmpArgumentOperation;
                        return true;
                    }

                    operation = operation.Parent;
                }

                argumentOperation = null;
                return false;
            }

            private static bool IsReturn(IOperation? operation)
            {
                while (operation is not null)
                {
                    if (operation is IReturnOperation)
                    {
                        return true;
                    }

                    operation = operation.Parent;
                }

                return false;
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
                    .Where(ShouldTrackArgument)
                    .Select(argument => (collector: Clone(), argument))
                    .ToImmutableArray();

                var tasks = collectorsAndArgumentMap
                    .Select(pair => pair.collector.VisitAsync(pair.argument, cancellationToken));

                await Task.WhenAll(tasks).ConfigureAwait(false);

                var items = collectorsAndArgumentMap
                    .Select(pair => pair.collector.ProgressCollector)
                    .SelectMany(collector => collector.GetItems())
                    .Reverse(); // ProgressCollector uses a Stack, and we want to maintain the order by arguments, so reverse

                foreach (var item in items)
                {
                    ProgressCollector.Report(item);
                }
            }

            private OperationCollector Clone()
            {
                var collector = new ValueTrackingProgressCollector();
                collector.Parent = ProgressCollector.Parent;
                return new OperationCollector(collector, Solution);
            }

            private static bool ShouldTrackArgument(IArgumentOperation argumentOperation)
            {
                return argumentOperation.Parameter?.IsRefOrOut() == true
                    || argumentOperation.Value is IExpressionStatementOperation
                        or IBinaryOperation
                        or IInvocationOperation
                        or IParameterReferenceOperation
                        or ILiteralOperation;
            }
        }
    }
}
