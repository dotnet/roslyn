// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Determines the kind of interprocedural dataflow analysis to perform for method invocations.
    /// </summary>
    public enum InterproceduralAnalysisKind
    {
        /// <summary>
        /// Skip interprocedural analysis for source method invocations, except for lambda and location function invocations,
        /// which are always analyzed in a context sensitive fashion.
        /// All the analysis data for invocation receiver and arguments is conservatively reset at call sites.
        /// Produces least precise results from amongst the possible interprocedural modes, but is the most performant mode.
        /// </summary>
        None,

        /* NOT YET IMPLEMENTED: https://github.com/dotnet/roslyn-analyzers/issues/1810
        /// <summary>
        /// Perform non-context sensitive interprocedural analysis for source method invocations,
        /// except for lambda and location function invocations, which are always analyzed in a context sensitive fashion.
        /// Non-context sensitive interprocedural analysis analyzes invoked method without considering the calling context,
        /// i.e. the argument values and instance receiver, hence is less precise then ContextSensitive mode.
        /// Analysis result from such an interprocedural analysis is applied at every call site, hence the analysis results
        /// are more precise then None mode.
        /// This mode is generally much less performance intensive then the ContextSensitive mode,
        /// but more intensive then None mode.
        /// </summary>
        NonContextSensitive,
        */

        /// <summary>
        /// Performs context sensitive interprocedural analysis for all source method invocations,
        /// including lambda and location function invocations.
        /// Context sensitive interprocedural analysis analyzes invoked method at each call site by considering the calling context,
        /// i.e. the argument values and instance receiver, hence is the most precise analysis mode.
        /// This mode is also the most performance intensive mode.
        /// Note that we apply a max threshold to the length of interprocedural call chain to analyze in
        /// a context sensitive fashion to avoid infinite computation for huge call graphs.
        /// </summary>
        ContextSensitive
    }
}
