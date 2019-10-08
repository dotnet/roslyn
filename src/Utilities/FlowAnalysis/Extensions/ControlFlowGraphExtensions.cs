// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
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

        internal static bool SupportsFlowAnalysis(this ControlFlowGraph cfg)
        {
            // Skip flow analysis for following root operation blocks:
            // 1. Null root operation (error case)
            // 2. OperationKind.None (used for attributes).
            // 3. OperationKind.ParameterInitialzer (default parameter values).
            if (cfg.OriginalOperation == null ||
                cfg.OriginalOperation.Kind == OperationKind.None ||
                cfg.OriginalOperation.Kind == OperationKind.ParameterInitializer)
            {
                return false;
            }

            // Skip flow analysis for code with syntax/semantic errors
            if (cfg.OriginalOperation.Syntax.GetDiagnostics().Any(d => d.DefaultSeverity == DiagnosticSeverity.Error) ||
                cfg.OriginalOperation.HasAnyOperationDescendant(o => o is IInvalidOperation))
            {
                return false;
            }

            return true;
        }
    }
}
