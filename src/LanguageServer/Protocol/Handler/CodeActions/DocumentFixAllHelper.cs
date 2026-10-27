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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;

internal static class DocumentFixAllHelper
{
    public static async Task<Document> GetChangedDocumentAsync(
        Document document, ICodeFixService codeFixService, DiagnosticSeverity minimumSeverity,
        Action<string> log, CancellationToken cancellationToken)
    {
        var services = document.Project.Solution.Services;
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

            // Only a single ApplyChangesOperation can be converted into the document edit returned by this action.
            if (operations.Length != 1 || operations[0] is not ApplyChangesOperation applyChanges)
            {
                log($"Skipping document Fix All for '{diagnosticId}': unsupported code action operations.");
                continue;
            }

            // Discard the whole candidate if it changes other files or project/solution state.
            var changedDocument = await GetDocumentOnlyChangeAsync(
                document, applyChanges.ChangedSolution, cancellationToken).ConfigureAwait(false);
            if (changedDocument is null)
            {
                log($"Skipping document Fix All for '{diagnosticId}': changes are not confined to the requested document.");
                continue;
            }

            document = changedDocument;
        }

        return document;
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

    private static async Task<Document?> GetDocumentOnlyChangeAsync(
        Document document, Solution changedSolution, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var changes = changedSolution.GetChanges(solution);

        // Reject solution-wide changes and explicit edits to generated documents.
        if (changedSolution.Id != solution.Id ||
            changedSolution.FilePath != solution.FilePath ||
            !changedSolution.ProjectIds.SequenceEqual(solution.ProjectIds) ||
            !changedSolution.AnalyzerReferences.SequenceEqual(solution.AnalyzerReferences) ||
            changes.GetExplicitlyChangedSourceGeneratedDocuments().Any())
        {
            return null;
        }

        // Linked contexts share a physical file, so their text edits must agree.
        var allowedIds = document.GetLinkedDocumentIds().Add(document.Id);
        SourceText? changedText = null;
        foreach (var projectChange in changes.GetProjectChanges())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var oldProject = projectChange.OldProject;
            var newProject = projectChange.NewProject;

            // Preserve project configuration, source document membership, and additional/analyzer-config documents.
            if (oldProject.State.Attributes != newProject.State.Attributes ||
                !Equals(oldProject.CompilationOptions, newProject.CompilationOptions) ||
                !Equals(oldProject.ParseOptions, newProject.ParseOptions) ||
                !oldProject.ProjectReferences.SequenceEqual(newProject.ProjectReferences) ||
                !oldProject.MetadataReferences.SequenceEqual(newProject.MetadataReferences) ||
                !oldProject.AnalyzerReferences.SequenceEqual(newProject.AnalyzerReferences) ||
                !oldProject.DocumentIds.SequenceEqual(newProject.DocumentIds) ||
                projectChange.GetAddedAdditionalDocuments().Any() ||
                projectChange.GetRemovedAdditionalDocuments().Any() ||
                projectChange.GetChangedAdditionalDocuments().Any() ||
                projectChange.GetAddedAnalyzerConfigDocuments().Any() ||
                projectChange.GetRemovedAnalyzerConfigDocuments().Any() ||
                projectChange.GetChangedAnalyzerConfigDocuments().Any())
            {
                return null;
            }

            // Validate each changed source document's identity and metadata before accepting its text.
            foreach (var id in projectChange.GetChangedDocuments())
            {
                if (!allowedIds.Contains(id))
                    return null;

                var updated = newProject.GetRequiredDocument(id);
                if (updated.HasInfoChanged(oldProject.GetRequiredDocument(id)))
                    return null;

                // Different text for linked contexts cannot be represented by one physical-file edit.
                var text = await updated.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                if (changedText is not null && !changedText.ContentEquals(text))
                    return null;

                changedText = text;
            }
        }

        if (changedText is null)
            return document;

        // Keep linked contexts consistent for subsequent diagnostics, with one physical-file edit at resolve.
        foreach (var id in allowedIds)
            solution = solution.WithDocumentText(id, changedText);

        return solution.GetRequiredDocument(document.Id);
    }
}
