// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Suppression
{
    /// <summary>
    /// Service to compute and apply bulk suppression fixes.
    /// </summary>
    [Export(typeof(IVisualStudioSuppressionFixService))]
    internal sealed class VisualStudioSuppressionFixService : IVisualStudioSuppressionFixService
    {
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IWpfTableControl _tableControl;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly ICodeFixService _codeFixService;
        private readonly IFixMultipleOccurrencesService _fixMultipleOccurencesService;
        private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public VisualStudioSuppressionFixService(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl workspace,
            IDiagnosticAnalyzerService diagnosticService,
            ICodeFixService codeFixService,
            IVisualStudioDiagnosticListSuppressionStateService suppressionStateService,
            IWaitIndicator waitIndicator)
        {
            _workspace = workspace;
            _diagnosticService = diagnosticService;
            _codeFixService = codeFixService;
            _suppressionStateService = (VisualStudioDiagnosticListSuppressionStateService)suppressionStateService;
            _waitIndicator = waitIndicator;
            _fixMultipleOccurencesService = workspace.Services.GetService<IFixMultipleOccurrencesService>();

            var errorList = serviceProvider.GetService(typeof(SVsErrorList)) as IErrorList;
            _tableControl = errorList?.TableControl;
        }

        public void AddSuppressions(IVsHierarchy projectHierarchyOpt)
        {
            Func<Project, bool> shouldFixInProject = GetShouldFixInProjectDelegate(_workspace, projectHierarchyOpt);
            
            // Apply suppressions fix in global suppressions file for non-compiler diagnostics and
            // in source only for compiler diagnostics.
            ApplySuppressionFix(shouldFixInProject, selectedEntriesOnly: false, isAddSuppression: true, isSuppressionInSource: false, onlyCompilerDiagnostics: false, showPreviewChangesDialog: false);
            ApplySuppressionFix(shouldFixInProject, selectedEntriesOnly: false, isAddSuppression: true, isSuppressionInSource: true, onlyCompilerDiagnostics: true, showPreviewChangesDialog: false);
        }

        public void AddSuppressions(bool selectedErrorListEntriesOnly, bool suppressInSource, IVsHierarchy projectHierarchyOpt)
        {
            if (_tableControl == null)
            {
                return;
            }

            Func<Project, bool> shouldFixInProject = GetShouldFixInProjectDelegate(_workspace, projectHierarchyOpt);
            ApplySuppressionFix(shouldFixInProject, selectedErrorListEntriesOnly, isAddSuppression: true, isSuppressionInSource: suppressInSource, onlyCompilerDiagnostics: false, showPreviewChangesDialog: true);
        }

        private static Func<Project, bool> GetShouldFixInProjectDelegate(VisualStudioWorkspaceImpl workspace, IVsHierarchy projectHierarchyOpt)
        {
            if (projectHierarchyOpt == null)
            {
                return p => true;
            }
            else
            {
                var projectIdsForHierarchy = workspace.ProjectTracker.Projects
                    .Where(p => p.Language == LanguageNames.CSharp || p.Language == LanguageNames.VisualBasic)
                    .Where(p => p.Hierarchy == projectHierarchyOpt)
                    .Select(p => workspace.CurrentSolution.GetProject(p.Id).Id)
                    .ToImmutableHashSet();
                return p => projectIdsForHierarchy.Contains(p.Id);
            }
        }

        public void RemoveSuppressions(bool selectedErrorListEntriesOnly, IVsHierarchy projectHierarchyOpt)
        {
            if (_tableControl == null)
            {
                return;
            }

            // TODO
        }

        private void ApplySuppressionFix(Func<Project, bool> shouldFixInProject, bool selectedEntriesOnly, bool isAddSuppression, bool isSuppressionInSource, bool onlyCompilerDiagnostics, bool showPreviewChangesDialog)
        {
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFixMap = null;

            // Get the diagnostics to fix from the suppression state service.
            var result = _waitIndicator.Wait(
                ServicesVSResources.SuppressMultipleOccurrences,
                ServicesVSResources.ComputingSuppressionFix,
                allowCancel: true,
                action: waitContext =>
                {
                    try
                    {
                        var diagnosticsToFix = _suppressionStateService.GetItems(
                                selectedEntriesOnly,
                                isAddSuppression,
                                isSuppressionInSource,
                                onlyCompilerDiagnostics,
                                waitContext.CancellationToken);

                        if (diagnosticsToFix.IsEmpty)
                        {
                            return;
                        }

                        waitContext.CancellationToken.ThrowIfCancellationRequested();
                        diagnosticsToFixMap = GetDiagnosticsToFixMapAsync(diagnosticsToFix, shouldFixInProject, waitContext.CancellationToken).WaitAndGetResult(waitContext.CancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });

            // Bail out if the user cancelled.
            if (result == WaitIndicatorResult.Canceled ||
                diagnosticsToFixMap == null || diagnosticsToFixMap.IsEmpty)
            {
                return;
            }

            var equivalenceKey = isSuppressionInSource ? FeaturesResources.SuppressWithPragma : FeaturesResources.SuppressWithGlobalSuppressMessage;

            // We have different suppression fixers for every language.
            // So we need to group diagnostics by the containing project language and apply fixes separately.
            var groups = diagnosticsToFixMap.GroupBy(entry => entry.Key.Project.Language);
            var hasMultipleLangauges = groups.Count() > 1;
            var currentSolution = _workspace.CurrentSolution;
            var newSolution = currentSolution;
            var needsMappingToNewSolution = false;

            foreach (var group in groups)
            {
                var language = group.Key;
                var waitDialogAndPreviewChangesTitle = hasMultipleLangauges ? string.Format(ServicesVSResources.SuppressMultipleOccurrencesForLanguage, language) : ServicesVSResources.SuppressMultipleOccurrences;
                var waitDialogMessage = hasMultipleLangauges ? string.Format(ServicesVSResources.ComputingSuppressionFixForLanguage, language) : ServicesVSResources.ComputingSuppressionFix;

                ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentDiagnosticsPerLanguage = null;
                CodeFixProvider suppressionFixer = null;

                if (needsMappingToNewSolution)
                {
                    // A fix was applied to a project group targeting a different language in a prior iteration.
                    // We need to remap our document entries to the new solution.
                    var builder = ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();
                    foreach (var kvp in group)
                    {
                        var document = newSolution.GetDocument(kvp.Key.Id);
                        if (document != null)
                        {
                            builder.Add(document, kvp.Value);
                        }
                    }

                    documentDiagnosticsPerLanguage = builder.ToImmutable();
                }
                else
                {
                    documentDiagnosticsPerLanguage = group.ToImmutableDictionary();
                }

                var allDiagnosticsBuilder = ImmutableArray.CreateBuilder<Diagnostic>();
                foreach (var documentDiagnostics in documentDiagnosticsPerLanguage)
                {
                    allDiagnosticsBuilder.AddRange(documentDiagnostics.Value);
                }

                // Fetch the suppression fixer to apply the fix.
                suppressionFixer = _codeFixService.GetSuppressionFixer(language, allDiagnosticsBuilder.ToImmutable());
                if (suppressionFixer == null)
                {
                    continue;
                }

                // Use the Fix multiple occurrences service to compute a bulk suppression fix for the specified diagnostics,
                // show a preview changes dialog and then apply the fix to the workspace.
                _fixMultipleOccurencesService.ComputeAndApplyFix(
                    documentDiagnosticsPerLanguage,
                    _workspace,
                    suppressionFixer,
                    suppressionFixer.GetFixAllProvider(),
                    equivalenceKey,
                    waitDialogAndPreviewChangesTitle,
                    waitDialogMessage,
                    showPreviewChangesDialog,
                    CancellationToken.None);
                
                newSolution = _workspace.CurrentSolution;
                if (currentSolution == newSolution)
                {
                    // User cancelled, so we just bail out.
                    break;
                }

                needsMappingToNewSolution = true;
            }
        }

        private async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDiagnosticsToFixMapAsync(IEnumerable<DiagnosticData> diagnosticsToFix, Func<Project, bool> shouldFixInProject, CancellationToken cancellationToken)
        {
            var builder = ImmutableDictionary.CreateBuilder<DocumentId, List<DiagnosticData>>();
            foreach (var diagnosticData in diagnosticsToFix)
            {
                List<DiagnosticData> diagnosticsPerDocument;
                if (!builder.TryGetValue(diagnosticData.DocumentId, out diagnosticsPerDocument))
                {
                    diagnosticsPerDocument = new List<DiagnosticData>();
                    builder[diagnosticData.DocumentId] = diagnosticsPerDocument;
                }

                diagnosticsPerDocument.Add(diagnosticData);
            }

            if (builder.Count == 0)
            {
                return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
            }

            var finalBuilder = ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();
            var latestDocumentDiagnosticsMap = new Dictionary<DocumentId, ImmutableHashSet<DiagnosticData>>();
            foreach (var group in builder.GroupBy(kvp => kvp.Key.ProjectId).AsImmutable())
            {
                var projectId = group.Key;
                var project = _workspace.CurrentSolution.GetProject(projectId);
                if (project == null || !shouldFixInProject(project))
                {
                    continue;
                }

                var uniqueDiagnosticIds = group.SelectMany(kvp => kvp.Value.Select(d => d.Id)).ToImmutableHashSet();
                var latestProjectDiagnostics = (await _diagnosticService.GetDiagnosticsForIdsAsync(project.Solution, project.Id, diagnosticIds: uniqueDiagnosticIds, includeSuppressedDiagnostics: true, cancellationToken: cancellationToken)
                    .ConfigureAwait(false)).Where(d => d.DocumentId != null);

                latestDocumentDiagnosticsMap.Clear();
                foreach (var kvp in latestProjectDiagnostics.GroupBy(d => d.DocumentId))
                {
                    latestDocumentDiagnosticsMap.Add(kvp.Key, kvp.ToImmutableHashSet());
                }

                var documentsToTreeMap = await GetDocumentIdsToTreeMapAsync(project, cancellationToken).ConfigureAwait(false);
                foreach (var documentDiagnostics in group)
                {
                    var document = project.GetDocument(documentDiagnostics.Key);
                    if (document == null)
                    {
                        continue;
                    }

                    ImmutableHashSet<DiagnosticData> latestDocumentDiagnostics;
                    if (!latestDocumentDiagnosticsMap.TryGetValue(document.Id, out latestDocumentDiagnostics))
                    {
                        // Ignore stale diagnostics in error list.
                        continue;
                    }

                    // Filter out stale diagnostics in error list.
                    var documentDiagnosticsToFix = documentDiagnostics.Value.Where(d => latestDocumentDiagnostics.Contains(d));

                    if (documentDiagnosticsToFix.IsEmpty())
                    {
                        continue;
                    }

                    var diagnostics = await DiagnosticData.ToDiagnosticsAsync(project, documentDiagnosticsToFix, cancellationToken).ConfigureAwait(false);
                    finalBuilder.Add(document, diagnostics.ToImmutableArray());
                }
            }

            return finalBuilder.ToImmutableDictionary();
        }

        private static async Task<ImmutableDictionary<DocumentId, SyntaxTree>> GetDocumentIdsToTreeMapAsync(Project project, CancellationToken cancellationToken)
        {
            var builder = ImmutableDictionary.CreateBuilder<DocumentId, SyntaxTree>();
            foreach (var document in project.Documents)
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                builder.Add(document.Id, tree);
            }

            return builder.ToImmutable();
        }
    }
}
