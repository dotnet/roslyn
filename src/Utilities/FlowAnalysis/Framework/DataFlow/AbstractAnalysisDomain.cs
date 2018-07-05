// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Abstract analysis domain for a <see cref="DataFlowAnalysis"/> to merge and compare analysis data.
    /// </summary>
    internal abstract class AbstractAnalysisDomain<TAnalysisData> : AbstractDomain<TAnalysisData>
    {
        /// <summary>
        /// Creates a clone of the analysis data.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract TAnalysisData Clone(TAnalysisData value);
    }
}
