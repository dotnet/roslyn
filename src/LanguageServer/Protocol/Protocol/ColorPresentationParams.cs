// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Class representing the parameters sent for a textDocument/colorPresentation request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#colorPresentationParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.6</remarks>
internal class ColorPresentationParams : ITextDocumentParams, IWorkDoneProgressParams, IPartialResultParams<ColorPresentation[]>
{
    /// <summary>
    /// The text document.
    /// </summary>
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument { get; set; }

    /// <summary>
    /// The color information to request presentations for.
    /// </summary>
    [JsonPropertyName("color")]
    [JsonRequired]
    public Color Color { get; init; }

    /// <summary>
    /// The range where the color would be inserted. Serves as a context.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<ColorPresentation[]>? PartialResultToken { get; set; }
}
