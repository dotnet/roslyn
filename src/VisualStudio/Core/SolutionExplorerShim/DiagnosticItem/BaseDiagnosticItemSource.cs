// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal abstract partial class BaseDiagnosticItemSource : IAttachedCollectionSource
    {
        private static readonly DiagnosticDescriptorComparer s_comparer = new DiagnosticDescriptorComparer();

        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        private BulkObservableCollection<BaseDiagnosticItem> _diagnosticItems;
        private ReportDiagnostic _generalDiagnosticOption;
        private ImmutableDictionary<string, ReportDiagnostic> _specificDiagnosticOptions;
        private AnalyzerConfigOptionsResult? _analyzerConfigOptions;

        public BaseDiagnosticItemSource(Workspace workspace, ProjectId projectId, IAnalyzersCommandHandler commandHandler, IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            Workspace = workspace;
            ProjectId = projectId;
            CommandHandler = commandHandler;
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
        }

        public Workspace Workspace { get; }
        public ProjectId ProjectId { get; }
        protected IAnalyzersCommandHandler CommandHandler { get; }

        public abstract AnalyzerReference AnalyzerReference { get; }
        protected abstract BaseDiagnosticItem CreateItem(DiagnosticDescriptor diagnostic, ReportDiagnostic effectiveSeverity, string language);

        public abstract object SourceItem { get; }

        public bool HasItems
        {
            get
            {
                if (_diagnosticItems != null)
                {
                    return _diagnosticItems.Count > 0;
                }

                if (AnalyzerReference == null)
                {
                    return false;
                }

                var project = Workspace.CurrentSolution.GetProject(ProjectId);
                return AnalyzerReference.GetAnalyzers(project.Language).Length > 0;
            }
        }

        public IEnumerable Items
        {
            get
            {
                if (_diagnosticItems == null)
                {
                    var project = Workspace.CurrentSolution.GetProject(ProjectId);
                    _generalDiagnosticOption = project.CompilationOptions.GeneralDiagnosticOption;
                    _specificDiagnosticOptions = project.CompilationOptions.SpecificDiagnosticOptions;
                    _analyzerConfigOptions = project.GetAnalyzerConfigOptions();

                    _diagnosticItems = new BulkObservableCollection<BaseDiagnosticItem>();
                    _diagnosticItems.AddRange(GetDiagnosticItems(project.Language, project.CompilationOptions, _analyzerConfigOptions));

                    Workspace.WorkspaceChanged += OnWorkspaceChangedLookForOptionsChanges;
                }

                Logger.Log(
                    FunctionId.SolutionExplorer_DiagnosticItemSource_GetItems,
                    KeyValueLogMessage.Create(m => m["Count"] = _diagnosticItems.Count));

                return _diagnosticItems;
            }
        }

        private IEnumerable<BaseDiagnosticItem> GetDiagnosticItems(string language, CompilationOptions options, AnalyzerConfigOptionsResult? analyzerConfigOptions)
        {
            // Within an analyzer assembly, an individual analyzer may report multiple different diagnostics
            // with the same ID. Or, multiple analyzers may report diagnostics with the same ID. Or a
            // combination of the two may occur.
            // We only want to show one node in Solution Explorer for a given ID. So we pick one, but we need
            // to be consistent in which one we pick. Diagnostics with the same ID may have different
            // descriptions or messages, and it would be strange if the node's name changed from one run of
            // VS to another. So we group the diagnostics by ID, sort them within a group, and take the first
            // one.

            return AnalyzerReference.GetAnalyzers(language)
                .SelectMany(a => _diagnosticAnalyzerService.AnalyzerInfoCache.GetDiagnosticDescriptors(a))
                .GroupBy(d => d.Id)
                .OrderBy(g => g.Key, StringComparer.CurrentCulture)
                .Select(g =>
                {
                    var selectedDiagnostic = g.OrderBy(d => d, s_comparer).First();
                    var effectiveSeverity = selectedDiagnostic.GetEffectiveSeverity(options, analyzerConfigOptions);
                    return CreateItem(selectedDiagnostic, effectiveSeverity, language);
                });
        }

        private void OnWorkspaceChangedLookForOptionsChanges(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.SolutionCleared ||
                e.Kind == WorkspaceChangeKind.SolutionReloaded ||
                e.Kind == WorkspaceChangeKind.SolutionRemoved)
            {
                Workspace.WorkspaceChanged -= OnWorkspaceChangedLookForOptionsChanges;
            }
            else if (e.ProjectId == ProjectId)
            {
                if (e.Kind == WorkspaceChangeKind.ProjectRemoved)
                {
                    Workspace.WorkspaceChanged -= OnWorkspaceChangedLookForOptionsChanges;
                }
                else if (e.Kind == WorkspaceChangeKind.ProjectChanged)
                {
                    OnProjectConfigurationChanged();
                }
                else if (e.DocumentId != null)
                {
                    switch (e.Kind)
                    {
                        case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
                        case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                        case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                        case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                            OnProjectConfigurationChanged();
                            break;
                    }
                }
            }

            return;

            // Local functions.
            void OnProjectConfigurationChanged()
            {
                var project = e.NewSolution.GetProject(ProjectId);
                var newGeneralDiagnosticOption = project.CompilationOptions.GeneralDiagnosticOption;
                var newSpecificDiagnosticOptions = project.CompilationOptions.SpecificDiagnosticOptions;
                var newAnalyzerConfigOptions = project.GetAnalyzerConfigOptions();

                if (newGeneralDiagnosticOption != _generalDiagnosticOption ||
                    !object.ReferenceEquals(newSpecificDiagnosticOptions, _specificDiagnosticOptions) ||
                    !object.ReferenceEquals(newAnalyzerConfigOptions?.TreeOptions, _analyzerConfigOptions?.TreeOptions) ||
                    !object.ReferenceEquals(newAnalyzerConfigOptions?.AnalyzerOptions, _analyzerConfigOptions?.AnalyzerOptions))
                {
                    _generalDiagnosticOption = newGeneralDiagnosticOption;
                    _specificDiagnosticOptions = newSpecificDiagnosticOptions;
                    _analyzerConfigOptions = newAnalyzerConfigOptions;

                    foreach (var item in _diagnosticItems)
                    {
                        var effectiveSeverity = item.Descriptor.GetEffectiveSeverity(project.CompilationOptions, newAnalyzerConfigOptions);
                        item.UpdateEffectiveSeverity(effectiveSeverity);
                    }
                }
            }
        }
    }
}
