// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing additional information about the content in which a completion request is triggered.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionContext">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class CompletionContext
{
    /// <summary>
    /// Gets or sets the <see cref="CompletionTriggerKind"/> indicating how the completion was triggered.
    /// </summary>
    [JsonPropertyName("triggerKind")]
    [JsonRequired]
    public CompletionTriggerKind TriggerKind
    {
        get;
        set;
    }

    /// <summary>
    /// The trigger character (a single character) that has triggered code completion.
    /// Undefined when <see cref="TriggerKind"/> is not <see cref="CompletionTriggerKind.TriggerCharacter"/>
    /// </summary>
    [JsonPropertyName("triggerCharacter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TriggerCharacter
    {
        get;
        set;
    }
}
