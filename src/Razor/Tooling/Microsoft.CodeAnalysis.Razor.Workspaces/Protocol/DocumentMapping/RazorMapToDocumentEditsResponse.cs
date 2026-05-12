// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.DocumentMapping;

internal sealed record class RazorMapToDocumentEditsResponse
{
    [JsonPropertyName("textChanges")]
    public required RazorTextChange[] TextChanges { get; init; }

    [JsonPropertyName("hostDocumentVersion")]
    public int? HostDocumentVersion { get; init; }
}
