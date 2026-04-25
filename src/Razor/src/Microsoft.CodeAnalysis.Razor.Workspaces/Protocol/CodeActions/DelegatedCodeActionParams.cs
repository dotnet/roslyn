// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;

internal class DelegatedCodeActionParams
{
    [JsonPropertyName("hostDocumentVersion")]
    public int HostDocumentVersion { get; set; }

    [JsonPropertyName("codeActionParams")]
    public required VSCodeActionParams CodeActionParams { get; set; }

    [JsonPropertyName("languageKind")]
    public RazorLanguageKind LanguageKind { get; set; }

    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; set; }
}
