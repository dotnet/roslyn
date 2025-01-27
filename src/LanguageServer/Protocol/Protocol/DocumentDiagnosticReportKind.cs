// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Value representing the kind of the document diagnostic report.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentDiagnosticReportKind">Language Server Protocol specification</see> for additional information.
/// </para>
/// <remarks>Since LSP 3.17</remarks>
/// </summary>
internal static class DocumentDiagnosticReportKind
{
    /// <summary>
    /// Kind representing a diagnostic report with a full set of problems.
    /// </summary>
    public const string Full = "full";

    /// <summary>
    /// Kind representing a diagnostic report indicating that the last returned report is still accurate.
    /// </summary>
    public const string Unchanged = "unchanged";
}
