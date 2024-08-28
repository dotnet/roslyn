// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Describe options to be used when registering for file system change events.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didChangeWatchedFilesRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class DidChangeWatchedFilesRegistrationOptions : DynamicRegistrationSetting
{
    /// <summary>
    /// The watchers to register.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("watchers")]
    [JsonRequired]
    public FileSystemWatcher[] Watchers { get; init; }
}
