// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Language.Intellisense;
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

                NotifyPropertyChanged("HasItems");
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

                var referencesToAdd = _analyzerReferences
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
                for (int i = 0; i < sorted.Length; i++)
                {
                    _analyzerItems.Move(_analyzerItems.IndexOf(sorted[i]), i);
                }

                _analyzerItems.EndBulkOperation();

                NotifyPropertyChanged("HasItems");
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
                        var initialSet = _analyzerReferences
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
    }
}
