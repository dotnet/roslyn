// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [ExportWorkspaceServiceFactory(typeof(ITextUndoHistoryWorkspaceService), ServiceLayer.Host), Shared]
    internal class VisualStudioTextUndoHistoryWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private readonly ITextUndoHistoryWorkspaceService _serviceSingleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioTextUndoHistoryWorkspaceServiceFactory(ITextUndoHistoryRegistry undoHistoryRegistry)
            => _serviceSingleton = new TextUndoHistoryWorkspaceService(undoHistoryRegistry);

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _serviceSingleton;

        private class TextUndoHistoryWorkspaceService : ITextUndoHistoryWorkspaceService
        {
            private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;

            public TextUndoHistoryWorkspaceService(ITextUndoHistoryRegistry undoHistoryRegistry)
                => _undoHistoryRegistry = undoHistoryRegistry;

            public bool TryGetTextUndoHistory(Workspace editorWorkspace, ITextBuffer textBuffer, out ITextUndoHistory undoHistory)
            {
                switch (editorWorkspace)
                {
                    case VisualStudioWorkspaceImpl visualStudioWorkspace:

                        // TODO: Handle undo if context changes
                        var documentId = editorWorkspace.GetDocumentIdInCurrentContext(textBuffer.AsTextContainer());
                        if (documentId == null)
                        {
                            undoHistory = null;
                            return false;
                        }

                        // In the Visual Studio case, there might be projection buffers involved for Venus,
                        // where we associate undo history with the surface buffer and not the subject buffer.
                        var containedDocument = visualStudioWorkspace.TryGetContainedDocument(documentId);

                        if (containedDocument != null)
                        {
                            textBuffer = containedDocument.DataBuffer;
                        }

                        break;

                    case MiscellaneousFilesWorkspace _:

                        // Nothing to do in this case: textBuffer is correct!

                        break;

                    default:

                        undoHistory = null;
                        return false;

                }

                undoHistory = _undoHistoryRegistry.GetHistory(textBuffer);
                return true;
            }
        }
    }
}
