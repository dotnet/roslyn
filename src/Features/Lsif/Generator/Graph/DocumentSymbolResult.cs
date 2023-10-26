// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;

/// <summary>
/// Represents a documentSymbolResult vertex for serialization. See https://microsoft.github.io/language-server-protocol/specifications/lsif/0.6.0/specification/#documentSymbol for further details.
/// </summary>
internal sealed class DocumentSymbolResult : Vertex
{
    // The specification allows us to output either range based document symbols, or regular LSP symbols. We choose the former.
    [JsonProperty("result")]
    public List<RangeBasedDocumentSymbol> Result { get; }

    public DocumentSymbolResult(List<RangeBasedDocumentSymbol> result, IdFactory idFactory)
        : base(label: "documentSymbolResult", idFactory)
    {
        Result = result;
    }
}
