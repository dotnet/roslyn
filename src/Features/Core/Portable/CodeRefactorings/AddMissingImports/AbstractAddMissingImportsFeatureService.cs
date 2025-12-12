// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.AddMissingImports;

internal abstract class AbstractAddMissingImportsFeatureService : IAddMissingImportsFeatureService
{
    protected abstract ImmutableArray<string> FixableDiagnosticIds { get; }

    protected abstract ImmutableArray<AbstractFormattingRule> GetFormatRules(SourceText text);

    public async Task<ImmutableArray<AddImportFixData>> AnalyzeAsync(
        Document document, TextSpan textSpan, bool cleanDocument, CancellationToken cancellationToken)
    {
        // Get the diagnostics that indicate a missing import.
        var addImportFeatureService = document.GetRequiredLanguageService<IAddImportFeatureService>();

        var solution = document.Project.Solution;
        var symbolSearchService = solution.Services.GetRequiredService<ISymbolSearchService>();

        // Since we are not currently considering NuGet packages, pass an empty array
        var packageSources = ImmutableArray<PackageSource>.Empty;

        // Only search for symbols within the current project.  We don't want to add any sort of reference/package to
        // something outside of the starting project.
        var addImportOptions = await document.GetAddImportOptionsAsync(
            searchOptions: new()
            {
                SearchUnreferencedProjectSourceSymbols = false,
                SearchUnreferencedMetadataSymbols = false,
                SearchReferenceAssemblies = false,
                SearchNuGetPackages = false,
            },
            cleanDocument,
            cancellationToken).ConfigureAwait(false);

        var unambiguousFixes = await addImportFeatureService.GetUniqueFixesAsync(
            document, textSpan, FixableDiagnosticIds, symbolSearchService,
            addImportOptions, packageSources, cancellationToken).ConfigureAwait(false);

        Debug.Assert(unambiguousFixes.All(d => d.Kind == AddImportFixKind.ProjectSymbol));

        // We do not want to add project or framework references without the user's input, so filter those out.
        var usableFixes = unambiguousFixes.WhereAsArray(fixData => fixData.Kind == AddImportFixKind.ProjectSymbol);

        return usableFixes;
    }

    public async Task<Document> AddMissingImportsAsync(
        Document document,
        ImmutableArray<AddImportFixData> fixes,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        if (fixes.IsEmpty)
            return document;

        var solution = document.Project.Solution;
        var packageInstallerService = solution.Services.GetService<IPackageInstallerService>();

        var addImportService = document.GetRequiredLanguageService<IAddImportFeatureService>();
        var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();

        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var organizeImportsOptions = await document.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(false);

        // Do not limit the results since we plan to fix all the reported issues.
        var codeActions = addImportService.GetCodeActionsForFixes(
            document, fixes, packageInstallerService, maxResults: int.MaxValue);

        // Using Sets allows us to accumulate only the distinct changes. Only consider insertion changes to reduce the
        // chance of producing a badly merged final document.
        using var _ = PooledHashSet<TextChange>.GetInstance(out var insertionOnlyChanges);

        var changes = ProducerConsumer<TextChange>.RunParallelStreamAsync(
            codeActions,
            produceItems: static async (codeAction, callback, args, cancellationToken) =>
            {
                var (document, progressTracker) = args;
                await GetInsertionOnlyChangesForCodeActionAsync(
                    document, codeAction, progressTracker, callback, cancellationToken).ConfigureAwait(false);
            },
            args: (document, progressTracker),
            cancellationToken);

        await foreach (var change in changes.ConfigureAwait(false))
            insertionOnlyChanges.Add(change);

        // Capture each location where we are inserting imports as well as the total
        // length of the text we are inserting so that we can format the span afterwards.
        var insertSpans = insertionOnlyChanges
            .GroupBy(change => change.Span)
            .Select(changes => new TextSpan(changes.Key.Start, changes.Sum(change => change.NewText!.Length)));

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var newText = text.WithChanges(insertionOnlyChanges);
        var newDocument = document.WithText(newText);

        // When imports are added to a code file that has no previous imports, extra newlines are generated between each
        // import because the fix is expecting to separate the imports from the rest of the code file. We need to format
        // the imports to remove these extra newlines.
        var cleanedDocument = await CleanUpNewLinesAsync(
            newDocument, insertSpans, formattingOptions, cancellationToken).ConfigureAwait(false);

        // Finally, organize the imports to ensure they are in the correct order.  Normally, the underling add-import
        // service will already ensure this.  However, this takes care of the case where we want to insert two or more
        // usings into the same location in an existing using-list.  In that case, there are many possible outcomes we 
        // could get depending on what order we processed the fixes in.  This ensures that no matter what order we do 
        // things in, the final result is organized properly.
        var organizedDocument = await organizeImportsService.OrganizeImportsAsync(
            cleanedDocument, organizeImportsOptions, cancellationToken).ConfigureAwait(false);

        return organizedDocument;
    }

    private async Task<Document> CleanUpNewLinesAsync(Document document, IEnumerable<TextSpan> insertSpans, SyntaxFormattingOptions formattingOptions, CancellationToken cancellationToken)
    {
        var newDocument = document;

        // Since imports can be added at both the CompilationUnit and the Namespace level,
        // format each span individually so that we can retain each newline that was intended
        // to separate the import section from the other content.
        foreach (var insertSpan in insertSpans)
            newDocument = await CleanUpNewLinesAsync(newDocument, insertSpan, formattingOptions, cancellationToken).ConfigureAwait(false);

        return newDocument;
    }

    private async Task<Document> CleanUpNewLinesAsync(Document document, TextSpan insertSpan, SyntaxFormattingOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var services = document.Project.Solution.Services;

        var textChanges = Formatter.GetFormattedTextChanges(
            root,
            [insertSpan],
            services,
            options: options,
            rules: GetFormatRules(text),
            cancellationToken);

        // If there are no changes then, do less work.
        if (textChanges.Count == 0)
            return document;

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

    private static async ValueTask GetInsertionOnlyChangesForCodeActionAsync(
        Document document,
        CodeAction codeAction,
        IProgress<CodeAnalysisProgress> progressTracker,
        Action<TextChange> callback,
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
        var textDiffingService = document.Project.Solution.Services.GetRequiredService<IDocumentTextDifferencingService>();
        var textChanges = await textDiffingService.GetTextChangesAsync(
            document, newDocument, TextDifferenceTypes.Line, cancellationToken).ConfigureAwait(false);

        foreach (var change in textChanges)
        {
            if (change.Span.IsEmpty)
                callback(change);
        }
    }

    protected sealed class CleanUpNewLinesFormatter(SourceText text) : AbstractFormattingRule
    {
        private readonly SourceText _text = text;

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
