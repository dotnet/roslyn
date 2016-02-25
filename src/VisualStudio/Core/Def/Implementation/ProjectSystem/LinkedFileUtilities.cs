// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed class LinkedFileUtilities : ForegroundThreadAffinitizedObject
    {
        private LinkedFileUtilities()
        {
        }

        private static readonly LinkedFileUtilities s_singleton = new LinkedFileUtilities();

        public static bool IsCurrentContextHierarchy(IVisualStudioHostDocument document, IVsRunningDocumentTable4 runningDocumentTable)
        {
            return document.Project.Hierarchy == GetContextHierarchy(document, runningDocumentTable);
        }

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

            var itemId = document.GetItemId();
            if (itemId == (uint)VSConstants.VSITEMID.Nil)
            {
                // the document is no longer part of the solution
                return null;
            }

            var sharedHierarchy = GetSharedHierarchyForItem(document.Project.Hierarchy, itemId);
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
        private IVsHierarchy GetSharedItemContextHierarchy(IVsHierarchy hierarchy)
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

        public static IVisualStudioHostProject GetContextHostProject(IVsHierarchy sharedHierarchy, IVisualStudioHostProjectContainer hostProjectContainer)
        {
            return s_singleton.GetContextHostProjectInternal(sharedHierarchy, hostProjectContainer);
        }

        private IVisualStudioHostProject GetContextHostProjectInternal(IVsHierarchy hierarchy, IVisualStudioHostProjectContainer hostProjectContainer)
        {
            hierarchy = GetSharedItemContextHierarchy(hierarchy) ?? hierarchy;
            var projectName = GetActiveIntellisenseProjectContextInternal(hierarchy);

            if (projectName != null)
            {
                return hostProjectContainer.GetProjects().FirstOrDefault(p => p.ProjectSystemName == projectName);
            }
            else
            {
                return hostProjectContainer.GetProjects().FirstOrDefault(p => p.Hierarchy == hierarchy);
            }
        }

        private string GetActiveIntellisenseProjectContextInternal(IVsHierarchy hierarchy)
        {
            AssertIsForeground();

            hierarchy = GetSharedItemContextHierarchy(hierarchy) ?? hierarchy;

            object intellisenseProjectName;
            return hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext, out intellisenseProjectName) == VSConstants.S_OK
                ? intellisenseProjectName as string
                : null;
        }

        public static bool TryGetSharedHierarchyAndItemId(IVsHierarchy hierarchy, uint itemId, out IVsHierarchy sharedHierarchy, out uint itemIdInSharedHierarchy)
        {
            return s_singleton.TryGetSharedHierarchyAndItemIdInternal(hierarchy, itemId, out sharedHierarchy, out itemIdInSharedHierarchy);
        }

        private bool TryGetSharedHierarchyAndItemIdInternal(IVsHierarchy hierarchy, uint itemId, out IVsHierarchy sharedHierarchy, out uint itemIdInSharedHierarchy)
        {
            AssertIsForeground();

            sharedHierarchy = null;
            itemIdInSharedHierarchy = (uint)VSConstants.VSITEMID.Nil;

            if (hierarchy == null)
            {
                return false;
            }

            sharedHierarchy = s_singleton.GetSharedHierarchyForItemInternal(hierarchy, itemId);

            return sharedHierarchy == null
                ? false
                : s_singleton.TryGetItemIdInSharedHierarchyInternal(hierarchy, itemId, sharedHierarchy, out itemIdInSharedHierarchy);
        }

        private bool TryGetItemIdInSharedHierarchyInternal(IVsHierarchy hierarchy, uint itemId, IVsHierarchy sharedHierarchy, out uint itemIdInSharedHierarchy)
        {
            string fullPath;
            int found;
            VSDOCUMENTPRIORITY[] priority = new VSDOCUMENTPRIORITY[1];

            if (ErrorHandler.Succeeded(((IVsProject)hierarchy).GetMkDocument(itemId, out fullPath))
                && ErrorHandler.Succeeded(((IVsProject)sharedHierarchy).IsDocumentInProject(fullPath, out found, priority, out itemIdInSharedHierarchy))
                && found != 0
                && itemIdInSharedHierarchy != (uint)VSConstants.VSITEMID.Nil)
            {
                return true;
            }

            itemIdInSharedHierarchy = (uint)VSConstants.VSITEMID.Nil;
            return false;
        }

        /// <summary>
        /// Check whether given project is project k project.
        /// </summary>
        public static bool IsProjectKProject(Project project)
        {
            // TODO: we need better way to see whether a project is project k project or not.
            if (project.FilePath == null)
            {
                return false;
            }

            return project.FilePath.EndsWith(".xproj", StringComparison.InvariantCultureIgnoreCase) ||
                   project.FilePath.EndsWith(".kproj", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
