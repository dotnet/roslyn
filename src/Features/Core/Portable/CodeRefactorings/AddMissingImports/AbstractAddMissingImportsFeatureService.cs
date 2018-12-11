// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal abstract class AbstractAddMissingImportsFeatureService : IAddMissingImportsFeatureService
    {
        protected abstract ImmutableArray<string> FixableDiagnosticIds { get; }

        public async Task<bool> HasMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            // Get the diagnostics that indicate a missing import.
            var diagnostics = await GetDiagnosticsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (diagnostics.IsEmpty)
            {
                return false;
            }

            // Find fixes for the diagnostic where there is only a single fix.
            var usableFixes = await GetUnambiguousFixesAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);
            return !usableFixes.IsEmpty;
        }

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
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            return semanticModel.GetDiagnostics(textSpan, cancellationToken)
                .Where(diagnostic => FixableDiagnosticIds.Contains(diagnostic.Id))
                .ToImmutableArray();
        }

        private async Task<ImmutableArray<AddImportFixData>> GetUnambiguousFixesAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var symbolSearchService = solution.Workspace.Services.GetService<ISymbolSearchService>();
            // Since we are not currently considering NuGet packages, pass an empty array
            var packageSources = ImmutableArray<PackageSource>.Empty;
            var addImportService = document.GetLanguageService<IAddImportFeatureService>();

            // We only need to recieve 2 results back per diagnostic to determine that the fix is ambiguous.
            var getFixesForDiagnosticsTasks = diagnostics
                .GroupBy(diagnostic => diagnostic.Location.SourceSpan)
                .Select(diagnosticsForSourceSpan => addImportService
                    .GetFixesForDiagnosticsAsync(document, diagnosticsForSourceSpan.Key, diagnosticsForSourceSpan.AsImmutable(),
                        maxResultsPerDiagnostic: 2, symbolSearchService, searchReferenceAssemblies: true, packageSources, cancellationToken));

            var fixes = ArrayBuilder<AddImportFixData>.GetInstance();
            foreach (var getFixesForDiagnosticsTask in getFixesForDiagnosticsTasks)
            {
                var fixesForDiagnostics = await getFixesForDiagnosticsTask.ConfigureAwait(false);

                foreach (var fixesForDiagnostic in fixesForDiagnostics)
                {
                    // When there is more than one potential fix for a missing import diagnostic,
                    // which is possible when the same class name is present in mutliple namespaces,
                    // we do not want to choose for the user and be wrong. We will not attempt to
                    // fix this diagnostic and instead leave it for the user to resolve since they
                    // will have more context for determining the proper fix.
                    if (fixesForDiagnostic.Fixes.Length == 1)
                    {
                        fixes.Add(fixesForDiagnostic.Fixes[0]);
                    }
                }
            }

            return fixes.ToImmutableAndFree();
        }

        private async Task<Document> ApplyFixesAsync(Document document, ImmutableArray<AddImportFixData> fixes, CancellationToken cancellationToken)
        {
            if (fixes.IsEmpty)
            {
                return document;
            }

            var solution = document.Project.Solution;
            var progressTracker = new ProgressTracker();
            var textDiffingService = solution.Workspace.Services.GetService<IDocumentTextDifferencingService>();
            var packageInstallerService = solution.Workspace.Services.GetService<IPackageInstallerService>();
            var addImportService = document.GetLanguageService<IAddImportFeatureService>();

            // Do not limit the results since we plan to fix all the reported issues.
            var codeActions = addImportService.GetCodeActionsForFixes(document, fixes, packageInstallerService, maxResults: int.MaxValue);
            var getChangesTasks = codeActions.Select(
                action => GetChangesForCodeActionAsync(document, action, progressTracker, textDiffingService, cancellationToken));

            // Using Sets allows us to accumulate only the distinct changes.
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
            var newProject = document.Project;
            newProject = newProject.AddMetadataReferences(allAddedMetaDataReferences);
            newProject = newProject.AddProjectReferences(allAddedProjectReferences);

            // Only consider insertion changes to reduce the chance of producing a
            // badly merged final document. Alphabetize the new imports, this will not
            // change the insertion point but will give a more correct result. The user
            // may still need to use organize imports afterwards.
            var orderedTextInserts = allTextChanges.Where(change => change.Span.IsEmpty)
                .OrderBy(change => change.NewText);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.WithChanges(orderedTextInserts);
            var newDocument = newProject.GetDocument(document.Id).WithText(newText);

            return newDocument;
        }

        private async Task<(ProjectChanges, IEnumerable<TextChange>)> GetChangesForCodeActionAsync(
            Document document,
            CodeAction codeAction,
            ProgressTracker progressTracker,
            IDocumentTextDifferencingService textDiffingService,
            CancellationToken cancellationToken)
        {
            var newSolution = await codeAction.GetChangedSolutionAsync(
                progressTracker, cancellationToken: cancellationToken).ConfigureAwait(false);
            var newDocument = newSolution.GetDocument(document.Id);

            // Use Line differencing to reduce the possibility of changes that overwrite existing code.
            var textChanges = await textDiffingService.GetTextChangesAsync(
                document, newDocument, TextDifferenceTypes.Line, cancellationToken).ConfigureAwait(false);
            var projectChanges = newDocument.Project.GetChanges(document.Project);

            return (projectChanges, textChanges);
        }
    }
}
