// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing a diagnostic report with a full set of problems.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fullDocumentDiagnosticReport">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
[Kind(DocumentDiagnosticReportKind.Full)]
internal class FullDocumentDiagnosticReport
{
    /// <summary>
    /// Gets the kind of this report.
    /// </summary>
    [JsonPropertyName("kind")]
#pragma warning disable CA1822 // Mark members as static
    public string Kind => DocumentDiagnosticReportKind.Full;
#pragma warning restore CA1822 // Mark members as static

    /// <summary>
    /// An optional result id. If provided it will
    /// be sent on the next diagnostic request for the
    /// same document.
    /// </summary>
    [JsonPropertyName("resultId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResultId
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the diagnostics in this report.
    /// </summary>
    [JsonPropertyName("items")]
    public Diagnostic[] Items
    {
        get;
        set;
    }
}
