// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal static class IVsRunningDocumentTableExtensions
    {
        public static bool TryGetCookieForInitializedDocument(this IVsRunningDocumentTable4 runningDocTable, string moniker, out uint docCookie)
        {
            docCookie = VSConstants.VSCOOKIE_NIL;

            if (runningDocTable != null && runningDocTable.IsMonikerValid(moniker))
            {
                var foundDocCookie = runningDocTable.GetDocumentCookie(moniker);

                if (runningDocTable.IsDocumentInitialized(foundDocCookie))
                {
                    docCookie = foundDocCookie;
                    return true;
                }
            }

            return false;
        }

        public static bool IsDocumentInitialized(this IVsRunningDocumentTable4 runningDocTable, uint docCookie)
        {
            var flags = runningDocTable.GetDocumentFlags(docCookie);

            return (flags & (uint)_VSRDTFLAGS4.RDT_PendingInitialization) == 0;
        }
    }
}
