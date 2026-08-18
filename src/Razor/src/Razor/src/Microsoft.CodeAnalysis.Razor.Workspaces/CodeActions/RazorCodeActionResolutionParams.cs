// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

internal sealed record RazorCodeActionResolutionParams(TextDocumentIdentifier TextDocument) : DocumentResolveData(TextDocument)
{
    [JsonPropertyName("action")]
    public required string Action { get; set; }

    [JsonPropertyName("language")]
    public required RazorLanguageKind Language { get; set; }

    [JsonPropertyName("delegatedDocumentUri")]
    public required Uri? DelegatedDocumentUri { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    public static RazorCodeActionResolutionParams Unwrap(CodeAction codeAction)
    {
        if (codeAction.Data is not JsonElement paramsObj)
        {
            throw new InvalidOperationException($"Invalid CodeAction Received '{codeAction.Title}'.");
        }

        if (paramsObj.Deserialize<RazorCodeActionResolutionParams>() is not { } resolutionParams)
        {
            throw new InvalidOperationException($"codeAction.Data should be convertible to {nameof(RazorCodeActionResolutionParams)}");
        }

        return resolutionParams;
    }
}
