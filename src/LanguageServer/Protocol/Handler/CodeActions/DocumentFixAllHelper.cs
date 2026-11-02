// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;

internal static class DocumentFixAllHelper
{
    public static async Task<Solution> GetChangedSolutionAsync(
        Document document, ICodeFixService codeFixService, DiagnosticSeverity minimumSeverity,
        ResourceOperationKind[] resourceOperations, Action<string> log, CancellationToken cancellationToken)
    {
        var originalSolution = document.Project.Solution;
        var solution = originalSolution;
        var services = solution.Services;
        var diagnosticService = services.GetRequiredService<IDiagnosticAnalyzerService>();
        var fixAllService = services.GetRequiredService<IFixAllGetFixesService>();
        var extensionManager = services.GetRequiredService<IExtensionManager>();

        // Snapshot eligible diagnostic IDs across the whole document in a stable order.
        var diagnostics = await diagnosticService.GetDiagnosticsForSpanAsync(
            document, range: null, DiagnosticIdFilter.All, priority: null, DiagnosticKind.All, cancellationToken).ConfigureAwait(false);
        var diagnosticIds = diagnostics
            .Where(d => !d.IsSuppressed && d.Severity >= minimumSeverity)
            .Select(d => d.Id).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToImmutableArray();

        // Different keys may be competing ways to fix one diagnostic. Recompute the same ID after each accepted fix
        // instead of applying all original actions. IDs introduced by those edits are left for a later invocation.
        foreach (var diagnosticId in diagnosticIds)
        {
            int? previousDiagnosticCount = null;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // The initial snapshot keeps only IDs. Earlier fixes can move or remove diagnostics,
                // so request them for the current document rather than reusing the original locations.
                var collection = await codeFixService.GetDocumentFixAllForIdInSpanAsync(
                    document, textSpan: null, diagnosticId, minimumSeverity,
                    includeInteractive: false, cancellationToken).ConfigureAwait(false);
                if (collection?.FixAllState is not { } state || collection.Provider is not CodeFixProvider)
                    break;

                // Changing text alone need not fix anything. A decreasing diagnostic count bounds retries,
                // including providers that continually introduce different text without making progress.
                if (previousDiagnosticCount is { } count && collection.Diagnostics.Length >= count)
                {
                    log($"Stopping document Fix All for '{diagnosticId}': no diagnostic progress.");
                    break;
                }

                previousDiagnosticCount = collection.Diagnostics.Length;

                // Every retained fix has an eligible leaf; use the first fix's leaf equivalence key, not its container's.
                var action = CodeFixService.GetFirstNonInteractiveAction(collection.Fixes[0].Action);
                if (action is null)
                    break;

                state = state.With(codeActionEquivalenceKey: action.EquivalenceKey);

                // Compute operations through normal extension exception handling, without changing the live workspace.
                var operations = await extensionManager.PerformFunctionAsync(
                    state.Provider,
                    ct => fixAllService.GetFixAllOperationsAsync(
                        new FixAllContext(state, CodeAnalysisProgress.None, ct), showPreviewChangesDialog: false),
                    defaultValue: [],
                    cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (operations.IsEmpty)
                    break;

                // Only a single ApplyChangesOperation can be converted into the workspace edit returned by this action.
                if (operations.Length != 1 || operations[0] is not ApplyChangesOperation applyChanges)
                {
                    log($"Skipping document Fix All for '{diagnosticId}': unsupported code action operations.");
                    break;
                }

                var candidate = applyChanges.ChangedSolution;
                var normalized = await NormalizeLinkedDocumentChangesAsync(solution, candidate, cancellationToken).ConfigureAwait(false);
                if (normalized is null)
                {
                    log($"Skipping document Fix All for '{diagnosticId}': conflicting linked document changes.");
                    break;
                }

                // Validate the complete edit after propagation, rejecting only this candidate while retaining earlier fixes.
                if (!CodeActionResolveHelper.CanApplyFixAllChanges(originalSolution, normalized, resourceOperations))
                {
                    log($"Skipping document Fix All for '{diagnosticId}': unsupported accumulated workspace changes.");
                    break;
                }

                solution = normalized;
                if (solution.GetDocument(document.Id) is not { } changedDocument)
                    return solution;

                document = changedDocument;
            }
        }

        return solution;
    }

    private static async Task<Solution?> NormalizeLinkedDocumentChangesAsync(
        Solution solution, Solution changedSolution, CancellationToken cancellationToken)
    {
        // The normal linked merger merges differing source text (including conflict comments) and skips additions
        // and non-source documents. Here we must reject differing text and update every context before the next fix.
        using var _1 = PooledDictionary<DocumentUri, SourceText>.GetInstance(out var textsByUri);
        using var _2 = PooledDictionary<DocumentId, SourceText>.GetInstance(out var textsById);
        foreach (var projectChange in changedSolution.GetChanges(solution).GetProjectChanges())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var changedIds = projectChange.GetChangedDocuments().Concat(projectChange.GetAddedDocuments())
                .Concat(projectChange.GetChangedAdditionalDocuments()).Concat(projectChange.GetAddedAdditionalDocuments())
                .Concat(projectChange.GetChangedAnalyzerConfigDocuments()).Concat(projectChange.GetAddedAnalyzerConfigDocuments());
            foreach (var id in changedIds)
            {
                var updated = changedSolution.GetRequiredTextDocument(id);
                var text = await updated.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                if (solution.GetTextDocument(id) is { } original &&
                    text.ContentEquals(await original.GetValueTextAsync(cancellationToken).ConfigureAwait(false)))
                {
                    continue;
                }

                // The final validator rejects unusable output paths; avoid constructing their URIs during normalization.
                if (updated.FilePath is null &&
                    (updated.Project.FilePath is not { } projectPath || !PathUtilities.IsAbsolute(projectPath)))
                {
                    continue;
                }

                var filePath = updated.FilePath ?? updated.GetFilePathForDocumentWithoutFilePath();
                if (!PathUtilities.IsAbsolute(filePath))
                    continue;

                // Different text for linked contexts cannot be represented by one physical-file edit.
                var uri = ProtocolConversions.CreateAbsoluteDocumentUri(filePath);
                if (textsByUri.TryGetValue(uri, out var previousText) && !previousText.ContentEquals(text))
                    return null;

                textsByUri[uri] = text;
                foreach (var linkedId in changedSolution.GetDocumentIdsWithFilePath(filePath).Add(id))
                {
                    if (textsById.TryGetValue(linkedId, out previousText) && !previousText.ContentEquals(text))
                        return null;

                    textsById[linkedId] = text;
                }
            }
        }

        // Keep every physical file's contexts consistent before computing diagnostics for the next ID.
        foreach (var pair in textsById)
            changedSolution = changedSolution.WithTextDocumentText(pair.Key, pair.Value);

        return changedSolution;
    }
}
