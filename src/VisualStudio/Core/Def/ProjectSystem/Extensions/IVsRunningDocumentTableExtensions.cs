// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal static class IVsRunningDocumentTableExtensions
    {
        public static bool IsDocumentInitialized(this IVsRunningDocumentTable4 runningDocTable, uint docCookie)
        {
            var flags = runningDocTable.GetDocumentFlags(docCookie);

            return (flags & (uint)_VSRDTFLAGS4.RDT_PendingInitialization) == 0;
        }
    }
}
