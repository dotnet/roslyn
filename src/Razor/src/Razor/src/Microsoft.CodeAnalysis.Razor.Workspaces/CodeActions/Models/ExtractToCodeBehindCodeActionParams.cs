// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

internal sealed class ExtractToCodeBehindCodeActionParams
{
    [JsonPropertyName("extractStart")]
    public int ExtractStart { get; set; }

    [JsonPropertyName("extractEnd")]
    public int ExtractEnd { get; set; }

    [JsonPropertyName("removeStart")]
    public int RemoveStart { get; set; }

    [JsonPropertyName("removeEnd")]
    public int RemoveEnd { get; set; }

    [JsonPropertyName("namespace")]
    public required string Namespace { get; set; }
}
