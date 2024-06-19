// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// A diagnostic report indicating that the last returned report is still accurate.
/// A server can only return this if result ids are provided.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#unchangedDocumentDiagnosticReport">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
[Kind(DocumentDiagnosticReportKind.Unchanged)]
internal class UnchangedDocumentDiagnosticReport
{
    /// <summary>
    /// Gets the kind of this report.
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonRequired]
#pragma warning disable CA1822 // Mark members as static
    public string Kind => DocumentDiagnosticReportKind.Unchanged;
#pragma warning restore CA1822 // Mark members as static

    /// <summary>
    /// A result id which will be sent on the next
    /// diagnostic request for the same document.
    /// </summary>
    [JsonPropertyName("resultId")]
    [JsonRequired]
    public string ResultId
    {
        get;
        set;
    }
}
