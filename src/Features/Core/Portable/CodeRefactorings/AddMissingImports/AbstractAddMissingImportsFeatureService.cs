// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal abstract class AbstractAddMissingImportsFeatureService : IAddMissingImportsFeatureService
    {
        private IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        public AbstractAddMissingImportsFeatureService(IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
        }

        protected abstract ImmutableArray<string> FixableDiagnosticIds { get; }

        public async Task<Solution> AddMissingImportsAsync(Solution solution, CancellationToken cancellationToken)
        {
            var newSolution = solution;

            foreach (var projectId in solution.ProjectIds)
            {
                var project = newSolution.GetProject(projectId);
                var newProject = await AddMissingImportsAsync(project, cancellationToken).ConfigureAwait(false);
                newSolution = newProject.Solution;
            }

            return newSolution;
        }

        public async Task<Project> AddMissingImportsAsync(Project project, CancellationToken cancellationToken)
        {
            var newProject = project;

            foreach (var documentId in project.DocumentIds)
            {
                var document = newProject.GetDocument(documentId);
                newProject = await AddMissingImportsAsync(document, cancellationToken).ConfigureAwait(false);
            }

            return newProject;
        }

        public async Task<Project> AddMissingImportsAsync(Document document, CancellationToken cancellationToken)
        {
            var diagnostics = await GetDiagnosticsAsync(document, cancellationToken).ConfigureAwait(false);
            return await FixDocument(document, diagnostics, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Project> AddMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var diagnostics = await GetDiagnosticsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            return await FixDocument(document, diagnostics, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> IsMissingImportsAsync(Document document, CancellationToken cancellationToken)
        {
            var diagnostics = await GetDiagnosticsAsync(document, cancellationToken).ConfigureAwait(false);
            return !diagnostics.IsEmpty;
        }

        public async Task<bool> IsMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var diagnostics = await GetDiagnosticsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            return !diagnostics.IsEmpty;
        }

        private Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            if (!document.TryGetText(out var text))
            {
                return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
            }

            var textSpan = new TextSpan(0, text.Length);
            return GetDiagnosticsAsync(document, textSpan, cancellationToken);
        }

        private async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            if (!document.SupportsSemanticModel)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var project = document.Project;

            var diagnosticData = await _diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(document, textSpan, cancellationToken: cancellationToken).ConfigureAwait(false);
            var getDiagnosticTasks = diagnosticData
                .Where(d => FixableDiagnosticIds.Contains(d.Id))
                .Select(datum => datum.ToDiagnosticAsync(project, cancellationToken));

            await Task.WhenAll(getDiagnosticTasks).ConfigureAwait(false);

            var diagnostics = ArrayBuilder<Diagnostic>.GetInstance();
            foreach (var getDiagnosticTask in getDiagnosticTasks)
            {
                var diagnostic = await getDiagnosticTask.ConfigureAwait(false);
                diagnostics.Add(diagnostic);
            }

            return diagnostics.ToImmutableAndFree()
                .Sort((d1, d2) => d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start);
        }

        private async Task<Project> FixDocument(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            if (diagnostics.IsEmpty)
            {
                return document.Project;
            }

            var usableFixes = await GetUnambiguousFixesAsync(document, diagnostics, cancellationToken);
            if (usableFixes.IsEmpty)
            {
                return document.Project;
            }

            var newDocument = await ApplyFixesAsync(document, usableFixes, cancellationToken).ConfigureAwait(false);
            return newDocument.Project;
        }

        private async Task<ImmutableArray<AddImportFixData>> GetUnambiguousFixesAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            if (diagnostics.IsEmpty)
            {
                return ImmutableArray<AddImportFixData>.Empty;
            }

            var addImportService = document.GetLanguageService<IAddImportFeatureService>();

            var getFixesForSpanTasks = diagnostics
                .GroupBy(diagnostic => diagnostic.Location.SourceSpan)
                .Select(diagnosticsForSourceSpan => addImportService
                    .GetFixesForDiagnosticsAsync(document, diagnosticsForSourceSpan.Key, diagnosticsForSourceSpan.AsImmutable(), true, false, cancellationToken)
                );

            await Task.WhenAll(getFixesForSpanTasks).ConfigureAwait(false);

            var fixes = ArrayBuilder<AddImportFixData>.GetInstance();
            foreach (var getFixesForSpanTask in getFixesForSpanTasks)
            {
                var fixesForSpan = await getFixesForSpanTask.ConfigureAwait(false);

                foreach (var fixForSpan in fixesForSpan)
                {
                    // If there is more than one fix then we will leave it for the user to manually apply
                    if (fixForSpan.Fixes.Length == 1)
                    {
                        fixes.Add(fixForSpan.Fixes[0]);
                    }
                }
            }

            return fixes.ToImmutableAndFree();
        }

        private async Task<Document> ApplyFixesAsync(Document document, ImmutableArray<AddImportFixData> fixes, CancellationToken cancellationToken)
        {
            if (fixes.IsEmpty
                || !document.TryGetText(out var text))
            {
                return document;
            }

            var codeActions = GetCodeActionsForFixes(document, fixes);
            var getChangesTasks = codeActions.Select(action => GetChangesForCodeActionAsync(document, action, cancellationToken));

            await Task.WhenAll(getChangesTasks).ConfigureAwait(false);

            var allTextChanges = Enumerable.Empty<TextChange>();
            var allAddedProjectReferences = Enumerable.Empty<ProjectReference>();
            var allAddedMetaDataReferences = Enumerable.Empty<MetadataReference>();

            foreach (var getChangesTask in getChangesTasks)
            {
                var (projectChanges, textChanges) = await getChangesTask.ConfigureAwait(false);

                allTextChanges = allTextChanges.Concat(textChanges);
                allAddedProjectReferences = allAddedProjectReferences.Concat(projectChanges.GetAddedProjectReferences());
                allAddedMetaDataReferences = allAddedMetaDataReferences.Concat(projectChanges.GetAddedMetadataReferences());
            }

            var newProject = document.Project.AddMetadataReferences(allAddedMetaDataReferences.Distinct());
            newProject = newProject.AddProjectReferences(allAddedProjectReferences.Distinct());

            var newText = text.WithChanges(allTextChanges.Distinct());
            var newDocument = newProject.GetDocument(document.Id).WithText(newText);

            return newDocument;
        }

        private ImmutableArray<CodeAction> GetCodeActionsForFixes(Document document, ImmutableArray<AddImportFixData> fixes)
        {
            var addImportService = document.GetLanguageService<IAddImportFeatureService>();
            var codeActions = ArrayBuilder<CodeAction>.GetInstance();

            foreach (var fix in fixes)
            {
                var codeAction = addImportService.TryCreateCodeAction(document, fix);
                codeActions.AddIfNotNull(codeAction);
            }

            return codeActions.ToImmutableAndFree();
        }

        private async Task<(ProjectChanges projectChanges, IEnumerable<TextChange> textChanges)> GetChangesForCodeActionAsync(Document document, CodeAction codeAction, CancellationToken cancellationToken)
        {
            var newSolution = await codeAction.GetChangedSolutionAsync(new ProgressTracker(), cancellationToken).ConfigureAwait(false);
            var newDocument = newSolution.GetDocument(document.Id);

            var textChanges = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var projectChanges = newDocument.Project.GetChanges(document.Project);

            return (projectChanges, textChanges);
        }
    }
}
