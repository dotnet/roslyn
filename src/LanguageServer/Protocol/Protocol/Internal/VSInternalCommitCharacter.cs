// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Extension class for CompletionItem with fields specific to Visual Studio functionalities.
/// </summary>
internal sealed class VSInternalCommitCharacter
{
    /// <summary>
    /// Gets or sets the commit character.
    /// </summary>
    [JsonPropertyName("_vs_character")]
    public string Character { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the commit character should be inserted or not.
    /// </summary>
    [JsonPropertyName("_vs_insert")]
    public bool Insert { get; set; }
}
