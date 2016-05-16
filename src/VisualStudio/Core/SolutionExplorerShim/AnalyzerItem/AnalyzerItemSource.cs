// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal sealed class AnalyzerItemSource : ProjectItemSource
    {
        private readonly AnalyzersFolderItem _analyzersFolder;
        private readonly IAnalyzersCommandHandler _commandHandler;
        private IReadOnlyCollection<AnalyzerReference> _analyzerReferences;
        private BulkObservableCollection<AnalyzerItem> _analyzerItems;

        public AnalyzerItemSource(AnalyzersFolderItem analyzersFolder, IAnalyzersCommandHandler commandHandler) :
            base(analyzersFolder, analyzersFolder.Workspace, analyzersFolder.ProjectId)
        {
            _analyzersFolder = analyzersFolder;
            _commandHandler = commandHandler;
        }

        protected override void Update()
        {
            if (_analyzerItems == null)
            {
                // The set of AnalyzerItems hasn't been realized yet. Just signal that HasItems
                // may have changed.

                NotifyPropertyChanged(nameof(HasItems));
                return;
            }

            var project = _analyzersFolder.GetProject();
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

                NotifyPropertyChanged(nameof(HasItems));
            }
        }

        public override bool HasItems
        {
            get
            {
                if (_analyzerItems != null)
                {
                    return _analyzerItems.Count > 0;
                }

                var project = _analyzersFolder.GetProject();
                if (project != null)
                {
                    return project.AnalyzerReferences.Count > 0;
                }

                return false;
            }
        }

        public override IEnumerable Items
        {
            get
            {
                if (_analyzerItems == null)
                {
                    _analyzerItems = new BulkObservableCollection<AnalyzerItem>();

                    var project = _analyzersFolder.GetProject();
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
    }
}
