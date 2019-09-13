using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Test.Utilities.Workspaces
{
    internal class SymbolRenameEventArgs
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
