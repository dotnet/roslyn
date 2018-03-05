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
            // runningDocumentTable might be null for tests.
            return runningDocumentTable != null && document.Project.Hierarchy == GetContextHierarchy(document, runningDocumentTable);
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
            if (!runningDocumentTable.TryGetCookieForInitializedDocument(document.Key.Moniker, out var docCookie))
            {
                return null;
            }

            runningDocumentTable.GetDocumentHierarchyItem(docCookie, out var hierarchy, out var itemid);

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
            if (hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID7.VSHPROPID_SharedItemContextHierarchy, out var contextHierarchy) != VSConstants.S_OK)
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
            if (headProjectHierarchy.GetProperty(itemId, (int)__VSHPROPID7.VSHPROPID_IsSharedItem, out var isShared) != VSConstants.S_OK || !(bool)isShared)
            {
                return null;
            }

            return headProjectHierarchy.GetProperty(itemId, (int)__VSHPROPID7.VSHPROPID_SharedProjectHierarchy, out var sharedHierarchy) == VSConstants.S_OK
                ? sharedHierarchy as IVsHierarchy
                : null;
        }

        public static AbstractProject GetContextHostProject(IVsHierarchy sharedHierarchy, VisualStudioProjectTracker projectTracker)
        {
            return s_singleton.GetContextHostProjectInternal(sharedHierarchy, projectTracker);
        }

        private AbstractProject GetContextHostProjectInternal(IVsHierarchy hierarchy, VisualStudioProjectTracker projectTracker)
        {
            hierarchy = GetSharedItemContextHierarchy(hierarchy) ?? hierarchy;
            var projectName = GetActiveIntellisenseProjectContextInternal(hierarchy);

            if (projectName != null)
            {
                return projectTracker.ImmutableProjects.FirstOrDefault(p => p.ProjectSystemName == projectName);
            }
            else
            {
                return projectTracker.ImmutableProjects.FirstOrDefault(p => p.Hierarchy == hierarchy);
            }
        }

        private string GetActiveIntellisenseProjectContextInternal(IVsHierarchy hierarchy)
        {
            AssertIsForeground();

            hierarchy = GetSharedItemContextHierarchy(hierarchy) ?? hierarchy;
            return hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext, out var intellisenseProjectName) == VSConstants.S_OK
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
            VSDOCUMENTPRIORITY[] priority = new VSDOCUMENTPRIORITY[1];

            if (ErrorHandler.Succeeded(((IVsProject)hierarchy).GetMkDocument(itemId, out var fullPath))
                && ErrorHandler.Succeeded(((IVsProject)sharedHierarchy).IsDocumentInProject(fullPath, out var found, priority, out itemIdInSharedHierarchy))
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
