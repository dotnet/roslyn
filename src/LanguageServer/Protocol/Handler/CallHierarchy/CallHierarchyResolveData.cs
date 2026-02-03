// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

/// <summary>
/// Data stored in LSP CallHierarchyItem.Data to preserve item information across requests.
/// </summary>
/// <param name="ResultId">The resultId associated with the call hierarchy item created on original request.</param>
/// <param name="ItemIndex">The index of the specific call hierarchy item in the original list.</param>
/// <param name="TextDocument">The text document associated with the call hierarchy item.</param>
internal sealed record CallHierarchyResolveData(long ResultId, int ItemIndex, TextDocumentIdentifier TextDocument) : DocumentResolveData(TextDocument);
