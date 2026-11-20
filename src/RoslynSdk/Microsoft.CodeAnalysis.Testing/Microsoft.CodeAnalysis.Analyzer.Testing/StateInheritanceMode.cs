// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Indicates the manner in which properties are inherited from base test states.
    /// </summary>
    /// <seealso cref="SolutionState.InheritanceMode"/>
    public enum StateInheritanceMode
    {
        /// <summary>
        /// The contents of the <see cref="SolutionState"/> may be explicitly specified, but unspecified elements of
        /// partially-specified state instances are inherited from another source. Fixable diagnostics are not
        /// inherited.
        /// </summary>
        AutoInherit,

        /// <summary>
        /// The contents of the <see cref="SolutionState"/> are fully and explicitly specified.
        /// </summary>
        Explicit,

        /// <summary>
        /// The contents of the <see cref="SolutionState"/> may be explicitly specified, but unspecified elements of
        /// partially-specified state instances are inherited from another source. All diagnostics, including fixable
        /// diagnostics, are inherited.
        /// </summary>
        AutoInheritAll,
    }
}
