// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal abstract partial class BaseDiagnosticAndGeneratorItemSource : IAttachedCollectionSource
    {
        private static readonly DiagnosticDescriptorComparer s_comparer = new DiagnosticDescriptorComparer();

        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        private BulkObservableCollection<BaseItem>? _items;
        private ReportDiagnostic _generalDiagnosticOption;
        private ImmutableDictionary<string, ReportDiagnostic>? _specificDiagnosticOptions;
        private AnalyzerConfigData? _analyzerConfigOptions;

        public BaseDiagnosticAndGeneratorItemSource(Workspace workspace, ProjectId projectId, IAnalyzersCommandHandler commandHandler, IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            Workspace = workspace;
            ProjectId = projectId;
            CommandHandler = commandHandler;
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
        }

        public Workspace Workspace { get; }
        public ProjectId ProjectId { get; }
        protected IAnalyzersCommandHandler CommandHandler { get; }

        public abstract AnalyzerReference? AnalyzerReference { get; }

        public abstract object SourceItem { get; }

        [MemberNotNullWhen(true, nameof(AnalyzerReference))]
        public bool HasItems
        {
            get
            {
                if (_items != null)
                {
                    return _items.Count > 0;
                }

                if (AnalyzerReference == null)
                {
                    return false;
                }

                var project = Workspace.CurrentSolution.GetProject(ProjectId);

                if (project == null)
                {
                    return false;
                }

                return AnalyzerReference.GetAnalyzers(project.Language).Any() ||
                       AnalyzerReference.GetGenerators(project.Language).Any();
            }
        }

        public IEnumerable Items
        {
            get
            {
                if (_items == null)
                {
                    var project = Workspace.CurrentSolution.GetRequiredProject(ProjectId);
                    _generalDiagnosticOption = project.CompilationOptions!.GeneralDiagnosticOption;
                    _specificDiagnosticOptions = project.CompilationOptions!.SpecificDiagnosticOptions;
                    _analyzerConfigOptions = project.GetAnalyzerConfigOptions();

                    _items = CreateDiagnosticAndGeneratorItems(project.Id, project.Language, project.CompilationOptions, _analyzerConfigOptions);

                    Workspace.WorkspaceChanged += OnWorkspaceChangedLookForOptionsChanges;
                }

                Logger.Log(
                    FunctionId.SolutionExplorer_DiagnosticItemSource_GetItems,
                    KeyValueLogMessage.Create(m => m["Count"] = _items.Count));

                return _items;
            }
        }

        private BulkObservableCollection<BaseItem> CreateDiagnosticAndGeneratorItems(ProjectId projectId, string language, CompilationOptions options, AnalyzerConfigData? analyzerConfigOptions)
        {
            // Within an analyzer assembly, an individual analyzer may report multiple different diagnostics
            // with the same ID. Or, multiple analyzers may report diagnostics with the same ID. Or a
            // combination of the two may occur.
            // We only want to show one node in Solution Explorer for a given ID. So we pick one, but we need
            // to be consistent in which one we pick. Diagnostics with the same ID may have different
            // descriptions or messages, and it would be strange if the node's name changed from one run of
            // VS to another. So we group the diagnostics by ID, sort them within a group, and take the first
            // one.

            Contract.ThrowIfFalse(HasItems);

            var collection = new BulkObservableCollection<BaseItem>();
            collection.AddRange(
                AnalyzerReference.GetAnalyzers(language)
                .SelectMany(a => _diagnosticAnalyzerService.AnalyzerInfoCache.GetDiagnosticDescriptors(a))
                .GroupBy(d => d.Id)
                .OrderBy(g => g.Key, StringComparer.CurrentCulture)
                .Select(g =>
                {
                    var selectedDiagnostic = g.OrderBy(d => d, s_comparer).First();
                    var effectiveSeverity = selectedDiagnostic.GetEffectiveSeverity(options, analyzerConfigOptions?.ConfigOptions, analyzerConfigOptions?.TreeOptions);
                    return new DiagnosticItem(projectId, AnalyzerReference, selectedDiagnostic, effectiveSeverity, CommandHandler);
                }));

            collection.AddRange(
                AnalyzerReference.GetGenerators(language)
                .Select(g => new SourceGeneratorItem(projectId, g, AnalyzerReference)));

            return collection;
        }

        private void OnWorkspaceChangedLookForOptionsChanges(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind is WorkspaceChangeKind.SolutionCleared or
                WorkspaceChangeKind.SolutionReloaded or
                WorkspaceChangeKind.SolutionRemoved)
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
                var project = e.NewSolution.GetRequiredProject(ProjectId);
                var newGeneralDiagnosticOption = project.CompilationOptions!.GeneralDiagnosticOption;
                var newSpecificDiagnosticOptions = project.CompilationOptions!.SpecificDiagnosticOptions;
                var newAnalyzerConfigOptions = project.GetAnalyzerConfigOptions();

                if (newGeneralDiagnosticOption != _generalDiagnosticOption ||
                    !object.ReferenceEquals(newSpecificDiagnosticOptions, _specificDiagnosticOptions) ||
                    !object.ReferenceEquals(newAnalyzerConfigOptions?.TreeOptions, _analyzerConfigOptions?.TreeOptions) ||
                    !object.ReferenceEquals(newAnalyzerConfigOptions?.ConfigOptions, _analyzerConfigOptions?.ConfigOptions))
                {
                    _generalDiagnosticOption = newGeneralDiagnosticOption;
                    _specificDiagnosticOptions = newSpecificDiagnosticOptions;
                    _analyzerConfigOptions = newAnalyzerConfigOptions;

                    Contract.ThrowIfNull(_items, "We only subscribe to events after we create the items, so this should not be null.");

                    foreach (var item in _items.OfType<DiagnosticItem>())
                    {
                        var effectiveSeverity = item.Descriptor.GetEffectiveSeverity(project.CompilationOptions, newAnalyzerConfigOptions?.ConfigOptions, newAnalyzerConfigOptions?.TreeOptions);
                        item.UpdateEffectiveSeverity(effectiveSeverity);
                    }
                }
            }
        }
    }
}
