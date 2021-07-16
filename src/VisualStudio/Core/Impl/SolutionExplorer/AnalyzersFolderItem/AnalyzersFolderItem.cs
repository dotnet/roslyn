// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using VSLangProj140;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal partial class AnalyzersFolderItem : BaseItem
    {
        private readonly IContextMenuController _contextMenuController;

        public AnalyzersFolderItem(
            Workspace workspace,
            ProjectId projectId,
            IVsHierarchyItem parentItem,
            IContextMenuController contextMenuController)
            : base(SolutionExplorerShim.Analyzers)
        {
            Workspace = workspace;
            ProjectId = projectId;
            ParentItem = parentItem;
            _contextMenuController = contextMenuController;
        }

        public override ImageMoniker IconMoniker => KnownMonikers.CodeInformation;

        public Workspace Workspace { get; }

        public ProjectId ProjectId { get; }

        public IVsHierarchyItem ParentItem { get; }

        public override object GetBrowseObject()
        {
            return new BrowseObject(this);
        }

        public override IContextMenuController ContextMenuController
        {
            get { return _contextMenuController; }
        }

        /// <summary>
        /// Get the DTE object for the Project.
        /// </summary>
        private VSProject3? GetVSProject()
        {
            var vsWorkspace = Workspace as VisualStudioWorkspace;
            if (vsWorkspace == null)
            {
                return null;
            }

            var hierarchy = vsWorkspace.GetHierarchy(ProjectId);
            if (hierarchy == null)
            {
                return null;
            }

            if (hierarchy.TryGetProject(out var project))
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
