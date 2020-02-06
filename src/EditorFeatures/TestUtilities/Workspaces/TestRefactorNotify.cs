// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.Test.Utilities.Workspaces
{
    [Export(typeof(IRefactorNotifyService)), Shared]
    [PartNotDiscoverable]
    internal class TestRefactorNotify : IRefactorNotifyService
    {
        public delegate bool SymbolRenamedEventHandler(SymbolRenameEventArgs args);
        public event SymbolRenamedEventHandler OnAfterRename;
        public event SymbolRenamedEventHandler OnBeforeRename;

        public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            var succeeded = OnAfterRename?.Invoke(new SymbolRenameEventArgs(workspace, changedDocumentIDs, symbol, newName)) ?? true;

            if (throwOnFailure && !succeeded)
            {
                Marshal.ThrowExceptionForHR(unchecked((int)0x80004004)); // E_ABORT
            }

            return succeeded;
        }

        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            var succeeded = OnBeforeRename?.Invoke(new SymbolRenameEventArgs(workspace, changedDocumentIDs, symbol, newName)) ?? true;

            if (throwOnFailure && !succeeded)
            {
                Marshal.ThrowExceptionForHR(unchecked((int)0x80004004)); // E_ABORT
            }

            return succeeded;
        }
    }
}
