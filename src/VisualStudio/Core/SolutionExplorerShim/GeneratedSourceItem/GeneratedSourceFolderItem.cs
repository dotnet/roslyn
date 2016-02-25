// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.SolutionExplorer;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed class GeneratedSourceFolderItem : BaseItem
    {
        private readonly Workspace _workspace;
        private readonly ProjectId _projectId;
        private readonly IVsHierarchyItem _parentItem;

        public GeneratedSourceFolderItem(
            Workspace workspace,
            ProjectId projectId,
            IVsHierarchyItem parentItem)
            : base(SolutionExplorerShim.GeneratedSourceFolderItem_Name)
        {
            _workspace = workspace;
            _projectId = projectId;
            _parentItem = parentItem;
        }

        public override ImageMoniker IconMoniker
        {
            get { return KnownMonikers.FolderClosed; }
        }

        public override ImageMoniker ExpandedIconMoniker
        {
            get { return KnownMonikers.FolderOpened; }
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

        internal Project GetProject()
        {
            return _workspace.CurrentSolution.GetProject(_projectId);
        }
    }
}
