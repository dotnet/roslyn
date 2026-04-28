// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.CodeLens;

internal record RazorCodeLensResolveData(
    // NOTE: Uppercase T here is required to match Roslyn's DocumentResolveData structure, so that the Roslyn
    //       language server can correctly route requests to us in cohosting. In future when we normalize
    //       on to Roslyn types, we should inherit from that class so we don't have to remember to do this.
    [property: JsonPropertyName("TextDocument")] TextDocumentIdentifier TextDocument,
    [property: JsonPropertyName("data")] object? OriginalData)
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
