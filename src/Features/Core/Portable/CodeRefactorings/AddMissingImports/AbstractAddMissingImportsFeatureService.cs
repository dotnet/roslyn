// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal abstract class AbstractAddMissingImportsFeatureService : IAddMissingImportsFeatureService
    {
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        public AbstractAddMissingImportsFeatureService(IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
        }

        protected abstract ImmutableArray<string> FixableDiagnosticIds { get; }

        public async Task<Project> AddMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            // Get the diagnostics that indicate a missing import.
            var diagnostics = await GetDiagnosticsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (diagnostics.IsEmpty)
            {
                return document.Project;
            }

            // Find fixes for the diagnostic where there is only a single fix.
            var usableFixes = await GetUnambiguousFixesAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);
            if (usableFixes.IsEmpty)
            {
                return document.Project;
            }

            // Apply those fixes to the document.
            var newDocument = await ApplyFixesAsync(document, usableFixes, cancellationToken).ConfigureAwait(false);
            return newDocument.Project;
        }

        private async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var project = document.Project;

            var diagnosticData = await _diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(document, textSpan, cancellationToken: cancellationToken).ConfigureAwait(false);
            var getDiagnosticTasks = diagnosticData
                .Where(datum => FixableDiagnosticIds.Contains(datum.Id))
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

        private async Task<ImmutableArray<AddImportFixData>> GetUnambiguousFixesAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var symbolSearchService = solution.Workspace.Services.GetService<ISymbolSearchService>();
            // Since we are not currently considering NuGet packages, pass an empty array
            var packageSources = ImmutableArray<PackageSource>.Empty;
            var addImportService = document.GetLanguageService<IAddImportFeatureService>();

            var getFixesForSpanTasks = diagnostics
                .GroupBy(diagnostic => diagnostic.Location.SourceSpan)
                .Select(diagnosticsForSourceSpan => addImportService
                    .GetFixesForDiagnosticsAsync(document, diagnosticsForSourceSpan.Key, diagnosticsForSourceSpan.AsImmutable(), symbolSearchService, searchReferenceAssemblies: true, packageSources, cancellationToken));

            await Task.WhenAll(getFixesForSpanTasks).ConfigureAwait(false);

            var fixes = ArrayBuilder<AddImportFixData>.GetInstance();
            foreach (var getFixesForSpanTask in getFixesForSpanTasks)
            {
                var fixesForSpan = await getFixesForSpanTask.ConfigureAwait(false);

                foreach (var fixForSpan in fixesForSpan)
                {
                    // If there is more than one fix, then we will leave it for the user to manually apply.
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

            var solution = document.Project.Solution;
            var packageInstallerService = solution.Workspace.Services.GetService<IPackageInstallerService>();
            var addImportService = document.GetLanguageService<IAddImportFeatureService>();

            // Do not limit the results since we plan to fix all the reported issues.
            var codeActions = addImportService.GetCodeActionsForFixes(document, fixes, packageInstallerService, limitResults: false);
            var getChangesTasks = codeActions.Select(action => GetChangesForCodeActionAsync(document, action, cancellationToken));

            await Task.WhenAll(getChangesTasks).ConfigureAwait(false);

            // Using Sets allows us to accumulate only the distict changes.
            var allTextChanges = new HashSet<TextChange>();
            // Some fixes require adding missing references.
            var allAddedProjectReferences = new HashSet<ProjectReference>();
            var allAddedMetaDataReferences = new HashSet<MetadataReference>();

            foreach (var getChangesTask in getChangesTasks)
            {
                var (projectChanges, textChanges) = await getChangesTask.ConfigureAwait(false);

                allTextChanges.UnionWith(textChanges);
                allAddedProjectReferences.UnionWith(projectChanges.GetAddedProjectReferences());
                allAddedMetaDataReferences.UnionWith(projectChanges.GetAddedMetadataReferences());
            }

            // Apply changes to both the project and document.
            var newProject = document.Project.AddMetadataReferences(allAddedMetaDataReferences);
            newProject = newProject.AddProjectReferences(allAddedProjectReferences);

            var newText = text.WithChanges(allTextChanges);
            var newDocument = newProject.GetDocument(document.Id).WithText(newText);

            return newDocument;
        }

        private async Task<(ProjectChanges projectChanges, IEnumerable<TextChange> textChanges)> GetChangesForCodeActionAsync(Document document, CodeAction codeAction, CancellationToken cancellationToken)
        {
            var newSolution = await codeAction.GetChangedSolutionInternalAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var newDocument = newSolution.GetDocument(document.Id);

            var textChanges = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var projectChanges = newDocument.Project.GetChanges(document.Project);

            return (projectChanges, textChanges);
        }
    }
}
