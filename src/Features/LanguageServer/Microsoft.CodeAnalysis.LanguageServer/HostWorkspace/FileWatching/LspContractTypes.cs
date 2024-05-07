// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Roslyn.LanguageServer.Protocol;

[DataContract]
internal class DidChangeWatchedFilesRegistrationOptions
{
    [DataMember(Name = "watchers")]
    public required FileSystemWatcher[] Watchers { get; set; }
}

[DataContract]
internal class FileSystemWatcher
{
    [DataMember(Name = "globPattern")]
    public required RelativePattern GlobPattern { get; set; }

    [DataMember(Name = "kind")]
    public WatchKind? Kind { get; set; }
}

[DataContract]
internal class RelativePattern
{
    [DataMember(Name = "baseUri")]
    [JsonConverter(typeof(DocumentUriConverter))]
    public required Uri BaseUri { get; set; }

    [DataMember(Name = "pattern")]
    public required string Pattern { get; set; }
}

// The LSP specification has a spelling error in the protocol, but Microsoft.VisualStudio.LanguageServer.Protocol
// didn't carry that error along. This corrects that.
[DataContract]
internal class UnregistrationParamsWithMisspelling
{
    [DataMember(Name = "unregisterations")]
    public required Unregistration[] Unregistrations { get; set; }
}

[Flags]
internal enum WatchKind
{
    Create = 1,
    Change = 2,
    Delete = 4
}
