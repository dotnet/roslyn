// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed class AnalyzersFolderItemSource(
    IThreadingContext threadingContext,
    Workspace workspace,
    ProjectId projectId,
    IVsHierarchyItem projectHierarchyItem,
    IAnalyzersCommandHandler commandHandler)
    : IAttachedCollectionSource
{
    private readonly ObservableCollection<AnalyzersFolderItem> _folderItems = [new AnalyzersFolderItem(
        threadingContext,
        workspace,
        projectId,
        projectHierarchyItem,
        commandHandler.AnalyzerFolderContextMenuController)];

    public bool HasItems => true;

    public IEnumerable Items => _folderItems;

    public object SourceItem => projectHierarchyItem;
}
