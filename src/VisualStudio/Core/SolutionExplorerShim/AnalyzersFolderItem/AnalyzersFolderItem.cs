// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;
using Microsoft.VisualStudio.Shell;
using VSLangProj140;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal partial class AnalyzersFolderItem : BaseItem
    {
        private readonly Workspace _workspace;
        private readonly ProjectId _projectId;
        private readonly IVsHierarchyItem _parentItem;
        private readonly IContextMenuController _contextMenuController;

        public AnalyzersFolderItem(
            Workspace workspace,
            ProjectId projectId,
            IVsHierarchyItem parentItem,
            IContextMenuController contextMenuController)
            : base(SolutionExplorerShim.AnalyzersFolderItem_Name)
        {
            _workspace = workspace;
            _projectId = projectId;
            _parentItem = parentItem;
            _contextMenuController = contextMenuController;
        }

        public override ImageMoniker IconMoniker
        {
            get
            {
                return KnownMonikers.CodeInformation;
            }
        }

        public override ImageMoniker ExpandedIconMoniker
        {
            get
            {
                return KnownMonikers.CodeInformation;
            }
        }

        public Workspace Workspace
        {
            get { return _workspace; }
        }

        public ProjectId ProjectId
        {
            get { return _projectId; }
        }

        public IVsHierarchyItem ParentItem
        {
            get { return _parentItem; }
        }

        public override object GetBrowseObject()
        {
            return new BrowseObject(this);
        }

        public override IContextMenuController ContextMenuController
        {
            get { return _contextMenuController; }
        }

        internal Project GetProject()
        {
            return _workspace.CurrentSolution.GetProject(_projectId);
        }

        /// <summary>
        /// Get the DTE object for the Project.
        /// </summary>
        private VSProject3 GetVSProject()
        {
            var vsWorkspace = _workspace as VisualStudioWorkspaceImpl;
            if (vsWorkspace == null)
            {
                return null;
            }

            var hierarchy = vsWorkspace.GetHierarchy(_projectId);
            if (hierarchy == null)
            {
                return null;
            }

            EnvDTE.Project project = null;
            if (hierarchy.TryGetProject(out project))
            {
                var vsproject = project.Object as VSProject3;
                return vsproject;
            }

            return null;
        }

        /// <summary>
        /// Add an analyzer with the given path to this folder.
        /// </summary>
        public void AddAnalyzer(string path)
        {
            var vsproject = GetVSProject();
            if (vsproject == null)
            {
                return;
            }

            vsproject.AnalyzerReferences.Add(path);
        }

        /// <summary>
        /// Remove an analyzer with the given path from this folder.
        /// </summary>
        public void RemoveAnalyzer(string path)
        {
            var vsproject = GetVSProject();
            if (vsproject == null)
            {
                return;
            }

            vsproject.AnalyzerReferences.Remove(path);
        }
    }
}
