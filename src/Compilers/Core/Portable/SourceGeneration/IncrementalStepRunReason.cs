// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The state of the output of a given executed incremental source generator step.
    /// </summary>
    public enum IncrementalStepRunReason
    {
        /// <summary>
        /// The input to this step was added or modified from a previous run, and it produced a new output.
        /// </summary>
        New,

        /// <summary>
        /// The input to this step was modified from a previous run, and it produced a different value than the previous run.
        /// </summary>
        Modified,

        /// <summary>
        /// The input to this step was modified from a previous run, but it produced an equal value to the previous run.
        /// </summary>
        Unchanged,

        /// <summary>
        /// The output of this step was pulled from this step's cache since the inputs was unchanged from the previous run.
        /// </summary>
        Cached,

        /// <summary>
        /// The input to this step was removed or modified from a previous run, and the output it used to generate is no longer present.
        /// </summary>
        Removed
    }
}
