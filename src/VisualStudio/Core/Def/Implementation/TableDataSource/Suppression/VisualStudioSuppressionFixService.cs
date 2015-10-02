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
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.CodeActions;

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
        private readonly ICodeActionEditHandlerService _editHandlerService;
        private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public VisualStudioSuppressionFixService(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl workspace,
            IDiagnosticAnalyzerService diagnosticService,
            ICodeFixService codeFixService,
            ICodeActionEditHandlerService editHandlerService,
            IVisualStudioDiagnosticListSuppressionStateService suppressionStateService,
            IWaitIndicator waitIndicator)
        {
            _workspace = workspace;
            _diagnosticService = diagnosticService;
            _codeFixService = codeFixService;
            _suppressionStateService = (VisualStudioDiagnosticListSuppressionStateService)suppressionStateService;
            _editHandlerService = editHandlerService;
            _waitIndicator = waitIndicator;
            _fixMultipleOccurencesService = workspace.Services.GetService<IFixMultipleOccurrencesService>();

            var errorList = serviceProvider.GetService(typeof(SVsErrorList)) as IErrorList;
            _tableControl = errorList?.TableControl;
        }

        public bool AddSuppressions(IVsHierarchy projectHierarchyOpt)
        {
            if (_tableControl == null)
            {
                return false;
            }

            Func<Project, bool> shouldFixInProject = GetShouldFixInProjectDelegate(_workspace, projectHierarchyOpt);

            // Apply suppressions fix in global suppressions file for non-compiler diagnostics and
            // in source only for compiler diagnostics.
            ApplySuppressionFix(shouldFixInProject, selectedEntriesOnly: false, isAddSuppression: true, isSuppressionInSource: false, onlyCompilerDiagnostics: false, showPreviewChangesDialog: false);
            ApplySuppressionFix(shouldFixInProject, selectedEntriesOnly: false, isAddSuppression: true, isSuppressionInSource: true, onlyCompilerDiagnostics: true, showPreviewChangesDialog: false);

            return true;
        }

        public bool AddSuppressions(bool selectedErrorListEntriesOnly, bool suppressInSource, IVsHierarchy projectHierarchyOpt)
        {
            if (_tableControl == null)
            {
                return false;
            }

            Func<Project, bool> shouldFixInProject = GetShouldFixInProjectDelegate(_workspace, projectHierarchyOpt);
            return ApplySuppressionFix(shouldFixInProject, selectedErrorListEntriesOnly, isAddSuppression: true, isSuppressionInSource: suppressInSource, onlyCompilerDiagnostics: false, showPreviewChangesDialog: true);
        }

        public bool RemoveSuppressions(bool selectedErrorListEntriesOnly, IVsHierarchy projectHierarchyOpt)
        {
            if (_tableControl == null)
            {
                return false;
            }

            Func<Project, bool> shouldFixInProject = GetShouldFixInProjectDelegate(_workspace, projectHierarchyOpt);
            return ApplySuppressionFix(shouldFixInProject, selectedErrorListEntriesOnly, isAddSuppression: false, isSuppressionInSource: false, onlyCompilerDiagnostics: false, showPreviewChangesDialog: true);
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

        private bool ApplySuppressionFix(Func<Project, bool> shouldFixInProject, bool selectedEntriesOnly, bool isAddSuppression, bool isSuppressionInSource, bool onlyCompilerDiagnostics, bool showPreviewChangesDialog)
        {
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentDiagnosticsToFixMap = null;
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> projectDiagnosticsToFixMap = null;

            var title = isAddSuppression ? ServicesVSResources.SuppressMultipleOccurrences : ServicesVSResources.RemoveSuppressMultipleOccurrences;
            var waitDialogMessage = isAddSuppression ? ServicesVSResources.ComputingSuppressionFix : ServicesVSResources.ComputingRemoveSuppressionFix;

            // Get the diagnostics to fix from the suppression state service.
            Action<CancellationToken> computeDiagnosticsToFix = cancellationToken =>
            {
                var diagnosticsToFix = _suppressionStateService.GetItemsAsync(
                                selectedEntriesOnly,
                                isAddSuppression,
                                isSuppressionInSource,
                                onlyCompilerDiagnostics,
                                cancellationToken)
                            .WaitAndGetResult(cancellationToken);

                if (diagnosticsToFix.IsEmpty)
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();
                documentDiagnosticsToFixMap = GetDocumentDiagnosticsToFixAsync(diagnosticsToFix, shouldFixInProject, cancellationToken).WaitAndGetResult(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                projectDiagnosticsToFixMap = isSuppressionInSource ?
                    ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty :
                    GetProjectDiagnosticsToFixAsync(diagnosticsToFix, shouldFixInProject, cancellationToken).WaitAndGetResult(cancellationToken);
            };

            var result = InvokeWithWaitDialog(computeDiagnosticsToFix, title, waitDialogMessage);

            // Bail out if the user cancelled.
            if (result == WaitIndicatorResult.Canceled ||
                documentDiagnosticsToFixMap == null ||
                projectDiagnosticsToFixMap == null)
            {
                return false;
            }

            if (documentDiagnosticsToFixMap.IsEmpty && projectDiagnosticsToFixMap.IsEmpty)
            {
                // Nothing to fix.
                return true;
            }

            // Equivalence key determines what fix will be applied.
            // Make sure we don't include any specific diagnostic ID, as we want all of the given diagnostics (which can have varied ID) to be fixed.
            var equivalenceKey = isAddSuppression ?
                (isSuppressionInSource ? FeaturesResources.SuppressWithPragma : FeaturesResources.SuppressWithGlobalSuppressMessage) :
                FeaturesResources.RemoveSuppressionEquivalenceKeyPrefix;

            // We have different suppression fixers for every language.
            // So we need to group diagnostics by the containing project language and apply fixes separately.
            var languages = new HashSet<string>(projectDiagnosticsToFixMap.Keys.Select(p => p.Language).Concat(documentDiagnosticsToFixMap.Select(kvp => kvp.Key.Project.Language)));
            var newSolution = _workspace.CurrentSolution;

            foreach (var language in languages)
            {
                // Use the Fix multiple occurrences service to compute a bulk suppression fix for the specified document and project diagnostics,
                // show a preview changes dialog and then apply the fix to the workspace.

                var documentDiagnosticsPerLanguage = GetDocumentDiagnosticsMappedToNewSolution(documentDiagnosticsToFixMap, newSolution, language);
                if (!documentDiagnosticsPerLanguage.IsEmpty)
                {
                    var suppressionFixer = GetSuppressionFixer(documentDiagnosticsPerLanguage.SelectMany(kvp => kvp.Value), language, _codeFixService);
                    if (suppressionFixer != null)
                    {
                        var suppressionFixAllProvider = suppressionFixer.GetFixAllProvider();
                        newSolution = _fixMultipleOccurencesService.GetFix(
                            documentDiagnosticsPerLanguage,
                            _workspace,
                            suppressionFixer,
                            suppressionFixAllProvider,
                            equivalenceKey,
                            title,
                            waitDialogMessage,
                            cancellationToken: CancellationToken.None);
                        if (newSolution == null)
                        {
                            // User cancelled or fixer threw an exception, so we just bail out.
                            return false;
                        }
                    }
                }

                var projectDiagnosticsPerLanguage = GetProjectDiagnosticsMappedToNewSolution(projectDiagnosticsToFixMap, newSolution, language);
                if (!projectDiagnosticsPerLanguage.IsEmpty)
                {
                    var suppressionFixer = GetSuppressionFixer(projectDiagnosticsPerLanguage.SelectMany(kvp => kvp.Value), language, _codeFixService);
                    if (suppressionFixer != null)
                    {
                        var suppressionFixAllProvider = suppressionFixer.GetFixAllProvider();
                        newSolution = _fixMultipleOccurencesService.GetFix(
                             projectDiagnosticsPerLanguage,
                             _workspace,
                             suppressionFixer,
                             suppressionFixAllProvider,
                             equivalenceKey,
                             title,
                             waitDialogMessage,
                             CancellationToken.None);
                        if (newSolution == null)
                        {
                            // User cancelled or fixer threw an exception, so we just bail out.
                            return false;
                        }
                    }
                }
            }

            if (showPreviewChangesDialog)
            {
                newSolution = FixAllGetFixesService.PreviewChanges(
                    _workspace.CurrentSolution,
                    newSolution,
                    fixAllPreviewChangesTitle: title,
                    fixAllTopLevelHeader: title,
                    languageOpt: languages.Count == 1 ? languages.Single() : null,
                    workspace: _workspace);
                if (newSolution == null)
                {
                    return false;
                }
            }

            waitDialogMessage = isAddSuppression ? ServicesVSResources.ApplyingSuppressionFix : ServicesVSResources.ApplyingRemoveSuppressionFix;
            Action<CancellationToken> applyFix = cancellationToken =>
            {
                var operations = SpecializedCollections.SingletonEnumerable<CodeActionOperation>(new ApplyChangesOperation(newSolution));
                _editHandlerService.Apply(
                    _workspace,
                    fromDocument: null,
                    operations: operations,
                    title: title,
                    cancellationToken: cancellationToken);
            };

            result = InvokeWithWaitDialog(applyFix, title, waitDialogMessage);
            return result == WaitIndicatorResult.Completed;
        }

        private WaitIndicatorResult InvokeWithWaitDialog(Action<CancellationToken> action, string waitDialogTitle, string waitDialogMessage)
        {
            var cancelled = false;
            var result = _waitIndicator.Wait(
                waitDialogTitle,
                waitDialogMessage,
                allowCancel: true,
                action: waitContext =>
                {
                    try
                    {
                        action(waitContext.CancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                    }
                });

            return cancelled ? WaitIndicatorResult.Canceled : result;
        }

        private static ImmutableDictionary<Document, ImmutableArray<Diagnostic>> GetDocumentDiagnosticsMappedToNewSolution(ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentDiagnosticsToFixMap, Solution newSolution, string language)
        {
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Builder builder = null;
            foreach (var kvp in documentDiagnosticsToFixMap)
            {
                if (kvp.Key.Project.Language != language)
                {
                    continue;
                }

                var document = newSolution.GetDocument(kvp.Key.Id);
                if (document != null)
                {
                    builder = builder ?? ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();
                    builder.Add(document, kvp.Value);
                }
            }

            return builder != null ? builder.ToImmutable() : ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
        }

        private static ImmutableDictionary<Project, ImmutableArray<Diagnostic>> GetProjectDiagnosticsMappedToNewSolution(ImmutableDictionary<Project, ImmutableArray<Diagnostic>> projectDiagnosticsToFixMap, Solution newSolution, string language)
        {
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Builder projectDiagsBuilder = null;
            foreach (var kvp in projectDiagnosticsToFixMap)
            {
                if (kvp.Key.Language != language)
                {
                    continue;
                }

                var project = newSolution.GetProject(kvp.Key.Id);
                if (project != null)
                {
                    projectDiagsBuilder = projectDiagsBuilder ?? ImmutableDictionary.CreateBuilder<Project, ImmutableArray<Diagnostic>>();
                    projectDiagsBuilder.Add(project, kvp.Value);
                }
            }

            return projectDiagsBuilder != null ? projectDiagsBuilder.ToImmutable() : ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
        }

        private static CodeFixProvider GetSuppressionFixer(IEnumerable<Diagnostic> diagnostics, string language, ICodeFixService codeFixService)
        {
            var allDiagnosticsBuilder = ImmutableArray.CreateBuilder<Diagnostic>();
            foreach (var documentDiagnostics in diagnostics)
            {
                allDiagnosticsBuilder.AddRange(diagnostics);
            }

            // Fetch the suppression fixer to apply the fix.
            return codeFixService.GetSuppressionFixer(language, allDiagnosticsBuilder.ToImmutable());
        }

        private async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(IEnumerable<DiagnosticData> diagnosticsToFix, Func<Project, bool> shouldFixInProject, CancellationToken cancellationToken)
        {
            Func<DiagnosticData, bool> isDocumentDiagnostic = d => d.DataLocation != null && d.HasTextSpan;

            var builder = ImmutableDictionary.CreateBuilder<DocumentId, List<DiagnosticData>>();
            foreach (var diagnosticData in diagnosticsToFix.Where(isDocumentDiagnostic))
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
            foreach (var group in builder.GroupBy(kvp => kvp.Key.ProjectId))
            {
                var projectId = group.Key;
                var project = _workspace.CurrentSolution.GetProject(projectId);
                if (project == null || !shouldFixInProject(project))
                {
                    continue;
                }

                var uniqueDiagnosticIds = group.SelectMany(kvp => kvp.Value.Select(d => d.Id)).ToImmutableHashSet();
                var latestProjectDiagnostics = (await _diagnosticService.GetDiagnosticsForIdsAsync(project.Solution, project.Id, diagnosticIds: uniqueDiagnosticIds, includeSuppressedDiagnostics: true, cancellationToken: cancellationToken)
                    .ConfigureAwait(false)).Where(isDocumentDiagnostic);

                latestDocumentDiagnosticsMap.Clear();
                foreach (var kvp in latestProjectDiagnostics.Where(d => d.DocumentId != null).GroupBy(d => d.DocumentId))
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
                        latestDocumentDiagnostics = ImmutableHashSet<DiagnosticData>.Empty;
                    }

                    // Filter out stale diagnostics in error list.
                    var documentDiagnosticsToFix = documentDiagnostics.Value.Where(d => latestDocumentDiagnostics.Contains(d) || _suppressionStateService.IsSynthesizedNonRoslynDiagnostic(d));

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

        private async Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync(IEnumerable<DiagnosticData> diagnosticsToFix, Func<Project, bool> shouldFixInProject, CancellationToken cancellationToken)
        {
            Func<DiagnosticData, bool> isProjectDiagnostic = d => d.DataLocation == null && d.ProjectId != null;
            var builder = ImmutableDictionary.CreateBuilder<ProjectId, List<DiagnosticData>>();
            foreach (var diagnosticData in diagnosticsToFix.Where(isProjectDiagnostic))
            {
                List<DiagnosticData> diagnosticsPerProject;
                if (!builder.TryGetValue(diagnosticData.ProjectId, out diagnosticsPerProject))
                {
                    diagnosticsPerProject = new List<DiagnosticData>();
                    builder[diagnosticData.ProjectId] = diagnosticsPerProject;
                }

                diagnosticsPerProject.Add(diagnosticData);
            }

            if (builder.Count == 0)
            {
                return ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
            }

            var finalBuilder = ImmutableDictionary.CreateBuilder<Project, ImmutableArray<Diagnostic>>();
            var latestDiagnosticsToFix = new HashSet<DiagnosticData>();
            foreach (var kvp in builder)
            {
                var projectId = kvp.Key;
                var project = _workspace.CurrentSolution.GetProject(projectId);
                if (project == null || !shouldFixInProject(project))
                {
                    continue;
                }

                var diagnostics = kvp.Value;
                var uniqueDiagnosticIds = diagnostics.Select(d => d.Id).ToImmutableHashSet();
                var latestDiagnosticsFromDiagnosticService = (await _diagnosticService.GetDiagnosticsForIdsAsync(project.Solution, project.Id, diagnosticIds: uniqueDiagnosticIds, includeSuppressedDiagnostics: true, cancellationToken: cancellationToken)
                    .ConfigureAwait(false));

                latestDiagnosticsToFix.Clear();
                latestDiagnosticsToFix.AddRange(latestDiagnosticsFromDiagnosticService.Where(isProjectDiagnostic));

                // Filter out stale diagnostics in error list.
                var projectDiagnosticsToFix = diagnostics.Where(d => latestDiagnosticsFromDiagnosticService.Contains(d) || _suppressionStateService.IsSynthesizedNonRoslynDiagnostic(d));
                if (projectDiagnosticsToFix.IsEmpty())
                {
                    continue;
                }

                var projectDiagnostics = await DiagnosticData.ToDiagnosticsAsync(project, projectDiagnosticsToFix, cancellationToken).ConfigureAwait(false);
                finalBuilder.Add(project, projectDiagnostics.ToImmutableArray());
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
