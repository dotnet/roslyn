// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Diagnostic tag enum.
/// Additional metadata about the type of a diagnostic
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#diagnosticTag">Language Server Protocol specification</see> for additional information.
/// </summary>
internal enum DiagnosticTag
{
    /// <summary>
    /// Unused or unnecessary code.
    /// Clients are allowed to render diagnostics with this tag faded out.
    /// </summary>
    Unnecessary = 1,

    /// <summary>
    /// Deprecated or obsolete code.
    /// Clients are allowed to render diagnostics with this tag strike through.
    /// </summary>
    Deprecated = 2,
}
