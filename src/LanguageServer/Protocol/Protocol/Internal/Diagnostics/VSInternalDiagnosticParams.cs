// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing a diagnostic pull request parameter used.
/// </summary>
internal class VSInternalDiagnosticParams
{
    /// <summary>
    /// Gets or sets the document for which diagnostics are desired.
    /// </summary>
    [JsonPropertyName("_vs_textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier? TextDocument { get; set; }

    /// <summary>
    /// Gets or sets a value indicating what kind of diagnostic this request is querying for.
    /// </summary>
    [JsonPropertyName("_vs_queryingDiagnosticKind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VSInternalDiagnosticKind? QueryingDiagnosticKind { get; set; }

    /// <summary>
    /// Gets or sets the server-generated version number for the diagnostics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is treated as a black box by the client: it is stored on the client
    /// for each textDocument and sent back to the server when requesting
    /// diagnostics. The server can use this result ID to avoid resending
    /// diagnostics that had previously been sent.</para>
    ///
    /// <para>Note that if a client does request diagnostics that haven’t changed, the
    /// language server should not reply with any diagnostics for that document.
    /// If the client requests diagnostics for a file that has been renamed or
    /// deleted, then the language service should respond with null for the
    /// diagnostics.
    /// Also, if a service is reporting multiple DiagnosticReports for the same
    /// document, then all reports are expected to have the same
    /// previousResultId.</para>
    /// </remarks>
    [JsonPropertyName("_vs_previousResultId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PreviousResultId { get; set; }
}
