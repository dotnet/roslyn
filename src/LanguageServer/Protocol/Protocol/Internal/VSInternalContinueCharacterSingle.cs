// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing single continue character for completion.
/// </summary>
[Kind(Type, "_vs_type")]
internal sealed class VSInternalContinueCharacterSingle
{
    public const string Type = "singleChar";

    /// <summary>
    /// Gets the type value.
    /// </summary>
    [JsonPropertyName("_vs_type")]
    [JsonRequired]
    [JsonInclude]
    internal string TypeDiscriminator = Type;

    /// <summary>
    /// Gets or sets the completion character.
    /// </summary>
    [JsonPropertyName("_vs_char")]
    [JsonRequired]
    public string Character { get; set; }
}
