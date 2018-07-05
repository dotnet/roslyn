// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis
{
    /// <summary>
    /// Kind for the <see cref="CopyAbstractValue"/>.
    /// </summary>
    internal enum CopyAbstractValueKind
    {
        /// <summary>
        /// Not applicable.
        /// </summary>
        NotApplicable,

        /// <summary>
        /// Copy shared by one or more <see cref="AnalysisEntity"/> instances.
        /// </summary>
        Known,

        /// <summary>
        /// Copy may or may not be shared by other <see cref="AnalysisEntity"/> instances.
        /// </summary>
        Unknown,

        /// <summary>
        /// Invalid state for an unreachable path from predicate analysis.
        /// </summary>
        Invalid,
    }
}
