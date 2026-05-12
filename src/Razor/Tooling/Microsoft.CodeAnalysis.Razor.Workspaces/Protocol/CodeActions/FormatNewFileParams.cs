// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;

[DataContract]
internal record FormatNewFileParams
{
    [DataMember(Name = "document")]
    [JsonPropertyName("document")]
    public required TextDocumentIdentifier Document { get; set; }

    [DataMember(Name = "project")]
    [JsonPropertyName("project")]
    public required TextDocumentIdentifier Project { get; set; }

    [DataMember(Name = "contents")]
    [JsonPropertyName("contents")]
    public required string Contents { get; set; }
}
