// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.CodeLens;

internal sealed record RazorCodeLensResolveData(
    TextDocumentIdentifier TextDocument,
    [property: JsonPropertyName("data")] object? OriginalData) : DocumentResolveData(TextDocument)
{
    public static RazorCodeLensResolveData Unwrap(LspCodeLens codeLens)
    {
        if (codeLens.Data is not JsonElement paramsObj)
        {
            throw new InvalidOperationException($"Invalid completion item received'{codeLens}'.");
        }

        if (paramsObj.Deserialize<RazorCodeLensResolveData>() is not { } data)
        {
            throw new InvalidOperationException($"codeLens.Data should be convertible to {nameof(RazorCodeLensResolveData)}");
        }

        return data;
    }

    public static void Wrap(LspCodeLens codeLens, TextDocumentIdentifier textDocument)
    {
        codeLens.Data = new RazorCodeLensResolveData(textDocument, codeLens.Data);
    }
}
