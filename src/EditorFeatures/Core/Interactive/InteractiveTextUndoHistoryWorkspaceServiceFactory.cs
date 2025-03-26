// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Interactive
{
    [ExportWorkspaceServiceFactory(typeof(ITextUndoHistoryWorkspaceService), [WorkspaceKind.Interactive]), Shared]
    internal sealed class InteractiveTextUndoHistoryWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private readonly TextUndoHistoryWorkspaceService _serviceSingleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InteractiveTextUndoHistoryWorkspaceServiceFactory(ITextUndoHistoryRegistry textUndoHistoryRegistry)
            => _serviceSingleton = new TextUndoHistoryWorkspaceService(textUndoHistoryRegistry);

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _serviceSingleton;

        private class TextUndoHistoryWorkspaceService : ITextUndoHistoryWorkspaceService
        {
            private readonly ITextUndoHistoryRegistry _textUndoHistoryRegistry;

            public TextUndoHistoryWorkspaceService(ITextUndoHistoryRegistry textUndoHistoryRegistry)
                => _textUndoHistoryRegistry = textUndoHistoryRegistry;

            public bool TryGetTextUndoHistory(Workspace editorWorkspace, ITextBuffer textBuffer, out ITextUndoHistory undoHistory)
            {
                undoHistory = null;

                if (editorWorkspace is not InteractiveWindowWorkspace)
                {
                    return false;
                }

                return _textUndoHistoryRegistry.TryGetHistory(textBuffer, out undoHistory);
            }
        }
    }
}
