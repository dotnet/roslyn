// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

/// <summary>
/// Data stored in the CallHierarchyItem.Data field that is used to resolve the item
/// for incoming/outgoing calls requests.
/// </summary>
/// <param name="ResultId">The result ID associated with the call hierarchy item from the prepare request.</param>
/// <param name="ListIndex">The index of the specific call hierarchy item in the original list.</param>
/// <param name="TextDocument">The text document associated with the call hierarchy item.</param>
internal sealed record CallHierarchyItemResolveData(
    long ResultId,
    int ListIndex,
    TextDocumentIdentifier TextDocument) : DocumentResolveData(TextDocument);
