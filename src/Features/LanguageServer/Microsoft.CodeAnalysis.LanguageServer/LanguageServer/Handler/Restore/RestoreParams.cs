// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[DataContract]
internal sealed record RestoreParams(
    [property: DataMember(Name = "projectFilePath"), JsonProperty(NullValueHandling = NullValueHandling.Ignore)] string? ProjectFilePath
) : IPartialResultParams<RestorePartialResult>
{
    [DataMember(Name = Methods.PartialResultTokenName)]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public IProgress<RestorePartialResult>? PartialResultToken { get; set; }
}
