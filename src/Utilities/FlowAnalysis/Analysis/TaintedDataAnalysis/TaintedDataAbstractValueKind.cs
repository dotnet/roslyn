// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
