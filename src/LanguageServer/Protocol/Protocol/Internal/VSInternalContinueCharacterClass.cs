// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing a unicode character class for completion continuation.
/// </summary>
[Kind(Type, "_vs_type")]
internal sealed class VSInternalContinueCharacterClass
{
    public const string Type = "unicodeClass";

    /// <summary>
    /// Gets the type value.
    /// </summary>
    [JsonPropertyName("_vs_type")]
    [JsonRequired]
    [JsonInclude]
    internal string TypeDiscriminator = Type;

    /// <summary>
    /// Gets or sets the unicode class.
    /// </summary>
    [JsonPropertyName("_vs_unicodeClass")]
    [JsonRequired]
    public string UnicodeClass { get; set; }
}
