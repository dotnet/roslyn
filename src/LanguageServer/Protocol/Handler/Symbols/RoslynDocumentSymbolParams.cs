// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// A parameter object that indicates whether the LSP client should use hierarchical symbols. Inherits from
/// DocumentSymbolParams.
/// </summary>
/// <remarks>
/// The LSP client does not support hierarchical document symbols and we can't contribute to client capabilities as
/// the extension. This type is required in order to obtain a response of type DocumentSymbol[] for a document
/// symbol request.
/// </remarks>
internal sealed class RoslynDocumentSymbolParams : DocumentSymbolParams
{
    [JsonPropertyName("useHierarchicalSymbols")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool UseHierarchicalSymbols { get; set; }
}
