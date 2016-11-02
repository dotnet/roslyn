// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    internal static class IVsEditorAdaptersFactoryServiceExtensions
    {
        public static IOleUndoManager TryGetUndoManager(
            this IVsEditorAdaptersFactoryService editorAdaptersFactoryService, 
            Workspace workspace,
            DocumentId contextDocumentId, 
            CancellationToken cancellationToken)
        {
            var document = workspace.CurrentSolution.GetDocument(contextDocumentId);
            var text = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var textSnapshot = text.FindCorrespondingEditorTextSnapshot();
            var textBuffer = textSnapshot?.TextBuffer;
            return editorAdaptersFactoryService.TryGetUndoManager(textBuffer);
        }

        public static IOleUndoManager TryGetUndoManager(
            this IVsEditorAdaptersFactoryService editorAdaptersFactoryService, ITextBuffer subjectBuffer)
        {
            if (subjectBuffer != null)
            {
                var adapter = editorAdaptersFactoryService.GetBufferAdapter(subjectBuffer);
                if (adapter != null)
                {
                    IOleUndoManager manager = null;
                    if (ErrorHandler.Succeeded(adapter.GetUndoManager(out manager)))
                    {
                        return manager;
                    }
                }
            }

            return null;
        }
    }
}