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
        /// The output of this step is a new output produced from a new input.
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
        /// The input that this output is generated from was removed from the input step's outputs, so this value will be removed from the output step results.
        /// </summary>
        Removed
    }
}
