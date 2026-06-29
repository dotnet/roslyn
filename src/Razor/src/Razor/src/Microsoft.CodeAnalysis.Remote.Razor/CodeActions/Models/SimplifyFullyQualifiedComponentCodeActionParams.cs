// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

internal sealed class SimplifyFullyQualifiedComponentCodeActionParams
{
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("componentName")]
    public string ComponentName { get; set; } = string.Empty;

    [JsonPropertyName("startTagSpanStart")]
    public int StartTagSpanStart { get; set; }

    [JsonPropertyName("startTagSpanEnd")]
    public int StartTagSpanEnd { get; set; }

    [JsonPropertyName("endTagSpanStart")]
    public int EndTagSpanStart { get; set; }

    [JsonPropertyName("endTagSpanEnd")]
    public int EndTagSpanEnd { get; set; }
}
