// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Protocol.SemanticTokens;

/// <summary>
/// Transports C# semantic token responses from the Razor LS client to the Razor LS.
/// </summary>
internal class ProvideSemanticTokensResponse
{
    public ProvideSemanticTokensResponse(int[]? tokens, long hostDocumentSyncVersion)
    {
        Tokens = tokens;
        HostDocumentSyncVersion = hostDocumentSyncVersion;
    }

    [JsonPropertyName("tokens")]
    public int[]? Tokens { get; }

    [JsonPropertyName("hostDocumentSyncVersion")]
    public long HostDocumentSyncVersion { get; }
}
