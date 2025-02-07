// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Marker interface for analysis contexts for execution of <see cref="DataFlowAnalysis"/> on a control flow graph.
    /// Primarily exists for specifying constraints on analysis context type parameters.
    /// </summary>
    public interface IDataFlowAnalysisContext
    {
        ControlFlowGraph ControlFlowGraph { get; }
        ISymbol OwningSymbol { get; }
        ControlFlowGraph? GetLocalFunctionControlFlowGraph(IMethodSymbol localFunction);
        ControlFlowGraph? GetAnonymousFunctionControlFlowGraph(IFlowAnonymousFunctionOperation lambda);
    }
}
