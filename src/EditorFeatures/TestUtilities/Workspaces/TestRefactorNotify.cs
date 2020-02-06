// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public int OnBeforeSymbolRenamedCount { get; private set; }
        public int OnAfterSymbolRenamedCount { get; private set; }

        public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            OnBeforeSymbolRenamedCount++;

            var succeeded = OnAfterRename?.Invoke(new SymbolRenameEventArgs(workspace, changedDocumentIDs, symbol, newName)) ?? true;

            if (throwOnFailure && !succeeded)
            {
                Marshal.ThrowExceptionForHR(unchecked((int)0x80004004)); // E_ABORT
            }

            return succeeded;
        }

        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            OnAfterSymbolRenamedCount++;

            var succeeded = OnBeforeRename?.Invoke(new SymbolRenameEventArgs(workspace, changedDocumentIDs, symbol, newName)) ?? true;

            if (throwOnFailure && !succeeded)
            {
                Marshal.ThrowExceptionForHR(unchecked((int)0x80004004)); // E_ABORT
            }

            return succeeded;
        }
    }
}
