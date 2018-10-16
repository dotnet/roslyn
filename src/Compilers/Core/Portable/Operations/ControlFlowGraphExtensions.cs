// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public static ControlFlowGraph GetLocalFunctionControlFlowGraphInScope(this ControlFlowGraph controlFlowGraph, IMethodSymbol localFunction, CancellationToken cancellationToken = default)
        {
            if (controlFlowGraph == null)
            {
                throw new ArgumentNullException(nameof(controlFlowGraph));
            }

            if (localFunction == null)
            {
                throw new ArgumentNullException(nameof(localFunction));
            }

            do
            {
                if (controlFlowGraph.TryGetLocalFunctionControlFlowGraph(localFunction, cancellationToken, out ControlFlowGraph localFunctionControlFlowGraph))
                {
                    return localFunctionControlFlowGraph;
                }
            }
            while ((controlFlowGraph = controlFlowGraph.Parent) != null);

            throw new ArgumentOutOfRangeException(nameof(localFunction));
        }

        /// <summary>
        /// Gets or creates a control flow graph for the given <paramref name="anonymousFunction"/> defined in
        /// the given <paramref name="controlFlowGraph"/> or any of it's parent control flow graphs.
        /// </summary>
        public static ControlFlowGraph GetAnonymousFunctionControlFlowGraphInScope(this ControlFlowGraph controlFlowGraph, IFlowAnonymousFunctionOperation anonymousFunction, CancellationToken cancellationToken = default)
        {
            if (controlFlowGraph == null)
            {
                throw new ArgumentNullException(nameof(controlFlowGraph));
            }

            if (anonymousFunction == null)
            {
                throw new ArgumentNullException(nameof(anonymousFunction));
            }

            do
            {
                if (controlFlowGraph.TryGetAnonymousFunctionControlFlowGraph(anonymousFunction, cancellationToken, out ControlFlowGraph localFunctionControlFlowGraph))
                {
                    return localFunctionControlFlowGraph;
                }
            }
            while ((controlFlowGraph = controlFlowGraph.Parent) != null);

            throw new ArgumentOutOfRangeException(nameof(anonymousFunction));
        }
    }
}
