// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

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
    }
}
