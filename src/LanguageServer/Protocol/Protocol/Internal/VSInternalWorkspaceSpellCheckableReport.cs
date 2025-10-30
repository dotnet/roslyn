// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Report for workspace spell checkable range request.
/// </summary>
internal sealed class VSInternalWorkspaceSpellCheckableReport : VSInternalSpellCheckableRangeReport, ITextDocumentParams
{
    /// <summary>
    /// Gets or sets the document for which the spell checkable ranges are returned.
    /// </summary>
    [JsonPropertyName("_vs_textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument { get; set; }
}
