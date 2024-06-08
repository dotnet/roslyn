// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing a diagnostic report indicating that the last returned report is still accurate.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#unchangedDocumentDiagnosticReport">Language Server Protocol specification</see> for additional information.
/// </summary>
[Kind(DocumentDiagnosticReportKind.Unchanged)]
internal class UnchangedDocumentDiagnosticReport
{
    /// <summary>
    /// Gets the kind of this report.
    /// </summary>
    [JsonPropertyName("kind")]
#pragma warning disable CA1822 // Mark members as static
    public string Kind => DocumentDiagnosticReportKind.Unchanged;
#pragma warning restore CA1822 // Mark members as static

    /// <summary>
    /// Gets or sets the optional result id.
    /// </summary>
    [JsonPropertyName("resultId")]
    public string ResultId
    {
        get;
        set;
    }
}
