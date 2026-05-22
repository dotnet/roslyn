// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;

[DataContract]
internal record SimplifyMethodParams : ITextDocumentParams
{
    [DataMember(Name = "textDocument")]
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; set; }

    [DataMember(Name = "textEdit")]
    [JsonPropertyName("textEdit")]
    public required TextEdit TextEdit { get; set; }
}
