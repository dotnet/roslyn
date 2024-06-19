// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Specifies patterns and kinds of file events to watch.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fileSystemWatcher">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class FileSystemWatcher
{
    /// <summary>
    /// The glob pattern to watch. See <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#pattern">Glob Pattern</see>
    /// and <see cref="RelativePattern"/> for more detail.
    /// </summary>
    [JsonPropertyName("globPattern")]
    [JsonRequired]
    public SumType<string, RelativePattern> GlobPattern { get; init; }

    /// <summary>The kind of events of interest.
    /// <para>
    /// </para>
    /// If omitted it defaults to
    /// <c>WatchKind.Create | WatchKind.Change | WatchKind.Delete</c>
    /// which is <c>7</c>.
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [DefaultValue(WatchKind.Create | WatchKind.Change | WatchKind.Delete)]
    public WatchKind Kind { get; init; } = WatchKind.Create | WatchKind.Change | WatchKind.Delete;
}
