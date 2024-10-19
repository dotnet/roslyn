// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Class representing the parameters for the <see cref="Methods.LogTraceName"/> notification.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#logTrace">Language Server Protocol specification</see> for additional information.
/// </summary>
internal class LogTraceParams
{
    /// <summary>
    /// The message to be logged.
    /// </summary>
    [JsonPropertyName("message")]
    [JsonRequired]
    public string Message { get; init; }

    /// <summary>
    /// Additional information that can be computed if the `trace` configuration
    /// is set to <see cref="TraceValue.Verbose"/>.
    /// </summary>
    [JsonPropertyName("verbose")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Verbose { get; init; }
}
