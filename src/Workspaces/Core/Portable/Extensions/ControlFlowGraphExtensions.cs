// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal static partial class ControlFlowGraphExtensions
    {
        public static BasicBlock EntryBlock(this ControlFlowGraph cfg)
        {
            var firstBlock = cfg.Blocks[0];
            Debug.Assert(firstBlock.Kind == BasicBlockKind.Entry);
            return firstBlock;
        }

        public static BasicBlock ExitBlock(this ControlFlowGraph cfg)
        {
            var lastBlock = cfg.Blocks.Last();
            Debug.Assert(lastBlock.Kind == BasicBlockKind.Exit);
            return lastBlock;
        }

        public static IEnumerable<IOperation> DescendantOperations(this ControlFlowGraph cfg)
            => cfg.Blocks.SelectMany(b => b.DescendantOperations());

        public static IEnumerable<T> DescendantOperations<T>(this ControlFlowGraph cfg, OperationKind operationKind)
            where T : IOperation
            => cfg.DescendantOperations().Where(d => d?.Kind == operationKind).Cast<T>();
    }
}
