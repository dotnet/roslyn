// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;

namespace Microsoft.CodeAnalysis.Razor.Protocol.DocumentMapping;

internal class RazorMapToDocumentRangesParams
{
    [JsonPropertyName("kind")]
    public RazorLanguageKind Kind { get; init; }

    [JsonPropertyName("razorDocumentUri")]
    public required Uri RazorDocumentUri { get; init; }

    [JsonPropertyName("projectedRanges")]
    public required LspRange[] ProjectedRanges { get; init; }

    [JsonPropertyName("mappingBehavior")]
    public MappingBehavior MappingBehavior { get; init; }
}
