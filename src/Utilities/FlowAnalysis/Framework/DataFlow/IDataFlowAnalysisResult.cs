// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Marker interface for analysis results from execution of <see cref="DataFlowAnalysis"/> on a control flow graph.
    /// Primarily exists for specifying constraints on analysis result type parameters.
    /// </summary>
    internal interface IDataFlowAnalysisResult
    {
    }
}
