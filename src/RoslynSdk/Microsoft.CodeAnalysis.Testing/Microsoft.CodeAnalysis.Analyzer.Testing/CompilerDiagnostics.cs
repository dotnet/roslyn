// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Specifies the behavior of compiler diagnostics in validation scenarios.
    /// </summary>
    public enum CompilerDiagnostics
    {
        /// <summary>
        /// All compiler diagnostics are ignored.
        /// </summary>
        None,

        /// <summary>
        /// Compiler errors are included in verification.
        /// </summary>
        Errors,

        /// <summary>
        /// Compiler errors and warnings are included in verification.
        /// </summary>
        Warnings,

        /// <summary>
        /// Compiler errors, warnings, and suggestions are included in verification.
        /// </summary>
        Suggestions,

        /// <summary>
        /// All compiler diagnostics are included in verification.
        /// </summary>
        All,
    }
}
