// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

[Export(typeof(IRefactorNotifyService))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorRefactorNotifyWrapper(
    [Import(AllowDefault = true)] IRazorRefactorNotifyService? implementation) : IRefactorNotifyService
{
    private readonly IRazorRefactorNotifyService? _implementation = implementation;

    public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
    {
        // If we return false, we block the rename operation. We don't want to do that :)
        if (_implementation is null)
            return true;

        return _implementation.TryOnAfterGlobalSymbolRenamed(workspace, changedDocumentIDs, symbol, newName, throwOnFailure);
    }

    public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
    {
        // If we return false, we block the rename operation. We don't want to do that :)
        if (_implementation is null)
            return true;

        return _implementation.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocumentIDs, symbol, newName, throwOnFailure);
    }
}
