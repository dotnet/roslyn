// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Diagnostic severity enum.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#diagnosticSeverity">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal enum DiagnosticSeverity
    {
        /// <summary>
        /// Error.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Warning.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Information.
        /// </summary>
        Information = 3,

        /// <summary>
        /// Hint.
        /// </summary>
        Hint = 4,
    }
}
