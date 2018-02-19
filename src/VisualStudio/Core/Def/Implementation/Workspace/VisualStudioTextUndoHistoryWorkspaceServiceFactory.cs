﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public VisualStudioTextUndoHistoryWorkspaceServiceFactory(ITextUndoHistoryRegistry undoHistoryRegistry)
        {
            _serviceSingleton = new TextUndoHistoryWorkspaceService(undoHistoryRegistry);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _serviceSingleton;
        }

        private class TextUndoHistoryWorkspaceService : ITextUndoHistoryWorkspaceService
        {
            private ITextUndoHistoryRegistry _undoHistoryRegistry;

            public TextUndoHistoryWorkspaceService(ITextUndoHistoryRegistry undoHistoryRegistry)
            {
                _undoHistoryRegistry = undoHistoryRegistry;
            }

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
                        textBuffer = visualStudioWorkspace.GetHostDocument(documentId).GetTextUndoHistoryBuffer();

                        break;

                    case MiscellaneousFilesWorkspace miscellaneousFilesWorkspace:

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
