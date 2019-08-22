// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
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
            var unambiguousFixes = await GetUnambiguousFixesAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);

            // We do not want to add project or framework references without the user's input, so filter those out.
            var usableFixes = unambiguousFixes.WhereAsArray(fixData => DoesNotAddReference(fixData, document.Project.Id));
            if (usableFixes.IsEmpty)
            {
                return document.Project;
            }

            // Apply those fixes to the document.
            var newDocument = await ApplyFixesAsync(document, usableFixes, cancellationToken).ConfigureAwait(false);
            return newDocument.Project;
        }

        private bool DoesNotAddReference(AddImportFixData fixData, ProjectId currentProjectId)
        {
            return (fixData.ProjectReferenceToAdd is null || fixData.ProjectReferenceToAdd == currentProjectId)
                && (fixData.PortableExecutableReferenceProjectId is null || fixData.PortableExecutableReferenceProjectId == currentProjectId)
                && string.IsNullOrEmpty(fixData.AssemblyReferenceAssemblyName);
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


            // The text changes produced by the AddImport code fixes with be inserts except in
            // the case where there were multiple trailing newlines.

            // Insertion case (zero or one trailing newline following the using directives):
            // using System;  // Added using directives will all be insertion changes on the following line
            //
            // namespace Foo
            // {

            // Replacement case (two or more trailing newlines following the using directives):
            // using System; // Added using directives will all be replacement changes that overwrite the following line
            //
            //
            // namespace Foo
            // {

            // Covert text changes to be insertions and keep the new lines. This will keep them
            // from stepping on each other. Alphabetize the new imports, this will not change
            // the insertion point but will give a more correct result. The user may still need
            // to use organize imports afterwards.
            var orderedTextInserts = allTextChanges
                .Select(change => change.Span.IsEmpty ? change : MakeChangeAnInsertion(change))
                .OrderBy(change => change.NewText);

            // Capture each location where we are inserting imports as well as the total
            // length of the text we are inserting so that we can format the span afterwards.
            var insertSpans = orderedTextInserts
                .GroupBy(change => change.Span)
                .Select(changes => new TextSpan(changes.Key.Start, changes.Sum(change => change.NewText.Length)));

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.WithChanges(orderedTextInserts);
            var newDocument = newProject.GetDocument(document.Id).WithText(newText);

            // When imports are added to a code file that has no previous imports, extra
            // newlines are generated between each import because the fix is expecting to
            // separate the imports from the rest of the code file. We need to format the
            // imports to remove these extra newlines.
            return await CleanUpNewLinesAsync(newDocument, insertSpans, cancellationToken).ConfigureAwait(false);

            static TextChange MakeChangeAnInsertion(TextChange change)
                => new TextChange(new TextSpan(change.Span.Start, 0), change.NewText);
        }

        private async Task<Document> CleanUpNewLinesAsync(Document document, IEnumerable<TextSpan> insertSpans, CancellationToken cancellationToken)
        {
            var languageFormatter = document.GetLanguageService<ISyntaxFormattingService>();
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var newDocument = document;

            // Since imports can be added at both the CompilationUnit and the Namespace level,
            // format each span individually so that we can retain each newline that was intended
            // to separate the import section from the other content.
            foreach (var insertSpan in insertSpans)
            {
                newDocument = await CleanUpNewLinesAsync(newDocument, insertSpan, languageFormatter, options, cancellationToken).ConfigureAwait(false);
            }

            return newDocument;
        }

        private async Task<Document> CleanUpNewLinesAsync(Document document, TextSpan insertSpan, ISyntaxFormattingService languageFormatter, OptionSet options, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var textChanges = languageFormatter.Format(root, new[] { insertSpan }, options, new[] { new CleanUpNewLinesFormatter(text) }, cancellationToken).GetTextChanges();

            // If there are no changes then, do less work.
            if (textChanges.Count == 0)
            {
                return document;
            }

            // The last text change should include where the insert span ends
            Debug.Assert(textChanges.Last().Span.IntersectsWith(insertSpan.End));

            // If there are changes then, this was a case where there were no
            // previous imports statements. We need to retain the final extra
            // newline because that separates the imports section from the rest
            // of the code.
            textChanges.RemoveAt(textChanges.Count - 1);

            var newText = text.WithChanges(textChanges);
            return document.WithText(newText);
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

        private sealed class CleanUpNewLinesFormatter : AbstractFormattingRule
        {
            private readonly SourceText _text;

            public CleanUpNewLinesFormatter(SourceText text)
            {
                _text = text;
            }

            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
            {
                // Since we know the general shape of these new import statements, we simply look for where
                // tokens are not on the same line and force them to only be separated by a single newline.

                _text.GetLineAndOffset(previousToken.Span.Start, out var previousLine, out _);
                _text.GetLineAndOffset(currentToken.Span.Start, out var currentLine, out _);

                if (previousLine != currentLine)
                {
                    return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);
                }

                return null;
            }
        }
    }
}
