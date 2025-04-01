// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    internal enum PropertySetAbstractValueKind
    {
        /// <summary>
        /// Doesn't matter.
        /// </summary>
        Unknown,

        /// <summary>
        /// Not flagged for badness.
        /// </summary>
        Unflagged,

        /// <summary>
        /// Flagged for badness.
        /// </summary>
        Flagged,

        /// <summary>
        /// Maybe flagged.
        /// </summary>
        MaybeFlagged,
    }
}
