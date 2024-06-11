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
    internal static partial class ValueTracker
    {
        private class OperationCollector(ValueTrackingProgressCollector progressCollector, Solution solution)
        {
            public ValueTrackingProgressCollector ProgressCollector { get; } = progressCollector;
            public Solution Solution { get; } = solution;

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
                    IMethodBodyOperation methodBodyOperation => VisitReturnDescendentsAsync(methodBodyOperation, allowImplicit: true, cancellationToken),
                    IBlockOperation blockOperation => VisitReturnDescendentsAsync(blockOperation, allowImplicit: false, cancellationToken),

                    // Default to reporting if there is symbol information available
                    _ => VisitDefaultAsync(operation, cancellationToken)
                };

            private async Task VisitReturnDescendentsAsync(IOperation operation, bool allowImplicit, CancellationToken cancellationToken)
            {
                var returnOperations = operation.Descendants().Where(d => d is IReturnOperation && (allowImplicit || !d.IsImplicit));
                foreach (var returnOperation in returnOperations)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await VisitAsync(returnOperation, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task VisitDefaultAsync(IOperation operation, CancellationToken cancellationToken)
            {
                // If an operation has children, desend in them by default. 
                // For cases that should not be descendend into, they should be explicitly handled
                // in VisitAsync. 
                // Ex: Binary operation of [| x + y |]
                // both x and y should be evaluated separately
                var childrenVisited = await TryVisitChildrenAsync(operation, cancellationToken).ConfigureAwait(false);

                // In cases where the child operations were visited, they would be added instead of the parent
                // currently being evaluated. Do not add the parent as well since it would be redundent. 
                // Ex: Binary operation of [| x + y |]
                // both x and y should be evaluated separately, but the whole operation should not be reported
                if (childrenVisited)
                {
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

            private async Task<bool> TryVisitChildrenAsync(IOperation operation, CancellationToken cancellationToken)
            {
                foreach (var child in operation.ChildOperations)
                {
                    await VisitAsync(child, cancellationToken).ConfigureAwait(false);
                }

                return operation.ChildOperations.Any();
            }

            private Task VisitAssignmentOperationAsync(IAssignmentOperation assignmentOperation, CancellationToken cancellationToken)
                => VisitAsync(assignmentOperation.Value, cancellationToken);

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

                if (IsContainedIn<IArgumentOperation>(operation, out var argumentOperation) && argumentOperation.Parameter is not null)
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

                if (IsContainedIn<IReturnOperation>(operation) || IsContainedIn<IAssignmentOperation>(operation))
                {
                    // If the reference is part of a return operation or assignment operation we want to track where the values come from
                    // since they contribute to the "output" of the method/assignment and are relavent for value tracking.
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

            private Task VisitLiteralAsync(ILiteralOperation literalOperation, CancellationToken cancellationToken)
            {
                if (literalOperation.Type is null)
                {
                    return Task.CompletedTask;
                }

                return AddOperationAsync(literalOperation, literalOperation.Type, cancellationToken);
            }

            private Task VisitReturnAsync(IReturnOperation returnOperation, CancellationToken cancellationToken)
            {
                if (returnOperation.ReturnedValue is null)
                {
                    return Task.CompletedTask;
                }

                return VisitAsync(returnOperation.ReturnedValue, cancellationToken);
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
                    // Clone the collector here to allow each argument to report multiple items.
                    // See Clone() docs for more details
                    .Select(argument => (collector: Clone(), argument))
                    .ToImmutableArray();

                var tasks = collectorsAndArgumentMap
                    .Select(pair => Task.Run(() => pair.collector.VisitAsync(pair.argument, cancellationToken)));

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

            /// <summary>
            /// Clone the current collector into a new one with
            /// the same parent but a separate progress collector.
            /// This allows collection of items given the same state
            /// as this collector while also keeping them "grouped" separately.
            /// </summary>
            /// <remarks>
            /// This is useful for cases such as tracking arguments, where each
            /// argument may be an expression or something else. We want to track each
            /// argument expression in the correct order, but a single argument may produce
            /// multiple items. By cloning we can track the items for each argument and then
            /// gather them all at the end to report in the correct order.
            /// </remarks>
            private OperationCollector Clone()
            {
                var collector = new ValueTrackingProgressCollector
                {
                    Parent = ProgressCollector.Parent
                };
                return new OperationCollector(collector, Solution);
            }

            private static bool ShouldTrackArgument(IArgumentOperation argumentOperation)
            {
                // Ref or Out arguments always contribute data as "assignments"
                // across method calls
                if (argumentOperation.Parameter?.IsRefOrOut() == true)
                {
                    return true;
                }

                // If the argument value is an expression, binary operation, or
                // invocation then parts of the operation need to be evaluated
                // to see if they contribute data for value tracking
                if (argumentOperation.Value is IExpressionStatementOperation
                        or IBinaryOperation
                        or IInvocationOperation)
                {
                    return true;
                }

                // If the argument value is a parameter reference, then the method calls
                // leading to that parameter value should be tracked as well.
                // Ex:
                // string Prepend(string s1) => "pre" + s1;
                // string CallPrepend(string [|s2|]) => Prepend(s2);
                // Tracking [|s2|] into calls as an argument means that we 
                // need to know where [|s2|] comes from and how it contributes
                // to the value s1
                if (argumentOperation.Value is IParameterReferenceOperation)
                {
                    return true;
                }

                // A literal value as an argument is a dead end for data, but still contributes
                // to a value and should be shown in value tracking. It should never expand
                // further though. 
                // Ex:
                // string Prepend(string [|s|]) => "pre" + s;
                // string DefaultPrepend() => Prepend("default");
                // [|s|] is the parameter we need to track values for, which 
                // is assigned to "default" in DefaultPrepend
                if (argumentOperation.Value is ILiteralOperation)
                {
                    return true;
                }

                return false;
            }

            private static bool IsContainedIn<TContainingOperation>(IOperation? operation) where TContainingOperation : IOperation
                => IsContainedIn<TContainingOperation>(operation, out var _);

            private static bool IsContainedIn<TContainingOperation>(IOperation? operation, [NotNullWhen(returnValue: true)] out TContainingOperation? containingOperation) where TContainingOperation : IOperation
            {
                while (operation is not null)
                {
                    if (operation is TContainingOperation tmpOperation)
                    {
                        containingOperation = tmpOperation;
                        return true;
                    }

                    operation = operation.Parent;
                }

                containingOperation = default;
                return false;
            }
        }
    }
}
