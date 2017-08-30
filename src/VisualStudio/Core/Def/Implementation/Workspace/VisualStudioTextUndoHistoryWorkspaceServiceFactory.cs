// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    [ExportWorkspaceServiceFactory(typeof(ITextUndoHistoryWorkspaceService), ServiceLayer.Host), Shared]
    internal class VisualStudioTextUndoHistoryWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        private readonly ITextUndoHistoryWorkspaceService _serviceSingleton;

        [ImportingConstructor]
        public VisualStudioTextUndoHistoryWorkspaceServiceFactory()
        {
            _serviceSingleton = new TextUndoHistoryWorkspaceService();
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _serviceSingleton;
        }

        private class TextUndoHistoryWorkspaceService : ITextUndoHistoryWorkspaceService
        {
            public bool TryGetTextUndoHistory(Workspace editorWorkspace, ITextBuffer textBuffer, out ITextUndoHistory undoHistory)
            {
                undoHistory = null;

                if (!(editorWorkspace is VisualStudioWorkspaceImpl) &&
                    !(editorWorkspace is MiscellaneousFilesWorkspace))
                {
                    return false;
                }

                // TODO: Handle undo if context changes
                var documentId = editorWorkspace.GetDocumentIdInCurrentContext(textBuffer.AsTextContainer());
                if (documentId == null)
                {
                    return false;
                }

                var document = GetDocument(editorWorkspace, documentId);
                if (document == null)
                {
                    return false;
                }

                undoHistory = document.GetTextUndoHistory();
                return true;
            }

            private IVisualStudioHostDocument GetDocument(Workspace workspace, DocumentId id)
            {
                switch (workspace)
                {
                    case VisualStudioWorkspaceImpl visualStudioWorkspace:
                        return visualStudioWorkspace.GetHostDocument(id);
                    case MiscellaneousFilesWorkspace miscWorkspace:
                        return miscWorkspace.GetDocument(id);
                }

                return null;
            }
        }
    }
}
