// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing text and an associated format that should be rendered.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#markupContentInnerDefinition">Language Server Protocol specification</see> for additional information.
/// </summary>
internal sealed class MarkupContent
{
    /// <summary>
    /// Gets or sets the <see cref="MarkupKind"/> representing the text's format.
    /// </summary>
    [JsonPropertyName("kind")]
    public MarkupKind Kind
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the text that should be rendered.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value
    {
        get;
        set;
    }
}
