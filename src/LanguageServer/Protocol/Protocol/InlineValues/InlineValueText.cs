// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Provide inline value as text.
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class InlineValueText
{
    /// <summary>
    /// The document range for which the inline value applies.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range { get; init; }

    /// <summary>
    /// The text of the inline value.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonRequired]
    public string Text { get; init; }
}
