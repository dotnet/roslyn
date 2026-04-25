// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Protocol.SemanticTokens;

internal class ProvideSemanticTokensRangesParams : SemanticTokensParams
{
    [JsonPropertyName("requiredHostDocumentVersion")]
    public int RequiredHostDocumentVersion { get; }

    [JsonPropertyName("ranges")]
    public LspRange[] Ranges { get; }

    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; }

    public ProvideSemanticTokensRangesParams(TextDocumentIdentifier textDocument, int requiredHostDocumentVersion, LspRange[] ranges, Guid correlationId)
    {
        TextDocument = textDocument;
        RequiredHostDocumentVersion = requiredHostDocumentVersion;
        Ranges = ranges;
        CorrelationId = correlationId;
    }
}
