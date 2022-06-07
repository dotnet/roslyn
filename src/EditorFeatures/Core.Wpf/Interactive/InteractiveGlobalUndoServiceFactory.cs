// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    [ExportWorkspaceServiceFactory(typeof(IGlobalUndoService), WorkspaceKind.Interactive), Shared]
    internal sealed class InteractiveGlobalUndoServiceFactory : IWorkspaceServiceFactory
    {
        private readonly GlobalUndoService _singleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InteractiveGlobalUndoServiceFactory(ITextUndoHistoryRegistry undoHistoryRegistry)
            => _singleton = new GlobalUndoService(undoHistoryRegistry);

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _singleton;

        private class GlobalUndoService : IGlobalUndoService
        {
            private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;

            public bool IsGlobalTransactionOpen(Workspace workspace)
                => GetHistory(workspace).CurrentTransaction != null;

            public GlobalUndoService(ITextUndoHistoryRegistry undoHistoryRegistry)
                => _undoHistoryRegistry = undoHistoryRegistry;

            public bool CanUndo(Workspace workspace)
            {
                // only primary workspace supports global undo
                return workspace is InteractiveWindowWorkspace;
            }

            public IWorkspaceGlobalUndoTransaction OpenGlobalUndoTransaction(Workspace workspace, string description)
            {
                if (!CanUndo(workspace))
                {
                    throw new ArgumentException(EditorFeaturesResources.Given_Workspace_doesn_t_support_Undo);
                }

                var textUndoHistory = GetHistory(workspace);

                var transaction = textUndoHistory.CreateTransaction(description);

                return new InteractiveGlobalUndoTransaction(transaction);
            }

            private ITextUndoHistory GetHistory(Workspace workspace)
            {
                var interactiveWorkspace = (InteractiveWindowWorkspace)workspace;
                var textBuffer = interactiveWorkspace.Window.TextView.TextBuffer;

                Contract.ThrowIfFalse(_undoHistoryRegistry.TryGetHistory(textBuffer, out var textUndoHistory));

                return textUndoHistory;
            }

            private class InteractiveGlobalUndoTransaction : IWorkspaceGlobalUndoTransaction
            {
                private readonly ITextUndoTransaction _transaction;

                public InteractiveGlobalUndoTransaction(ITextUndoTransaction transaction)
                    => _transaction = transaction;

                public void AddDocument(DocumentId id)
                {
                    // Nothing to do.
                }

                public void Commit()
                    => _transaction.Complete();

                public void Dispose()
                    => _transaction.Dispose();
            }
        }
    }
}
