// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal class LinkedFileUtilities : ForegroundThreadAffinitizedObject
    {
        private LinkedFileUtilities()
        {
        }

        private static LinkedFileUtilities s_singleton = new LinkedFileUtilities();

        /// <summary>
        /// Finds the current context hierarchy for the given document. If the document is in a
        /// Shared Code project, this returns that project's SharedItemContextHierarchy. If the
        /// document is linked into multiple projects, this returns the hierarchy in which it is
        /// currently open as indicated by the running document table. Otherwise, it returns the
        /// hierarchy of the document's project.
        /// </summary>
        public static IVsHierarchy GetContextHierarchy(IVisualStudioHostDocument document, IVsRunningDocumentTable4 runningDocumentTable)
        {
            return s_singleton.GetContextHierarchyInternal(document, runningDocumentTable);
        }

        private IVsHierarchy GetContextHierarchyInternal(IVisualStudioHostDocument document, IVsRunningDocumentTable4 runningDocumentTable)
        {
            AssertIsForeground();

            return GetSharedItemContextHierarchy(document) ?? GetContextHierarchyFromRunningDocumentTable(document, runningDocumentTable) ?? document.Project.Hierarchy;
        }

        /// <summary>
        /// If the document is open in the running document table, this returns the hierarchy in
        /// which it is currently open. Otherwise, it returns null.
        /// </summary>
        private IVsHierarchy GetContextHierarchyFromRunningDocumentTable(IVisualStudioHostDocument document, IVsRunningDocumentTable4 runningDocumentTable)
        {
            AssertIsForeground();

            uint docCookie;
            if (!runningDocumentTable.TryGetCookieForInitializedDocument(document.Key.Moniker, out docCookie))
            {
                return null;
            }

            IVsHierarchy hierarchy;
            uint itemid;
            runningDocumentTable.GetDocumentHierarchyItem(docCookie, out hierarchy, out itemid);

            return hierarchy;
        }

        /// <summary>
        /// If the document is in a Shared Code project, this returns that project's 
        /// SharedItemContextHierarchy. Otherwise, it returns null. 
        /// </summary>
        private IVsHierarchy GetSharedItemContextHierarchy(IVisualStudioHostDocument document)
        {
            AssertIsForeground();

            var hierarchy = document.Project.Hierarchy;
            var itemId = document.GetItemId();
            if (itemId == (uint)VSConstants.VSITEMID.Nil)
            {
                // the document is no longer part of the solution
                return null;
            }

            var sharedHierarchy = GetSharedHierarchyForItem(hierarchy, itemId);
            if (sharedHierarchy == null)
            {
                return null;
            }

            return GetSharedItemContextHierarchy(sharedHierarchy);
        }

        /// <summary>
        /// If the project is in a Shared Code project, this returns its 
        /// SharedItemContextHierarchy. Otherwise, it returns null.
        /// </summary>
        public static IVsHierarchy GetSharedItemContextHierarchy(IVsHierarchy hierarchy)
        {
            return s_singleton.GetSharedItemContextHierarchyInternal(hierarchy);
        }

        private IVsHierarchy GetSharedItemContextHierarchyInternal(IVsHierarchy hierarchy)
        {
            object contextHierarchy;
            if (hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID7.VSHPROPID_SharedItemContextHierarchy, out contextHierarchy) != VSConstants.S_OK)
            {
                return null;
            }

            return contextHierarchy as IVsHierarchy;
        }

        /// <summary>
        /// If the itemId represents a document from a Shared Code project, this returns the 
        /// SharedProjectHierarchy to which it belongs. Otherwise, it returns null.
        /// </summary>
        public static IVsHierarchy GetSharedHierarchyForItem(IVsHierarchy headProjectHierarchy, uint itemId)
        {
            return s_singleton.GetSharedHierarchyForItemInternal(headProjectHierarchy, itemId);
        }

        private IVsHierarchy GetSharedHierarchyForItemInternal(IVsHierarchy headProjectHierarchy, uint itemId)
        {
            AssertIsForeground();

            object isShared;
            if (headProjectHierarchy.GetProperty(itemId, (int)__VSHPROPID7.VSHPROPID_IsSharedItem, out isShared) != VSConstants.S_OK || !(bool)isShared)
            {
                return null;
            }

            object sharedHierarchy;
            return headProjectHierarchy.GetProperty(itemId, (int)__VSHPROPID7.VSHPROPID_SharedProjectHierarchy, out sharedHierarchy) == VSConstants.S_OK
                ? sharedHierarchy as IVsHierarchy
                : null;
        }
    }
}
