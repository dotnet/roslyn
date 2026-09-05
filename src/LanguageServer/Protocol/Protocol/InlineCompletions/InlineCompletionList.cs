// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a collection of inline completion items to be presented in the editor.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlineCompletionList">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal sealed class InlineCompletionList
{
    /// <summary>
    /// The inline completion items.
    /// </summary>
    [JsonPropertyName("items")]
    [JsonRequired]
    public InlineCompletionItem[] Items { get; set; }
}
