// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Flags to indicate which types of compiler diagnostics are reported by <see cref="DiagnosticServices"/>
    /// </summary>
    [Flags]
    public enum DiagnosticServiceOptions
    {
        /// <summary>
        /// Include syntax errors
        /// </summary>
        Syntax = 0x01,

        /// <summary>
        /// Include semantic errors
        /// </summary>
        Semantic = 0x02,
    }
}
