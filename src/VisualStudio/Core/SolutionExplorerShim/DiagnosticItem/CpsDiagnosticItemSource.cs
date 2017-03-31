// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal partial class CpsDiagnosticItemSource : IAttachedCollectionSource, INotifyPropertyChanged
    {
        private static readonly DiagnosticDescriptorComparer s_comparer = new DiagnosticDescriptorComparer();

        private readonly Workspace _workspace;
        private readonly ProjectId _projectId;
        private readonly IVsHierarchyItem _item;
        private readonly IAnalyzersCommandHandler _commandHandler;
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        private AnalyzerReference _analyzerReference;
        private BulkObservableCollection<CpsDiagnosticItem> _diagnosticItems;
        private ReportDiagnostic _generalDiagnosticOption;
        private ImmutableDictionary<string, ReportDiagnostic> _specificDiagnosticOptions;

        public event PropertyChangedEventHandler PropertyChanged;

        public CpsDiagnosticItemSource(Workspace workspace, ProjectId projectId, IVsHierarchyItem item, IAnalyzersCommandHandler commandHandler, IDiagnosticAnalyzerService analyzerService)
        {
            _workspace = workspace;
            _projectId = projectId;
            _item = item;
            _commandHandler = commandHandler;
            _diagnosticAnalyzerService = analyzerService;

            _analyzerReference = TryGetAnalyzerReference(_workspace.CurrentSolution);
            if (_analyzerReference == null)
            {
                // The workspace doesn't know about the project and/or the analyzer yet.
                // Hook up an event handler so we can update when it does.
                _workspace.WorkspaceChanged += OnWorkspaceChangedLookForAnalyzer;
            }
        }

        public IContextMenuController DiagnosticItemContextMenuController => _commandHandler.DiagnosticContextMenuController;
        public Workspace Workspace => _workspace;
        public ProjectId ProjectId => _projectId;
        public AnalyzerReference AnalyzerReference => _analyzerReference;

        public object SourceItem => _item;

        public bool HasItems
        {
            get
            {
                if (_diagnosticItems != null)
                {
                    return _diagnosticItems.Count > 0;
                }

                if (_analyzerReference == null)
                {
                    return false;
                }

                var project = _workspace.CurrentSolution.GetProject(_projectId);
                return _analyzerReference.GetAnalyzers(project.Language).Length > 0;
            }
        }

        public IEnumerable Items
        {
            get
            {
                if (_diagnosticItems == null)
                {
                    var project = _workspace.CurrentSolution.GetProject(_projectId);
                    _generalDiagnosticOption = project.CompilationOptions.GeneralDiagnosticOption;
                    _specificDiagnosticOptions = project.CompilationOptions.SpecificDiagnosticOptions;

                    _diagnosticItems = new BulkObservableCollection<CpsDiagnosticItem>();
                    _diagnosticItems.AddRange(GetDiagnosticItems(project.Language, project.CompilationOptions));

                    _workspace.WorkspaceChanged += OnWorkspaceChangedLookForOptionsChanges;
                }

                Logger.Log(
                    FunctionId.SolutionExplorer_DiagnosticItemSource_GetItems,
                    KeyValueLogMessage.Create(m => m["Count"] = _diagnosticItems.Count));

                return _diagnosticItems;
            }
        }

        private IEnumerable<CpsDiagnosticItem> GetDiagnosticItems(string language, CompilationOptions options)
        {
            // Within an analyzer assembly, an individual analyzer may report multiple different diagnostics
            // with the same ID. Or, multiple analyzers may report diagnostics with the same ID. Or a
            // combination of the two may occur.
            // We only want to show one node in Solution Explorer for a given ID. So we pick one, but we need
            // to be consistent in which one we pick. Diagnostics with the same ID may have different
            // descriptions or messages, and it would be strange if the node's name changed from one run of
            // VS to another. So we group the diagnostics by ID, sort them within a group, and take the first
            // one.

            return _analyzerReference.GetAnalyzers(language)
                .SelectMany(a => _diagnosticAnalyzerService.GetDiagnosticDescriptors(a))
                .GroupBy(d => d.Id)
                .OrderBy(g => g.Key, StringComparer.CurrentCulture)
                .Select(g =>
                {
                    var selectedDiagnostic = g.OrderBy(d => d, s_comparer).First();
                    var effectiveSeverity = selectedDiagnostic.GetEffectiveSeverity(options);
                    return new CpsDiagnosticItem(this, selectedDiagnostic, effectiveSeverity);
                });
        }

        private void OnWorkspaceChangedLookForAnalyzer(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.SolutionCleared ||
                e.Kind == WorkspaceChangeKind.SolutionReloaded ||
                e.Kind == WorkspaceChangeKind.SolutionRemoved)
            {
                _workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;
            }
            else if (e.Kind == WorkspaceChangeKind.SolutionAdded)
            {
                _analyzerReference = TryGetAnalyzerReference(e.NewSolution);
                if (_analyzerReference != null)
                {
                    _workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
                }
            }
            else if (e.ProjectId == _projectId)
            {
                if (e.Kind == WorkspaceChangeKind.ProjectRemoved)
                {
                    _workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;
                }
                else if (e.Kind == WorkspaceChangeKind.ProjectAdded
                         || e.Kind == WorkspaceChangeKind.ProjectChanged)
                {
                    _analyzerReference = TryGetAnalyzerReference(e.NewSolution);
                    if (_analyzerReference != null)
                    {
                        _workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;

                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
                    }
                }
            }
        }

        private void OnWorkspaceChangedLookForOptionsChanges(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.SolutionCleared ||
                e.Kind == WorkspaceChangeKind.SolutionReloaded ||
                e.Kind == WorkspaceChangeKind.SolutionRemoved)
            {
                _workspace.WorkspaceChanged -= OnWorkspaceChangedLookForOptionsChanges;
            }
            else if (e.ProjectId == _projectId)
            {
                if (e.Kind == WorkspaceChangeKind.ProjectRemoved)
                {
                    _workspace.WorkspaceChanged -= OnWorkspaceChangedLookForOptionsChanges;
                }
                else if (e.Kind == WorkspaceChangeKind.ProjectChanged)
                {
                    var project = e.NewSolution.GetProject(_projectId);
                    var newGeneralDiagnosticOption = project.CompilationOptions.GeneralDiagnosticOption;
                    var newSpecificDiagnosticOptions = project.CompilationOptions.SpecificDiagnosticOptions;

                    if (newGeneralDiagnosticOption != _generalDiagnosticOption ||
                        !object.ReferenceEquals(newSpecificDiagnosticOptions, _specificDiagnosticOptions))
                    {
                        _generalDiagnosticOption = newGeneralDiagnosticOption;
                        _specificDiagnosticOptions = newSpecificDiagnosticOptions;

                        foreach (var item in _diagnosticItems)
                        {
                            var effectiveSeverity = item.Descriptor.GetEffectiveSeverity(project.CompilationOptions);
                            item.UpdateEffectiveSeverity(effectiveSeverity);
                        }
                    }
                }
            }
        }

        private AnalyzerReference TryGetAnalyzerReference(Solution solution)
        {
            var project = solution.GetProject(_projectId);

            if (project == null)
            {
                return null;
            }

            return project.AnalyzerReferences.SingleOrDefault(r => r.FullPath.Equals(_item.CanonicalName, StringComparison.OrdinalIgnoreCase));
        }
    }
}