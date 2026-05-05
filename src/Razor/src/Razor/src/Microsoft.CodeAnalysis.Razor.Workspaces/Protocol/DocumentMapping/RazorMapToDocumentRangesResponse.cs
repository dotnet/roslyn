// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.DocumentMapping;

internal class RazorMapToDocumentRangesResponse
{
    [JsonPropertyName("ranges")]
    public required LspRange[] Ranges { get; init; }

    [JsonPropertyName("spans")]
    public required RazorTextSpan[] Spans { get; set; }

    [JsonPropertyName("hostDocumentVersion")]
    public int? HostDocumentVersion { get; init; }
}
