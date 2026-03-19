// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Utilities;

internal static class IVsEditorAdaptersFactoryServiceExtensions
{
    public static IOleUndoManager TryGetUndoManager(
        this IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
        Microsoft.CodeAnalysis.Workspace workspace,
        DocumentId contextDocumentId,
        CancellationToken cancellationToken)
    {
        // https://github.com/dotnet/roslyn/issues/17898
        // We have a report of a null ref occurring in this method. The only place we believe 
        // this could be would be if 'document' was null. Try to catch a reasonable 
        // non -fatal-watson dump to help track down what the root cause of this might be.
        var document = workspace.CurrentSolution.GetDocument(contextDocumentId);
        if (document == null)
        {
            var message = contextDocumentId == null
                ? $"{nameof(contextDocumentId)} was null."
                : $"{nameof(contextDocumentId)} was not null.";

            FatalError.ReportAndCatch(new InvalidOperationException("Could not retrieve document. " + message));

            return null;
        }

        var text = document.GetTextSynchronously(cancellationToken);
        var textSnapshot = text.FindCorrespondingEditorTextSnapshot();
        var textBuffer = textSnapshot?.TextBuffer;
        return editorAdaptersFactoryService.TryGetUndoManager(textBuffer);
    }

    public static IOleUndoManager TryGetUndoManager(
        this IVsEditorAdaptersFactoryService editorAdaptersFactoryService, ITextBuffer subjectBuffer)
    {
        if (subjectBuffer != null)
        {
            var adapter = editorAdaptersFactoryService?.GetBufferAdapter(subjectBuffer);
            if (adapter != null)
            {
                if (ErrorHandler.Succeeded(adapter.GetUndoManager(out var manager)))
                {
                    return manager;
                }
            }
        }

        return null;
    }
}
