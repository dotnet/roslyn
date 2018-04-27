// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.Extensions
{
    internal static class ControlFlowGraphExtensions
    {
        public static BasicBlock GetEntry(this ControlFlowGraph cfg) => cfg.Blocks.Single(b => b.Kind == BasicBlockKind.Entry);
        public static BasicBlock GetExit(this ControlFlowGraph cfg) => cfg.Blocks.Single(b => b.Kind == BasicBlockKind.Exit);
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
