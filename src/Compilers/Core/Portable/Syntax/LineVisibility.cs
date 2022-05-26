// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The state of the visibility of a line.
    /// </summary>
    public enum LineVisibility
    {
        /// <summary>
        /// The line is located before any #line directive and there is at least one #line directive present in this syntax tree.
        /// This enum value is used for C# only to enable the consumer to define how to interpret the lines before the first
        /// line directive. 
        /// </summary>
        BeforeFirstLineDirective = 0,

        /// <summary>
        /// The line is following a #line hidden directive.
        /// </summary>
        Hidden = 1,

        /// <summary>
        /// The line is following a #line default directive or a #line directive with at least a line number.
        /// If there is no line directive at all, Visible is returned for all lines.
        /// </summary>
        Visible = 2
    }
}
