// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Defines options for interpreting <see cref="DiagnosticLocation"/>.
    /// </summary>
    [Flags]
    public enum DiagnosticLocationOptions
    {
        /// <summary>
        /// The diagnostic location is a simple <see cref="FileLinePositionSpan"/>.
        /// </summary>
        None = 0,

        /// <summary>
        /// The diagnostic location is defined as a position instead of a span. The length of the actual diagnostic span
        /// should be ignored when comparing results.
        /// </summary>
        IgnoreLength = 1,

        /// <summary>
        /// The diagnostic location is defined in markup. The associated <see cref="DiagnosticLocation"/> has the
        /// following characteristics:
        ///
        /// <list type="bullet">
        /// <item><description>The <see cref="FileLinePositionSpan.Path"/> is an empty string.</description></item>
        /// <item><description>The <see cref="LinePosition.Line"/> is 0.</description></item>
        /// <item><description>The <see cref="LinePosition.Character"/> is the index of the markup span which defines
        /// the location. For example, an index of <c>5</c> would appear using the markup syntax <c>{|#5:...|}</c> or
        /// <c>{|#5:...|#5}</c>.</description></item>
        /// </list>
        /// </summary>
        InterpretAsMarkupKey = 2,

        /// <summary>
        /// The diagnostic location is marked as unnecessary code.
        /// </summary>
        UnnecessaryCode = 4,
    }
}
