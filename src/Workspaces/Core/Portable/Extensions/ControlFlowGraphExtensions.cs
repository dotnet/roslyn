// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

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
            var lastBlock = cfg.Blocks[cfg.Blocks.Length - 1];
            Debug.Assert(lastBlock.Kind == BasicBlockKind.Exit);
            return lastBlock;
        }

        public static IEnumerable<IOperation> DescendantOperations(this ControlFlowGraph cfg)
        {
            foreach (BasicBlock block in cfg.Blocks)
            {
                foreach (IOperation operation in block.DescendantOperations())
                {
                    yield return operation;
                }
            }
        }

        public static IEnumerable<T> DescendantOperations<T>(this ControlFlowGraph cfg, OperationKind operationKind)
            where T : IOperation
        {
            Debug.Assert(cfg != null);

            foreach (var descendant in cfg.DescendantOperations())
            {
                if (descendant?.Kind == operationKind)
                {
                    yield return (T)descendant;
                }
            }
        }
    }
}
