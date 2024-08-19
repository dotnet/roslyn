// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed class AnalyzersFolderItemSource : IAttachedCollectionSource
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

        _folderItems = [];

        Update();
    }

    public bool HasItems => true;

    public IEnumerable Items => _folderItems;

    public object SourceItem => _projectHierarchyItem;

    internal void Update()
    {
        // Don't create the item a 2nd time.
        if (_folderItems.Any())
            return;

        _folderItems.Add(new AnalyzersFolderItem(
            _workspace,
            _projectId,
            _projectHierarchyItem,
            _commandHandler.AnalyzerFolderContextMenuController));
    }
}
