// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

/// <summary>
/// Data type storing the information needed to resolve a particular call hierarchy item.
/// </summary>
/// <param name="ResultId">The resultId associated with the call hierarchy item created on original request.</param>
/// <param name="TextDocument">The text document associated with the call hierarchy item.</param>
/// <param name="SymbolName">The name of the symbol (for caching purposes).</param>
/// <param name="SymbolKind">The kind of symbol (for caching purposes).</param>
internal sealed record CallHierarchyResolveData(
    long ResultId,
    TextDocumentIdentifier TextDocument,
    string SymbolName,
    int SymbolKind) : DocumentResolveData(TextDocument);
