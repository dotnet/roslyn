// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters sent for the textDocument/validateBreakableRange request.
/// </summary>
/// <remarks>Do not seal this type! This is extended by Razor</remarks>
internal class VSInternalValidateBreakableRangeParams : ITextDocumentParams
{
    /// <summary>
    /// Gets or sets the <see cref="TextDocumentIdentifier"/> for the request.
    /// </summary>
    [JsonPropertyName("_vs_textDocument")]
    public TextDocumentIdentifier TextDocument { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Range"/> at which the request was sent.
    /// </summary>
    [JsonPropertyName("_vs_range")]
    public Range Range { get; set; }
}
