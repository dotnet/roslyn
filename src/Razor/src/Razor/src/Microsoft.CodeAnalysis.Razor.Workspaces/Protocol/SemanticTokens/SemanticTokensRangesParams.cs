// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Protocol.SemanticTokens;

internal class SemanticTokensRangesParams : SemanticTokensRangeParams
{
    [JsonPropertyName("ranges")]
    public required LspRange[] Ranges { get; set; }
}
