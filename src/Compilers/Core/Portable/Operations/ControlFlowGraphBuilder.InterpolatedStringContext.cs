// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal partial class ControlFlowGraphBuilder
    {
        private class InterpolatedStringHandlerArgumentsContext
        {
            public readonly ImmutableArray<IInterpolatedStringHandlerCreationOperation> ApplicableCreationOperations;
            public readonly int StartingStackDepth;
            public readonly bool HasReceiver;

            public InterpolatedStringHandlerArgumentsContext(ImmutableArray<IInterpolatedStringHandlerCreationOperation> applicableCreationOperations, int startingStackDepth, bool hasReceiver)
            {
                ApplicableCreationOperations = applicableCreationOperations;
                HasReceiver = hasReceiver;
                StartingStackDepth = startingStackDepth;
            }
        }

        private class InterpolatedStringHandlerCreationContext
        {
            public readonly IInterpolatedStringHandlerCreationOperation ApplicableCreationOperation;
            public readonly int MaximumStackDepth;
            public readonly int HandlerPlaceholder;
            public readonly int OutPlaceholder;

            public InterpolatedStringHandlerCreationContext(IInterpolatedStringHandlerCreationOperation applicableCreationOperation, int maximumStackDepth, int handlerPlaceholder, int outParameterPlaceholder)
            {
                ApplicableCreationOperation = applicableCreationOperation;
                MaximumStackDepth = maximumStackDepth;
                OutPlaceholder = outParameterPlaceholder;
                HandlerPlaceholder = handlerPlaceholder;
            }
        }

        [Conditional("DEBUG")]
        [MemberNotNull(nameof(_currentInterpolatedStringHandlerCreationContext))]
        private void AssertContainingContextIsForThisCreation(IOperation placeholderOperation, bool assertArgumentContext)
        {
            Debug.Assert(_currentInterpolatedStringHandlerCreationContext != null);
            Debug.Assert(placeholderOperation is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.InterpolatedStringHandler } or IInterpolatedStringHandlerArgumentPlaceholderOperation);

            IOperation? operation = placeholderOperation.Parent;
            while (operation is not (null or IInterpolatedStringHandlerCreationOperation))
            {
                operation = operation.Parent;
            }

            Debug.Assert(operation != null);
            Debug.Assert(_currentInterpolatedStringHandlerCreationContext.ApplicableCreationOperation == operation);

            // Note: _currentInterpolatedStringHandlerArgumentContext may be null in error scenarios
            // (for example, indexer with no setter in object initializer where the handler creation is visited
            // as a child of an InvalidOperation rather than through VisitAndPushArguments).
            Debug.Assert(!assertArgumentContext || _currentInterpolatedStringHandlerArgumentContext != null || placeholderOperation.Parent!.Parent!.HasErrors(_compilation));
            if (assertArgumentContext && _currentInterpolatedStringHandlerArgumentContext != null)
            {
                Debug.Assert(_currentInterpolatedStringHandlerArgumentContext.ApplicableCreationOperations.Contains((IInterpolatedStringHandlerCreationOperation)operation));
            }
        }
    }
}
