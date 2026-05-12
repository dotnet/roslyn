// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.DocumentMapping;

internal sealed record class RazorMapToDocumentEditsParams
{
    [JsonPropertyName("kind")]
    public RazorLanguageKind Kind { get; init; }

    [JsonPropertyName("razorDocumentUri")]
    public required Uri RazorDocumentUri { get; init; }

    [JsonPropertyName("textChanges")]
    public required RazorTextChange[] TextChanges { get; init; }
}
