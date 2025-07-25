// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.FlowAnalysis;

internal static partial class ControlFlowGraphExtensions
{
    extension(ControlFlowGraph cfg)
    {
        public BasicBlock EntryBlock()
        {
            var firstBlock = cfg.Blocks[0];
            Debug.Assert(firstBlock.Kind == BasicBlockKind.Entry);
            return firstBlock;
        }

        public BasicBlock ExitBlock()
        {
            var lastBlock = cfg.Blocks.Last();
            Debug.Assert(lastBlock.Kind == BasicBlockKind.Exit);
            return lastBlock;
        }

        public IEnumerable<IOperation> DescendantOperations()
            => cfg.Blocks.SelectMany(b => b.DescendantOperations());

        public IEnumerable<T> DescendantOperations<T>(OperationKind operationKind)
            where T : IOperation
            => cfg.DescendantOperations().Where(d => d?.Kind == operationKind).Cast<T>();
    }
}
