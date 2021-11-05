// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal partial class ControlFlowGraphBuilder
    {
        private readonly struct InterpolatedStringHandlerContext
        {
            private const byte IsNotDefaultFlag = 1 << 0;
            private const byte HasReceiverFlag = 1 << 1;

            private readonly ImmutableArray<IArgumentOperation> _allowedCreations;
            private readonly IOperation? _singleAllowedCreation;
            private readonly byte _flags;
            private readonly int _outPlaceholder;
            private readonly int _handlerPlaceholder;

            public readonly int StartingStackDepth;

            public InterpolatedStringHandlerContext(ImmutableArray<IArgumentOperation> allowedCreations, int startingStackDepth, bool hasReceiver)
            {
                _allowedCreations = allowedCreations;
                _singleAllowedCreation = null;
                StartingStackDepth = startingStackDepth;
                _outPlaceholder = -1;
                _handlerPlaceholder = -1;
                _flags = (byte)((hasReceiver ? HasReceiverFlag : 0) | IsNotDefaultFlag);
            }

            public InterpolatedStringHandlerContext(IOperation allowedCreation, int outPlaceholder, int handlerPlaceholder)
            {
                _allowedCreations = default;
                _singleAllowedCreation = allowedCreation;
                StartingStackDepth = -1;
                _outPlaceholder = outPlaceholder;
                _flags = IsNotDefaultFlag;
                _handlerPlaceholder = handlerPlaceholder;
            }

            public bool IsDefault => (_flags & IsNotDefaultFlag) == 0;
            public bool HasReceiver => (_flags & HasReceiverFlag) == HasReceiverFlag;

            public int OutPlaceholder
            {
                get => _outPlaceholder;
                init
                {
                    Debug.Assert(!IsDefault);
                    _outPlaceholder = value;
                }
            }

            public int HandlerPlaceholder
            {
                get => _handlerPlaceholder;
                init
                {
                    Debug.Assert(!IsDefault);
                    _handlerPlaceholder = value;
                }
            }

            public IOperation SingleAllowedCreation
            {
                init
                {
                    Debug.Assert(!IsDefault);
                    _allowedCreations = default;
                    _singleAllowedCreation = value;
                }
            }

            public bool IsCreationAllowed(IInterpolatedStringHandlerCreationOperation operation)
            {
                if (IsDefault)
                {
                    return false;
                }

                Debug.Assert((_singleAllowedCreation == null) ^ _allowedCreations.IsDefault);
                return _singleAllowedCreation != null
                            ? _singleAllowedCreation == operation
                            : _allowedCreations.Any(static (argument, operation) => argument.Value == operation, operation);
            }
        }

        private static void AssertContainingContextIsForThisCreation(IOperation placeholderOperation, InterpolatedStringHandlerContext context)
        {
            Debug.Assert(!context.IsDefault);
            Debug.Assert(placeholderOperation is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.InterpolatedStringHandler } or IInterpolatedStringHandlerArgumentPlaceholderOperation);

            IOperation? operation = placeholderOperation.Parent;
            while (operation is not (null or IInterpolatedStringHandlerCreationOperation))
            {
                operation = operation.Parent;
            }

            Debug.Assert(operation != null);
            Debug.Assert(context.IsCreationAllowed((IInterpolatedStringHandlerCreationOperation)operation));
        }
    }
}
