// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing a diagnostic pull request result for all documents.
/// </summary>
internal sealed class VSInternalWorkspaceDiagnosticReport : VSInternalDiagnosticReport
{
    /// <summary>
    /// Gets or sets the document for which diagnostics is returned.
    /// </summary>
    [JsonPropertyName("_vs_textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier? TextDocument { get; set; }
}
