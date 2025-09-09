// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing range of characters for completion continuation.
/// </summary>
[Kind(Type, "_vs_type")]
internal sealed class VSInternalContinueCharacterRange
{
    public const string Type = "charRange";

    /// <summary>
    /// Gets the type value.
    /// </summary>
    [JsonPropertyName("_vs_type")]
    [JsonRequired]
    [JsonInclude]
    internal string TypeDiscriminator = Type;

    /// <summary>
    /// Gets or sets the first completion character of the range.
    /// </summary>
    [JsonPropertyName("_vs_start")]
    [JsonRequired]
    public string Start { get; set; }

    /// <summary>
    /// Gets or sets the last completion character of the range.
    /// </summary>
    [JsonPropertyName("_vs_end")]
    [JsonRequired]
    public string End { get; set; }
}
