// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.Extensions
{
    internal static partial class OperationBlocksExtensions
    {
        public static ControlFlowGraph? GetControlFlowGraph(this ImmutableArray<IOperation> operationBlocks)
        {
            foreach (var operationRoot in operationBlocks)
            {
                IBlockOperation? topmostBlock = operationRoot.GetTopmostParentBlock();
                if (topmostBlock != null)
                {
                    return topmostBlock.GetEnclosingControlFlowGraph();
                }
            }

            return null;
        }
    }
}
