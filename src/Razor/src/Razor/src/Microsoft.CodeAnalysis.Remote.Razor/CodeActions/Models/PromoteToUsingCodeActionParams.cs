// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

internal sealed class PromoteToUsingCodeActionParams
{
    [JsonPropertyName("usingStart")]
    public required int UsingStart { get; init; }

    [JsonPropertyName("usingEnd")]
    public required int UsingEnd { get; init; }

    [JsonPropertyName("removeStart")]
    public required int RemoveStart { get; init; }

    [JsonPropertyName("removeEnd")]
    public required int RemoveEnd { get; init; }
}
