// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class VsHierarchyExtensions
    {
        public static string GetMonikerForHierarchyAndItemId(this IVsRunningDocumentTable runningDocTable, IVsHierarchy hierarchy, uint itemid)
        {
            if (runningDocTable == null)
            {
                throw new ArgumentNullException("runningDocTable");
            }

            if (hierarchy == null)
            {
                throw new ArgumentNullException("hierarchy");
            }

            // First, get the doc cookie for this
            IEnumRunningDocuments runningDocsEnum;
            Marshal.ThrowExceptionForHR(runningDocTable.GetRunningDocumentsEnum(out runningDocsEnum));
            var cookies = new uint[1];
            uint cookiesFetched;
            while (runningDocsEnum.Next(1, cookies, out cookiesFetched) == VSConstants.S_OK && cookiesFetched == 1)
            {
                uint documentFlags;
                uint documentReadLocks;
                uint documentEditLocks;
                string documentId;
                IVsHierarchy documentHierarchy;
                uint documentItemID;
                IntPtr pDocData;

                Marshal.ThrowExceptionForHR(runningDocTable.GetDocumentInfo(cookies[0], out documentFlags, out documentReadLocks, out documentEditLocks, out documentId, out documentHierarchy, out documentItemID, out pDocData));

                try
                {
                    if (documentHierarchy == hierarchy && documentItemID == itemid)
                    {
                        return documentId;
                    }
                }
                finally
                {
                    Marshal.Release(pDocData);
                }
            }

            // Uh, OK, that's probably not good that we're supposedly an open file but not in the RDT.
            return null;
        }

        // Gets the IVsHierarchy's name property for this item.
        public static string GetDocumentNameForHierarchyAndItemId(this IVsHierarchy hierarchy, uint itemid)
        {
            object property;
            Marshal.ThrowExceptionForHR(hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_Name, out property));
            return (string)property;
        }
    }
}
