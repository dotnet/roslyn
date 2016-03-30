// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed class GeneratedSourceFolderItemSource : IAttachedCollectionSource
    {
        private readonly IVsHierarchyItem _projectHierarchyItem;
        private readonly Workspace _workspace;
        private readonly ProjectId _projectId;
        private readonly ObservableCollection<GeneratedSourceFolderItem> _folderItems;

        public GeneratedSourceFolderItemSource(Workspace workspace, ProjectId projectId, IVsHierarchyItem projectHierarchyItem)
        {
            _workspace = workspace;
            _projectId = projectId;
            _projectHierarchyItem = projectHierarchyItem;

            _folderItems = new ObservableCollection<GeneratedSourceFolderItem>();
            _folderItems.Add(
                new GeneratedSourceFolderItem(
                    _workspace,
                    _projectId,
                    _projectHierarchyItem));
        }

        public bool HasItems
        {
            get { return true; }
        }

        public IEnumerable Items
        {
            get { return _folderItems; }
        }

        public object SourceItem
        {
            get { return _projectHierarchyItem; }
        }
    }
}
