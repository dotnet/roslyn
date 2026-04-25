// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal struct RemoteClientLSPInitializationOptions
{
    [JsonPropertyName("tokenTypes")]
    public required string[] TokenTypes { get; set; }

    [JsonPropertyName("tokenModifiers")]
    public required string[] TokenModifiers { get; set; }

    [JsonPropertyName("clientCapabilities")]
    public required VSInternalClientCapabilities ClientCapabilities { get; set; }
}
