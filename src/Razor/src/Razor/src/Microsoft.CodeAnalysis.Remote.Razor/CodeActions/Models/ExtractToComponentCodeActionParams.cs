// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

internal sealed class ExtractToComponentCodeActionParams
{
    [JsonPropertyName("start")]
    public required int Start { get; set; }

    [JsonPropertyName("end")]
    public required int End { get; set; }

    [JsonPropertyName("namespace")]
    public string? Namespace { get; set; }
}
