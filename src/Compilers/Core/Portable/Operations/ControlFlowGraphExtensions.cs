// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    public static partial class ControlFlowGraphExtensions
    {
        /// <summary>
        /// Gets or creates a control flow graph for the given <paramref name="localFunction"/> defined in
        /// the given <paramref name="controlFlowGraph"/> or any of it's parent control flow graphs.
        /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
        public static ControlFlowGraph GetLocalFunctionControlFlowGraphInScope(this ControlFlowGraph controlFlowGraph, IMethodSymbol localFunction, CancellationToken cancellationToken = default)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (controlFlowGraph == null)
            {
                throw new ArgumentNullException(nameof(controlFlowGraph));
            }

            if (localFunction == null)
            {
                throw new ArgumentNullException(nameof(localFunction));
            }

            ControlFlowGraph? currentGraph = controlFlowGraph;
            do
            {
                if (currentGraph.TryGetLocalFunctionControlFlowGraph(localFunction, out ControlFlowGraph? localFunctionControlFlowGraph))
                {
                    return localFunctionControlFlowGraph;
                }
            }
            while ((currentGraph = currentGraph.Parent) != null);

            throw new ArgumentOutOfRangeException(nameof(localFunction));
        }

        /// <summary>
        /// Gets or creates a control flow graph for the given <paramref name="anonymousFunction"/> defined in
        /// the given <paramref name="controlFlowGraph"/> or any of it's parent control flow graphs.
        /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
        public static ControlFlowGraph GetAnonymousFunctionControlFlowGraphInScope(this ControlFlowGraph controlFlowGraph, IFlowAnonymousFunctionOperation anonymousFunction, CancellationToken cancellationToken = default)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (controlFlowGraph == null)
            {
                throw new ArgumentNullException(nameof(controlFlowGraph));
            }

            if (anonymousFunction == null)
            {
                throw new ArgumentNullException(nameof(anonymousFunction));
            }

            ControlFlowGraph? currentGraph = controlFlowGraph;
            do
            {
                if (currentGraph.TryGetAnonymousFunctionControlFlowGraph(anonymousFunction, out ControlFlowGraph? localFunctionControlFlowGraph))
                {
                    return localFunctionControlFlowGraph;
                }
            }
            while ((currentGraph = currentGraph.Parent) != null);

            throw new ArgumentOutOfRangeException(nameof(anonymousFunction));
        }
    }
}
