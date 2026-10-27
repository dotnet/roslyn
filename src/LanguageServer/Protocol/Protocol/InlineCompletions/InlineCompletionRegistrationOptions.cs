// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Inline completion options used during static or dynamic capability registration.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlineCompletionRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal sealed class InlineCompletionRegistrationOptions : InlineCompletionOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
{
    /// <inheritdoc/>
    [JsonPropertyName("documentSelector")]
    public DocumentFilter[]? DocumentSelector { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }
}
