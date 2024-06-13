// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

[DataContract]
internal record RunTestsParams(
    [property: DataMember(Name = "textDocument")] TextDocumentIdentifier TextDocument,
    [property: DataMember(Name = "range")] VisualStudio.LanguageServer.Protocol.Range Range,
    [property: DataMember(Name = "attachDebugger")] bool AttachDebugger,
    [property: DataMember(Name = "runSettingsPath")] string? RunSettingsPath
) : IPartialResultParams<RunTestsPartialResult>
{
    [DataMember(Name = Methods.PartialResultTokenName)]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IProgress<RunTestsPartialResult>? PartialResultToken { get; set; }
}
