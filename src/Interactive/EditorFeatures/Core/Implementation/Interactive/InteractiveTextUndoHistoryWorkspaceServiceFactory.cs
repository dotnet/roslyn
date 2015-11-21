// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    [ExportWorkspaceServiceFactory(typeof(ITextUndoHistoryWorkspaceService), WorkspaceKind.Interactive), Shared]
    internal sealed class InteractiveTextUndoHistoryWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private readonly TextUndoHistoryWorkspaceService _serviceSingleton;

        [ImportingConstructor]
        public InteractiveTextUndoHistoryWorkspaceServiceFactory(ITextUndoHistoryRegistry textUndoHistoryRegistry)
        {
            _serviceSingleton = new TextUndoHistoryWorkspaceService(textUndoHistoryRegistry);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _serviceSingleton;
        }

        private class TextUndoHistoryWorkspaceService : ITextUndoHistoryWorkspaceService
        {
            private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;

            public TextUndoHistoryWorkspaceService(ITextUndoHistoryRegistry textUndoHistoryRegistry)
            {
                _textUndoHistoryRegistry = textUndoHistoryRegistry;
            }

            public bool TryGetTextUndoHistory(Workspace editorWorkspace, ITextBuffer textBuffer, out ITextUndoHistory undoHistory)
            {
                undoHistory = null;

                var interactiveWorkspace = editorWorkspace as InteractiveWorkspace;
                if (interactiveWorkspace == null)
                {
                    return false;
                }

                return _textUndoHistoryRegistry.TryGetHistory(textBuffer, out undoHistory);
            }
        }
    }
}
