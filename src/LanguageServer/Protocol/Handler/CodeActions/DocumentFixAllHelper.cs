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
using CodeAction = Microsoft.CodeAnalysis.CodeActions.CodeAction;

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

        // Each ID is visited once, but its locations and fixes must be recomputed after earlier edits.
        // Diagnostic IDs introduced by those edits are left for a later invocation.
        foreach (var diagnosticId in diagnosticIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var collection = await codeFixService.GetDocumentFixAllForIdInSpanAsync(
                document, textSpan: null, diagnosticId, minimumSeverity, cancellationToken).ConfigureAwait(false);
            if (collection?.FixAllState is not { } state || collection.Provider is not CodeFixProvider)
                continue;

            // No individual fix was selected by the user, so choose the first eligible action and reuse its equivalence key.
            var action = GetFirstAction(collection.Fixes.Select(fix => fix.Action));
            if (action is null)
                continue;

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
                continue;

            // Only a single ApplyChangesOperation can be converted into the workspace edit returned by this action.
            if (operations.Length != 1 || operations[0] is not ApplyChangesOperation applyChanges)
            {
                log($"Skipping document Fix All for '{diagnosticId}': unsupported code action operations.");
                continue;
            }

            // Reject the whole candidate if any required change cannot be represented by the client.
            var candidate = applyChanges.ChangedSolution;
            if (!CodeActionResolveHelper.CanApplyFixAllChanges(solution, candidate, resourceOperations))
            {
                log($"Skipping document Fix All for '{diagnosticId}': unsupported workspace changes.");
                continue;
            }

            var normalized = await NormalizeLinkedDocumentChangesAsync(solution, candidate, cancellationToken).ConfigureAwait(false);
            if (normalized is null)
            {
                log($"Skipping document Fix All for '{diagnosticId}': conflicting linked document changes.");
                continue;
            }

            if (!CodeActionResolveHelper.CanApplyFixAllChanges(originalSolution, normalized, resourceOperations))
            {
                log($"Skipping document Fix All for '{diagnosticId}': unsupported accumulated workspace changes.");
                continue;
            }

            solution = normalized;
            if (solution.GetDocument(document.Id) is not { } changedDocument)
                break;

            document = changedDocument;
        }

        return solution;
    }

    private static CodeAction? GetFirstAction(System.Collections.Generic.IEnumerable<CodeAction> actions)
    {
        // Preserve provider registration order, choosing the first eligible leaf in nested actions.
        foreach (var action in actions)
        {
            // Batch execution cannot prompt for options or include known non-document changes.
            if (action is CodeActionWithOptions || action.Tags.Contains(CodeAction.RequiresNonDocumentChange))
                continue;

            if (action.NestedActions.IsEmpty)
                return action;

            if (GetFirstAction(action.NestedActions) is { } nested)
                return nested;
        }

        return null;
    }

    private static async Task<Solution?> NormalizeLinkedDocumentChangesAsync(
        Solution solution, Solution changedSolution, CancellationToken cancellationToken)
    {
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

                // Different text for linked contexts cannot be represented by one physical-file edit.
                var uri = updated.FilePath is null ? updated.CreateUriForDocumentWithoutFilePath() : updated.GetURI();
                if (textsByUri.TryGetValue(uri, out var previousText) && !previousText.ContentEquals(text))
                    return null;

                textsByUri[uri] = text;
                foreach (var linkedId in changedSolution.GetDocumentIdsWithFilePath(uri.GetDocumentFilePathFromUri()).Add(id))
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
