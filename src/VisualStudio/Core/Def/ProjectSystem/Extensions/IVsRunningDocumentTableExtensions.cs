// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal static class IVsRunningDocumentTableExtensions
{
    public static bool IsDocumentInitialized(this IVsRunningDocumentTable4 runningDocTable, uint docCookie)
    {
        var flags = runningDocTable.GetDocumentFlags(docCookie);

        return (flags & (uint)_VSRDTFLAGS4.RDT_PendingInitialization) == 0;
    }

    public static IEnumerable<uint> GetRunningDocuments(this IVsRunningDocumentTable3 runningDocumentTable)
        => GetRunningDocuments((IVsRunningDocumentTable)runningDocumentTable);

    public static IEnumerable<uint> GetRunningDocuments(this IVsRunningDocumentTable runningDocumentTable)
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
