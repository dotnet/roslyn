// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Result of evaluating potentially hazardous usage.
    /// </summary>
    public enum HazardousUsageEvaluationResult
    {
        /// <summary>
        /// The usage is not hazardous.
        /// </summary>
        Unflagged,

        /// <summary>
        /// The usage might be hazardous.
        /// </summary>
        MaybeFlagged,

        /// <summary>
        /// The usage is definitely hazardous.
        /// </summary>
        Flagged,
    }
}
