// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

/// <summary>
/// Data stored in LSP CallHierarchyItem.Data to preserve item information across requests.
/// </summary>
internal sealed class CallHierarchyItemData
{
    [JsonPropertyName("symbolKey")]
    public string SymbolKey { get; }

    [JsonPropertyName("projectId")]
    public string ProjectId { get; }

    [JsonPropertyName("documentId")]
    public string DocumentId { get; }

    [JsonPropertyName("spanStart")]
    public int SpanStart { get; }

    [JsonPropertyName("spanLength")]
    public int SpanLength { get; }

    [JsonConstructor]
    public CallHierarchyItemData(
        string symbolKey,
        string projectId,
        string documentId,
        int spanStart,
        int spanLength)
    {
        SymbolKey = symbolKey;
        ProjectId = projectId;
        DocumentId = documentId;
        SpanStart = spanStart;
        SpanLength = spanLength;
    }

    public CallHierarchyItemData(
        SymbolKey symbolKey,
        ProjectId projectId,
        DocumentId documentId,
        TextSpan span)
        : this(
              symbolKey.ToString(),
              projectId.Id.ToString(),
              documentId.Id.ToString(),
              span.Start,
              span.Length)
    {
    }
}
