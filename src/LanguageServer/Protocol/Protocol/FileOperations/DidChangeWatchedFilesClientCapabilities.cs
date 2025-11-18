// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Capabilities specific to the `workspace/didChangeWatchedFiles` notification.
/// </summary>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didChangeWatchedFilesClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
internal sealed class DidChangeWatchedFilesClientCapabilities : DynamicRegistrationSetting
{
    /// <summary>
    /// Whether the client has support for relative patterns.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("relativePatternSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RelativePatternSupport { get; init; }
}
