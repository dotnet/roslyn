// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Kind for <see cref="TaintedDataAbstractValue"/>.
    /// </summary>
    internal enum TaintedDataAbstractValueKind
    {
        /// <summary>
        /// Indicates the data is definitely untainted (cuz it was sanitized).
        /// </summary>
        NotTainted,

        /// <summary>
        /// Indicates that data is definitely tainted.
        /// </summary>
        Tainted,
    }
}
