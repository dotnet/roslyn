// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal class AnalyzersFolderItemSource : IAttachedCollectionSource
    {
        private readonly IVsHierarchyItem _projectHierarchyItem;
        private readonly Workspace _workspace;
        private readonly ProjectId _projectId;
        private readonly ObservableCollection<AnalyzersFolderItem> _folderItems;
        private readonly IAnalyzersCommandHandler _commandHandler;

        public AnalyzersFolderItemSource(Workspace workspace, ProjectId projectId, IVsHierarchyItem projectHierarchyItem, IAnalyzersCommandHandler commandHandler)
        {
            _workspace = workspace;
            _projectId = projectId;
            _projectHierarchyItem = projectHierarchyItem;
            _commandHandler = commandHandler;

            _folderItems = new ObservableCollection<AnalyzersFolderItem>();
            _folderItems.Add(
                new AnalyzersFolderItem(
                    _workspace,
                    _projectId,
                    _projectHierarchyItem,
                    _commandHandler.AnalyzerFolderContextMenuController));
        }

        public bool HasItems
        {
            get
            {
                return true;
            }
        }

        public IEnumerable Items
        {
            get
            {
                return _folderItems;
            }
        }

        public object SourceItem
        {
            get
            {
                return _projectHierarchyItem;
            }
        }
    }
}
