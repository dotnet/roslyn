// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Helper class for shared code between <see cref="FixAllCodeFixGetFixesService"/>
    /// and <see cref="FixAllCodeRefactoringGetFixesService"/>.
    /// </summary>
    internal static class FixAllGetFixesServiceHelper
    {
        public static async Task<ImmutableArray<CodeActionOperation>> GetFixAllOperationsAsync(
            CodeAction codeAction,
            Project project,
            int fixAllCorrelationId,
            FunctionId fixAllOccurrencesPreviewChangesFunctionId,
            bool showPreviewChangesDialog,
            CancellationToken cancellationToken)
        {
            // We have computed the fix all occurrences code fix.
            // Now fetch the new solution with applied fix and bring up the Preview changes dialog.

            var workspace = project.Solution.Workspace;

            cancellationToken.ThrowIfCancellationRequested();
            var operations = await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            if (operations == null)
            {
                return ImmutableArray<CodeActionOperation>.Empty;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var newSolution = await codeAction.GetChangedSolutionInternalAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (newSolution == null)
            {
                return ImmutableArray<CodeActionOperation>.Empty;
            }

            if (showPreviewChangesDialog)
            {
                newSolution = PreviewChanges(
                    project.Solution,
                    newSolution,
                    FeaturesResources.Fix_all_occurrences,
                    codeAction.Title,
                    project.Language,
                    workspace,
                    fixAllOccurrencesPreviewChangesFunctionId,
                    fixAllCorrelationId,
                    cancellationToken);
                if (newSolution == null)
                {
                    return ImmutableArray<CodeActionOperation>.Empty;
                }
            }

            // Get a code action, with apply changes operation replaced with the newSolution.
            return GetNewFixAllOperations(operations, newSolution, cancellationToken);
        }

        internal static Solution? PreviewChanges(
            Solution currentSolution,
            Solution newSolution,
            string fixAllPreviewChangesTitle,
            string fixAllTopLevelHeader,
            string? language,
            Workspace workspace,
            FunctionId fixAllOccurrencesPreviewChangesFunctionId,
            int? correlationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (Logger.LogBlock(
                fixAllOccurrencesPreviewChangesFunctionId,
                KeyValueLogMessage.Create(LogType.UserAction, m =>
                {
                    // only set when correlation id is given
                    // we might not have this info for suppression
                    if (correlationId.HasValue)
                    {
                        m[FixAllLogger.CorrelationId] = correlationId;
                    }
                }),
                cancellationToken))
            {
                var glyph = language == null
                    ? Glyph.Assembly
                    : language == LanguageNames.CSharp
                        ? Glyph.CSharpProject
                        : Glyph.BasicProject;
#if COCOA

                var previewService = workspace.Services.GetService<IPreviewDialogService>();

                // Until IPreviewDialogService is implemented, just execute all changes without user ability to pick and choose
                if (previewService == null)
                    return newSolution;
#else

                var previewService = workspace.Services.GetRequiredService<IPreviewDialogService>();

#endif

                var changedSolution = previewService.PreviewChanges(
                    string.Format(EditorFeaturesResources.Preview_Changes_0, fixAllPreviewChangesTitle),
                    "vs.codefix.fixall",
                    fixAllTopLevelHeader,
                    fixAllPreviewChangesTitle,
                    glyph,
                    newSolution,
                    currentSolution);

                if (changedSolution == null)
                {
                    // User clicked cancel.
                    FixAllLogger.LogPreviewChangesResult(correlationId, applied: false);
                    return null;
                }

                FixAllLogger.LogPreviewChangesResult(correlationId, applied: true, allChangesApplied: changedSolution == newSolution);
                return changedSolution;
            }
        }

        private static ImmutableArray<CodeActionOperation> GetNewFixAllOperations(ImmutableArray<CodeActionOperation> operations, Solution newSolution, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<CodeActionOperation>.GetInstance();
            var foundApplyChanges = false;
            foreach (var operation in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!foundApplyChanges)
                {
                    if (operation is ApplyChangesOperation)
                    {
                        foundApplyChanges = true;
                        result.Add(new ApplyChangesOperation(newSolution));
                        continue;
                    }
                }

                result.Add(operation);
            }

            return result.ToImmutableAndFree();
        }
    }
}
