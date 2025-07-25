// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal static class IVsRunningDocumentTableExtensions
{
    extension(IVsRunningDocumentTable4 runningDocTable)
    {
        public bool IsDocumentInitialized(uint docCookie)
        {
            var flags = runningDocTable.GetDocumentFlags(docCookie);

            return (flags & (uint)_VSRDTFLAGS4.RDT_PendingInitialization) == 0;
        }
    }

    extension(IVsRunningDocumentTable3 runningDocumentTable)
    {
        public IEnumerable<uint> GetRunningDocuments()
        => GetRunningDocuments((IVsRunningDocumentTable)runningDocumentTable);
    }

    extension(IVsRunningDocumentTable runningDocumentTable)
    {
        public IEnumerable<uint> GetRunningDocuments()
        {
            ErrorHandler.ThrowOnFailure(runningDocumentTable.GetRunningDocumentsEnum(out var enumRunningDocuments));
            var cookies = new uint[16];

            while (ErrorHandler.Succeeded(enumRunningDocuments.Next((uint)cookies.Length, cookies, out var cookiesFetched))
                && cookiesFetched > 0)
            {
                for (var cookieIndex = 0; cookieIndex < cookiesFetched; cookieIndex++)
                {
                    yield return cookies[cookieIndex];
                }
            }
        }
    }
}
