// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.Test.Utilities.Workspaces
{
    [Export(typeof(IRefactorNotifyService)), Shared]
    [PartNotDiscoverable]
    internal class TestRefactorNotify : IRefactorNotifyService
    {
        public delegate void SymbolRenamedEventHandler(SymbolRenameEventArgs args);
        public event SymbolRenamedEventHandler OnAfterRename;
        public event SymbolRenamedEventHandler OnBeforeRename;

        public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            OnAfterRename?.Invoke(new SymbolRenameEventArgs(workspace, changedDocumentIDs, symbol, newName));
            return true;
        }

        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            OnBeforeRename?.Invoke(new SymbolRenameEventArgs(workspace, changedDocumentIDs, symbol, newName));
            return true;
        }
    }
}
