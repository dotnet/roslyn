// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer.Testing;

[DataContract]
internal class RunTestsParams : IPartialResultParams<RunTestsPartialResult>
{
    [DataMember(Name = Methods.PartialResultTokenName)]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IProgress<RunTestsPartialResult>? PartialResultToken { get; set; }

    [DataMember(Name = "textDocument")]
    public TextDocumentIdentifier TextDocument { get; set; }

    [DataMember(Name = "range")]
    public VisualStudio.LanguageServer.Protocol.Range Range { get; set; }
}