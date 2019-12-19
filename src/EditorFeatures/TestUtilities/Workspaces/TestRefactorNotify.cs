using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.Test.Utilities.Workspaces
{
    [Export(typeof(IRefactorNotifyService)), Shared]
    [PartNotDiscoverable]
    class TestRefactorNotify : IRefactorNotifyService
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

    class SymbolRenameEventArgs
    {
        public SymbolRenameEventArgs(Workspace workspace, IEnumerable<DocumentId> documentIds, ISymbol symbol, string newName)
        {
            Workspace = workspace;
            DocumentIds = documentIds;
            Symbol = symbol;
            NewName = newName;
        }

        public Workspace Workspace { get; }
        public IEnumerable<DocumentId> DocumentIds { get; }
        public ISymbol Symbol { get; }
        public string NewName { get; }
    }
}
