// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

/// <summary>
/// Helper extensions for calling into the RDT.
/// These must all be called from the UI thread.
/// </summary>
internal static class RunningDocumentTableExtensions
{
    public static bool TryGetBufferFromMoniker(this IVsRunningDocumentTable4 runningDocumentTable,
        IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
        string moniker, [NotNullWhen(true)] out ITextBuffer? textBuffer)
    {
        textBuffer = null;
        if (!runningDocumentTable.IsFileOpen(moniker))
        {
            return false;
        }

        var cookie = runningDocumentTable.GetDocumentCookie(moniker);
        if (!runningDocumentTable.IsDocumentInitialized(cookie))
        {
            return false;
        }

        return TryGetBuffer(runningDocumentTable, editorAdaptersFactoryService, cookie, out textBuffer);
    }

    public static bool IsFileOpen(this IVsRunningDocumentTable4 runningDocumentTable, string fileName)
        => runningDocumentTable.IsMonikerValid(fileName);

    public static bool TryGetBuffer(this IVsRunningDocumentTable4 runningDocumentTable, IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
        uint docCookie, [NotNullWhen(true)] out ITextBuffer? textBuffer)
    {
        textBuffer = null;

        if (runningDocumentTable.GetDocumentData(docCookie) is IVsTextBuffer bufferAdapter)
        {
            textBuffer = editorAdaptersFactoryService.GetDocumentBuffer(bufferAdapter);
            return textBuffer != null;
        }

        return false;
    }
}
