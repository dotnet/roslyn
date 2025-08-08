// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;

/// <summary>
/// Provides the intermediate data passed from CompletionHandler to CompletionResolveHandler.
/// Passed along via <see cref="LSP.CompletionItem.Data"/>.
/// <param name="ResultId">the resultId associated with the completion created on original request.</param>
/// <param name="TextDocument">the text document associated with the completion request to resolve.</param>
/// </summary>
internal sealed record CompletionResolveData(long ResultId, LSP.TextDocumentIdentifier TextDocument) : DocumentResolveData(TextDocument);
