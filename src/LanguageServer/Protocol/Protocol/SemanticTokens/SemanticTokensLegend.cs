// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Legend used by the server to describe how it encodes semantic token types in <see cref="SemanticTokens.Data"/>.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensLegend">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class SemanticTokensLegend
{
    /// <summary>.
    /// The semantic token types the server uses. Indices into this array are used to encode token types in semantic tokens responses.
    /// </summary>
    [JsonPropertyName("tokenTypes")]
    [JsonRequired]
    public string[] TokenTypes
    {
        get;
        set;
    }

    /// <summary>
    /// The semantic token modifiers the server uses. Indices into this array are used to encode modifiers in semantic tokens responses.
    /// </summary>
    [JsonPropertyName("tokenModifiers")]
    [JsonRequired]
    public string[] TokenModifiers
    {
        get;
        set;
    }
}
