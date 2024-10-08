// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;

/// <summary>
/// Datatype storing the information needed to resolve a particular inlay hint item.
/// </summary>
/// <param name="ResultId">the resultId associated with the inlay hint created on original request.</param>
/// <param name="ListIndex">the index of the specific inlay hint item in the original list.</param>
/// <param name="TextDocument">the text document associated with the inlay hint to resolve.</param>
internal sealed record InlayHintResolveData(long ResultId, int ListIndex, TextDocumentIdentifier TextDocument) : DocumentResolveData(TextDocument);
