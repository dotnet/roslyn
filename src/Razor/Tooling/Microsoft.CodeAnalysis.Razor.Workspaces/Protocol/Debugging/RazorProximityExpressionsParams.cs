// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.Debugging;

internal class RazorProximityExpressionsParams
{
    [JsonPropertyName("uri")]
    public required Uri Uri { get; init; }

    [JsonPropertyName("position")]
    public required Position Position { get; init; }

    [JsonPropertyName("hostDocumentSyncVersion")]
    public required long HostDocumentSyncVersion { get; init; }
}
