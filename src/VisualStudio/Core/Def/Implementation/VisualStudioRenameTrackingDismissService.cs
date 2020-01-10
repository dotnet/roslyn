using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// Handles dismissing the rename tracker when a symbol has changed
    /// </summary>
    [Export(typeof(IRefactorNotifyService)), Shared]
    class VisualStudioRenameTrackingDismissService : IRefactorNotifyService
    {
        public bool TryOnBeforeGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        => true;

        public bool TryOnAfterGlobalSymbolRenamed(Workspace workspace, IEnumerable<DocumentId> changedDocumentIDs, ISymbol symbol, string newName, bool throwOnFailure)
        {
            RenameTrackingDismisser.DismissRenameTracking(workspace, changedDocumentIDs);
            return true;
        }
    }
}
