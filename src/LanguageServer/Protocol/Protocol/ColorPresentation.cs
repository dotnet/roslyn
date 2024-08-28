// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A color presentation for a textDocument/colorPresentation response.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#colorPresentation">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.6</remarks>
internal class ColorPresentation
{
    /// <summary>
    /// The label of this color presentation. It will be shown on the color picker header.
    /// <para>
    /// By default this is also the text that is inserted when selecting this color presentation.
    /// </para>
    /// </summary>
    [JsonPropertyName("label")]
    [JsonRequired]
    public string Label { get; init; }

    /// <summary>
    /// A <see cref="Protocol.TextEdit"/> which is applied to a document when selecting this presentation for the color.
    /// <para>
    /// When omitted the [label](#ColorPresentation.label) is used.
    /// </para>
    /// </summary>
    [JsonPropertyName("textEdit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextEdit? TextEdit { get; init; }

    /// <summary>
    /// An optional array of additional <see cref="Protocol.TextEdit"/> that are applied
    /// when selecting this color presentation.
    /// <para>
    /// Edits must not overlap with the main <see cref="TextEdit"/> nor with themselves.
    /// </para>
    /// </summary>
    [JsonPropertyName("additionalTextEdits")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextEdit[]? AdditionalTextEdits { get; init; }

}
