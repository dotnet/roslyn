// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

internal class DidChangeWatchedFilesRegistrationOptions
{
    [JsonPropertyName("watchers")]
    public required FileSystemWatcher[] Watchers { get; set; }
}

internal class FileSystemWatcher
{
    [JsonPropertyName("globPattern")]
    public required RelativePattern GlobPattern { get; set; }

    [JsonPropertyName("kind")]
    public WatchKind? Kind { get; set; }
}

internal class RelativePattern
{
    [JsonPropertyName("baseUri")]
    [JsonConverter(typeof(DocumentUriConverter))]
    public required Uri BaseUri { get; set; }

    [JsonPropertyName("pattern")]
    public required string Pattern { get; set; }
}

// The LSP specification has a spelling error in the protocol, but Microsoft.VisualStudio.LanguageServer.Protocol
// didn't carry that error along. This corrects that.
internal class UnregistrationParamsWithMisspelling
{
    [JsonPropertyName("unregisterations")]
    public required Unregistration[] Unregistrations { get; set; }
}

[Flags]
internal enum WatchKind
{
    Create = 1,
    Change = 2,
    Delete = 4
}
