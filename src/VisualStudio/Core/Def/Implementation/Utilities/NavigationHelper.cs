// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class NavigationHelper
    {
        public static bool TryNavigateToPosition(this IServiceProvider serviceProvider, string filePath, int line, int column)
        {
            var docTable = (IVsRunningDocumentTable)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            var textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));
            if (docTable == null || textManager == null)
            {
                return false;
            }

            return TryNavigateToPosition(docTable, textManager, filePath, line, column);
        }

        public static bool TryNavigateToPosition(this VisualStudioWorkspaceImpl workspace, string filePath, int line, int column)
        {
            var docTable = workspace.GetVsService<SVsRunningDocumentTable, IVsRunningDocumentTable>();
            var textManager = workspace.GetVsService<SVsTextManager, IVsTextManager>();
            if (docTable == null || textManager == null)
            {
                return false;
            }

            return TryNavigateToPosition(docTable, textManager, filePath, line, column);
        }

        public static bool TryNavigateToPosition(this IVsRunningDocumentTable docTable, IVsTextManager textManager, string filePath, int line, int column)
        {
            if (ErrorHandler.Failed(docTable.FindAndLockDocument(
                (uint)_VSRDTFLAGS.RDT_NoLock, filePath,
                out var hierarchy, out var itemid, out var bufferPtr, out var cookie)))
            {
                return false;
            }

            try
            {
                var lines = Marshal.GetObjectForIUnknown(bufferPtr) as IVsTextLines;
                if (lines == null)
                {
                    return false;
                }

                return ErrorHandler.Succeeded(textManager.NavigateToLineAndColumn(
                    lines, VSConstants.LOGVIEWID.TextView_guid,
                    line, column, line, column));
            }
            finally
            {
                if (bufferPtr != IntPtr.Zero)
                {
                    Marshal.Release(bufferPtr);
                }
            }
        }
    }
}
