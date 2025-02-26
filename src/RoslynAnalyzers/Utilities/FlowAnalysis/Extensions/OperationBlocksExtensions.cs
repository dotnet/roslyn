// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
