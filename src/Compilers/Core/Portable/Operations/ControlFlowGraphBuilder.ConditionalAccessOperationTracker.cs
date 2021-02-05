// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal sealed partial class ControlFlowGraphBuilder
    {
        private readonly struct ConditionalAccessOperationTracker
        {
            public readonly ArrayBuilder<IOperation>? Operations;
            public readonly BasicBlockBuilder? WhenNull;

            public ConditionalAccessOperationTracker(ArrayBuilder<IOperation> operations, BasicBlockBuilder whenNull)
            {
                Debug.Assert(operations != null && whenNull != null);
                Operations = operations;
                WhenNull = whenNull;
            }

            [MemberNotNullWhen(false, nameof(Operations), nameof(WhenNull))]
            public bool IsDefault => Operations == null;

            public void Free()
            {
                Operations?.Free();
            }
        }
    }
}
