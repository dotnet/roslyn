// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CallHierarchy;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

internal sealed record CallHierarchyResolveData(
    string SymbolKeyData,
    Guid ProjectGuid,
    TextDocumentIdentifier TextDocument) : DocumentResolveData(TextDocument)
{
    public CallHierarchyItemId GetItemId()
        => new(SymbolKeyData, ProjectId.CreateFromSerialized(ProjectGuid));
}
