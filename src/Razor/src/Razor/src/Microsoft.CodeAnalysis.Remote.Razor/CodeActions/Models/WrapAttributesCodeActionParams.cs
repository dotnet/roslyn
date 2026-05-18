// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

internal sealed class WrapAttributesCodeActionParams
{
    [JsonPropertyName("indentSize")]
    public int IndentSize { get; init; }

    [JsonPropertyName("newLinePositions")]
    public required int[] NewLinePositions { get; init; }
}
