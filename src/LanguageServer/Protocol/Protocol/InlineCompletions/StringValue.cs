// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// A string value used as a snippet, a computed value, or with both.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#stringValue">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal sealed class StringValue
{
    /// <summary>
    /// The kind of string value.
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonRequired]
    public string Kind { get; init; } = "snippet";

    /// <summary>
    /// The snippet string.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonRequired]
    public string Value { get; set; }
}
