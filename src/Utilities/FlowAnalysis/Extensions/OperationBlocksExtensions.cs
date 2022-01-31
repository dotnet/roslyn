// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static partial class OperationBlocksExtensions
    {
        public static ControlFlowGraph? GetControlFlowGraph(this ImmutableArray<IOperation> operationBlocks)
        {
            foreach (var operationRoot in operationBlocks)
            {
                if (operationRoot.TryGetEnclosingControlFlowGraph(out var cfg))
                {
                    return cfg;
                }
            }

            return null;
        }
    }
}
