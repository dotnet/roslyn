// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
