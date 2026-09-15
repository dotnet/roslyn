// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Describes the currently selected completion item.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#selectedCompletionInfo">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal sealed class SelectedCompletionInfo
{
    /// <summary>
    /// The range that will be replaced if this completion item is accepted.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range { get; set; }

    /// <summary>
    /// The text the range will be replaced with if this completion is accepted.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonRequired]
    public string Text { get; set; }
}
