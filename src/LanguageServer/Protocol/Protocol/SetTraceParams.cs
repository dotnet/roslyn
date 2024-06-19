// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Class representing the parameters for the $/setTrace notification.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#setTrace">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class SetTraceParams
{
    /// <summary>
    /// The new value that should be assigned to the trace setting.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonRequired]
    public TraceValue Value { get; init; }
}
