// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Subclass of <see cref="InlineValueOptions"/> that allows scoping the registration.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlineValueRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class InlineValueRegistrationOptions : InlineValueOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
{
    /// <inheritdoc/>
    [JsonPropertyName("documentSelector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentFilter[]? DocumentSelector { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }
}
