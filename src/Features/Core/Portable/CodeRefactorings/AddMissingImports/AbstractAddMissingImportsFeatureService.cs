// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal abstract class AbstractAddMissingImportsFeatureService : IAddMissingImportsFeatureService
    {
        protected abstract ImmutableArray<string> FixableDiagnosticIds { get; }

        protected abstract ImmutableArray<AbstractFormattingRule> GetFormatRules(SourceText text);

        /// <inheritdoc/>
        public async Task<Document> AddMissingImportsAsync(Document document, TextSpan textSpan, AddMissingImportsOptions options, CancellationToken cancellationToken)
        {
            var analysisResult = await AnalyzeAsync(document, textSpan, options, cancellationToken).ConfigureAwait(false);
            return await AddMissingImportsAsync(document, analysisResult, options.CleanupOptions.FormattingOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<Document> AddMissingImportsAsync(Document document, AddMissingImportsAnalysisResult analysisResult, SyntaxFormattingOptions formattingOptions, CancellationToken cancellationToken)
        {
            if (analysisResult.CanAddMissingImports)
            {
                // Apply those fixes to the document.
                var newDocument = await ApplyFixesAsync(document, analysisResult.AddImportFixData, formattingOptions, cancellationToken).ConfigureAwait(false);
                return newDocument;
            }

            return document;
        }

        /// <inheritdoc/>
        public async Task<AddMissingImportsAnalysisResult> AnalyzeAsync(Document document, TextSpan textSpan, AddMissingImportsOptions options, CancellationToken cancellationToken)
        {
            // Get the diagnostics that indicate a missing import.
            var addImportFeatureService = document.GetRequiredLanguageService<IAddImportFeatureService>();

            var solution = document.Project.Solution;
            var symbolSearchService = solution.Services.GetRequiredService<ISymbolSearchService>();

            // Since we are not currently considering NuGet packages, pass an empty array
            var packageSources = ImmutableArray<PackageSource>.Empty;

            var addImportOptions = new AddImportOptions(
                SearchOptions: new() { SearchReferenceAssemblies = true, SearchNuGetPackages = false },
                CleanupOptions: options.CleanupOptions,
                HideAdvancedMembers: options.HideAdvancedMembers);

            var unambiguousFixes = await addImportFeatureService.GetUniqueFixesAsync(
                document, textSpan, FixableDiagnosticIds, symbolSearchService,
                addImportOptions, packageSources, cancellationToken).ConfigureAwait(false);

            // We do not want to add project or framework references without the user's input, so filter those out.
            var usableFixes = unambiguousFixes.WhereAsArray(fixData => DoesNotAddReference(fixData, document.Project.Id));

            return new AddMissingImportsAnalysisResult(usableFixes);
        }

        private static bool DoesNotAddReference(AddImportFixData fixData, ProjectId currentProjectId)
        {
            return (fixData.ProjectReferenceToAdd is null || fixData.ProjectReferenceToAdd == currentProjectId)
                && (fixData.PortableExecutableReferenceProjectId is null || fixData.PortableExecutableReferenceProjectId == currentProjectId)
                && string.IsNullOrEmpty(fixData.AssemblyReferenceAssemblyName);
        }

        private async Task<Document> ApplyFixesAsync(Document document, ImmutableArray<AddImportFixData> fixes, SyntaxFormattingOptions formattingOptions, CancellationToken cancellationToken)
        {
            if (fixes.IsEmpty)
            {
                return document;
            }

            var solution = document.Project.Solution;
            var progressTracker = new ProgressTracker();
            var textDiffingService = solution.Services.GetRequiredService<IDocumentTextDifferencingService>();
            var packageInstallerService = solution.Services.GetService<IPackageInstallerService>();
            var addImportService = document.GetRequiredLanguageService<IAddImportFeatureService>();

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

            // Capture each location where we are inserting imports as well as the total
            // length of the text we are inserting so that we can format the span afterwards.
            var insertSpans = allTextChanges
                .GroupBy(change => change.Span)
                .Select(changes => new TextSpan(changes.Key.Start, changes.Sum(change => change.NewText!.Length)));

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.WithChanges(orderedTextInserts);
            var newDocument = newProject.GetRequiredDocument(document.Id).WithText(newText);

            // When imports are added to a code file that has no previous imports, extra
            // newlines are generated between each import because the fix is expecting to
            // separate the imports from the rest of the code file. We need to format the
            // imports to remove these extra newlines.
            return await CleanUpNewLinesAsync(newDocument, insertSpans, formattingOptions, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Document> CleanUpNewLinesAsync(Document document, IEnumerable<TextSpan> insertSpans, SyntaxFormattingOptions formattingOptions, CancellationToken cancellationToken)
        {
            var newDocument = document;

            // Since imports can be added at both the CompilationUnit and the Namespace level,
            // format each span individually so that we can retain each newline that was intended
            // to separate the import section from the other content.
            foreach (var insertSpan in insertSpans)
            {
                newDocument = await CleanUpNewLinesAsync(newDocument, insertSpan, formattingOptions, cancellationToken).ConfigureAwait(false);
            }

            return newDocument;
        }

        private async Task<Document> CleanUpNewLinesAsync(Document document, TextSpan insertSpan, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var services = document.Project.Solution.Services;

            var textChanges = Formatter.GetFormattedTextChanges(
                root,
                new[] { insertSpan },
                services,
                options: options,
                rules: GetFormatRules(text),
                cancellationToken);

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

        private static async Task<(ProjectChanges, IEnumerable<TextChange>)> GetChangesForCodeActionAsync(
            Document document,
            CodeAction codeAction,
            ProgressTracker progressTracker,
            IDocumentTextDifferencingService textDiffingService,
            CancellationToken cancellationToken)
        {
            // CodeAction.GetChangedSolutionAsync is only implemented for code actions that can fully compute the new	            
            // solution without deferred computation or taking a dependency on the main thread. In other cases, the	                
            // implementation of GetChangedSolutionAsync will throw an exception and the code action application is	            
            // expected to apply the changes by executing the operations in GetOperationsAsync (which may have other	
            // side effects). This code cannot assume the input CodeAction supports GetChangedSolutionAsync, so it first	
            // attempts to apply text changes obtained from GetOperationsAsync. Two forms are supported:	
            //	
            // 1. GetOperationsAsync returns an empty list of operations (i.e. no changes are required)	
            // 2. GetOperationsAsync returns a list of operations, where the first change is an ApplyChangesOperation to	
            //    change the text in the solution, and any remaining changes are deferred computation changes.	
            //	
            // If GetOperationsAsync does not adhere to one of these patterns, the code falls back to calling	
            // GetChangedSolutionAsync since there is no clear way to apply the changes otherwise.	
            var operations = await codeAction.GetOperationsAsync(
                document.Project.Solution, progressTracker, cancellationToken).ConfigureAwait(false);
            Solution newSolution;
            if (operations.Length == 0)
            {
                newSolution = document.Project.Solution;
            }
            else if (operations is [ApplyChangesOperation applyChangesOperation])
            {
                newSolution = applyChangesOperation.ChangedSolution;
            }
            else
            {
                newSolution = await codeAction.GetRequiredChangedSolutionAsync(progressTracker, cancellationToken).ConfigureAwait(false);
            }

            var newDocument = newSolution.GetRequiredDocument(document.Id);

            // Use Line differencing to reduce the possibility of changes that overwrite existing code.
            var textChanges = await textDiffingService.GetTextChangesAsync(
                document, newDocument, TextDifferenceTypes.Line, cancellationToken).ConfigureAwait(false);
            var projectChanges = newDocument.Project.GetChanges(document.Project);

            return (projectChanges, textChanges);
        }

        protected sealed class CleanUpNewLinesFormatter : AbstractFormattingRule
        {
            private readonly SourceText _text;

            public CleanUpNewLinesFormatter(SourceText text)
                => _text = text;

            public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
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
