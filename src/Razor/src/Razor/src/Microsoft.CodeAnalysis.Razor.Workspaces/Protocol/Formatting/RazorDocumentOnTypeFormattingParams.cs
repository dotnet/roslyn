// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.Formatting;

internal class RazorDocumentOnTypeFormattingParams : DocumentOnTypeFormattingParams
{
    [JsonPropertyName("hostDocumentVersion")]
    public int HostDocumentVersion { get; init; }
}
