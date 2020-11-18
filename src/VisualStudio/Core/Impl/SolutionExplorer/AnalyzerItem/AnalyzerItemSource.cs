// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal class AnalyzerItemSource : IAttachedCollectionSource, INotifyPropertyChanged
    {
        private readonly AnalyzersFolderItem _analyzersFolder;
        private readonly IAnalyzersCommandHandler _commandHandler;
        private IReadOnlyCollection<AnalyzerReference> _analyzerReferences;
        private BulkObservableCollection<AnalyzerItem> _analyzerItems;

        public event PropertyChangedEventHandler PropertyChanged;

        public AnalyzerItemSource(AnalyzersFolderItem analyzersFolder, IAnalyzersCommandHandler commandHandler)
        {
            _analyzersFolder = analyzersFolder;
            _commandHandler = commandHandler;

            _analyzersFolder.Workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionReloaded:
                    UpdateAnalyzers();
                    break;

                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.SolutionCleared:
                    _analyzersFolder.Workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
                    break;

                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.ProjectReloaded:
                case WorkspaceChangeKind.ProjectChanged:
                    if (e.ProjectId == _analyzersFolder.ProjectId)
                    {
                        UpdateAnalyzers();
                    }

                    break;

                case WorkspaceChangeKind.ProjectRemoved:
                    if (e.ProjectId == _analyzersFolder.ProjectId)
                    {
                        _analyzersFolder.Workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
                    }

                    break;
            }
        }

        private void UpdateAnalyzers()
        {
            if (_analyzerItems == null)
            {
                // The set of AnalyzerItems hasn't been realized yet. Just signal that HasItems
                // may have changed.

                NotifyPropertyChanged(nameof(HasItems));
                return;
            }

            var project = _analyzersFolder.Workspace
                            .CurrentSolution
                            .GetProject(_analyzersFolder.ProjectId);

            if (project != null &&
                project.AnalyzerReferences != _analyzerReferences)
            {
                _analyzerReferences = project.AnalyzerReferences;

                _analyzerItems.BeginBulkOperation();

                var itemsToRemove = _analyzerItems
                                        .Where(item => !_analyzerReferences.Contains(item.AnalyzerReference))
                                        .ToArray();

                var referencesToAdd = GetFilteredAnalyzers(_analyzerReferences, project)
                                        .Where(r => !_analyzerItems.Any(item => item.AnalyzerReference == r))
                                        .ToArray();

                foreach (var item in itemsToRemove)
                {
                    _analyzerItems.Remove(item);
                }

                foreach (var reference in referencesToAdd)
                {
                    _analyzerItems.Add(new AnalyzerItem(_analyzersFolder, reference, _commandHandler.AnalyzerContextMenuController));
                }

                var sorted = _analyzerItems.OrderBy(item => item.AnalyzerReference.Display).ToArray();
                for (var i = 0; i < sorted.Length; i++)
                {
                    _analyzerItems.Move(_analyzerItems.IndexOf(sorted[i]), i);
                }

                _analyzerItems.EndBulkOperation();

                NotifyPropertyChanged(nameof(HasItems));
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool HasItems
        {
            get
            {
                if (_analyzerItems != null)
                {
                    return _analyzerItems.Count > 0;
                }

                var project = _analyzersFolder.Workspace
                                                .CurrentSolution
                                                .GetProject(_analyzersFolder.ProjectId);

                if (project != null)
                {
                    return project.AnalyzerReferences.Count > 0;
                }

                return false;
            }
        }

        public IEnumerable Items
        {
            get
            {
                if (_analyzerItems == null)
                {
                    _analyzerItems = new BulkObservableCollection<AnalyzerItem>();

                    var project = _analyzersFolder.Workspace
                                                .CurrentSolution
                                                .GetProject(_analyzersFolder.ProjectId);

                    if (project != null)
                    {
                        _analyzerReferences = project.AnalyzerReferences;
                        var initialSet = GetFilteredAnalyzers(_analyzerReferences, project)
                                            .OrderBy(ar => ar.Display)
                                            .Select(ar => new AnalyzerItem(_analyzersFolder, ar, _commandHandler.AnalyzerContextMenuController));
                        _analyzerItems.AddRange(initialSet);
                    }
                }

                Logger.Log(
                    FunctionId.SolutionExplorer_AnalyzerItemSource_GetItems,
                    KeyValueLogMessage.Create(m => m["Count"] = _analyzerItems.Count));

                return _analyzerItems;
            }
        }

        public object SourceItem
        {
            get
            {
                return _analyzersFolder;
            }
        }

        private ImmutableHashSet<string> GetAnalyzersWithLoadErrors()
        {
            if (_analyzersFolder.Workspace is VisualStudioWorkspaceImpl)
            {
                /*
                var vsProject = vsWorkspace.DeferredState?.ProjectTracker.GetProject(_analyzersFolder.ProjectId);
                var vsAnalyzersMap = vsProject?.GetProjectAnalyzersMap();

                if (vsAnalyzersMap != null)
                {
                    return vsAnalyzersMap.Where(kvp => kvp.Value.HasLoadErrors).Select(kvp => kvp.Key).ToImmutableHashSet();
                }
                */
            }

            return ImmutableHashSet<string>.Empty;
        }

        private ImmutableArray<AnalyzerReference> GetFilteredAnalyzers(IEnumerable<AnalyzerReference> analyzerReferences, Project project)
        {
            var analyzersWithLoadErrors = GetAnalyzersWithLoadErrors();

            // Filter out analyzer dependencies which have no diagnostic analyzers, but still retain the unresolved analyzers and analyzers with load errors.
            var builder = ArrayBuilder<AnalyzerReference>.GetInstance();
            foreach (var analyzerReference in analyzerReferences)
            {
                // Analyzer dependency:
                // 1. Must be an Analyzer file reference (we don't understand other analyzer dependencies).
                // 2. Mush have no diagnostic analyzers.
                // 3. Must have no source generators.
                // 4. Must have non-null full path.
                // 5. Must not have any assembly or analyzer load failures.
                if (analyzerReference is AnalyzerFileReference &&
                    analyzerReference.GetAnalyzers(project.Language).IsDefaultOrEmpty &&
                    analyzerReference.GetGenerators(project.Language).IsDefaultOrEmpty &&
                    analyzerReference.FullPath != null &&
                    !analyzersWithLoadErrors.Contains(analyzerReference.FullPath))
                {
                    continue;
                }

                builder.Add(analyzerReference);
            }

            return builder.ToImmutableAndFree();
        }
    }
}
