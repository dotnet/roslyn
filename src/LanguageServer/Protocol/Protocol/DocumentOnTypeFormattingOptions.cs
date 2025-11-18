// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the options for on type formatting.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentOnTypeFormattingOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class DocumentOnTypeFormattingOptions
{
    /// <summary>
    /// A character on which formatting should be triggered, like <c>{</c>.
    /// </summary>
    [JsonPropertyName("firstTriggerCharacter")]
    [JsonRequired]
    public string FirstTriggerCharacter
    {
        get;
        set;
    }

    /// <summary>
    /// Optional additional trigger characters.
    /// </summary>
    [JsonPropertyName("moreTriggerCharacter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? MoreTriggerCharacter
    {
        get;
        set;
    }
}
