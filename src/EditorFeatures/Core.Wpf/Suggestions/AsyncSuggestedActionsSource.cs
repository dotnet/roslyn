// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnifiedSuggestions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionsSourceProvider
    {
        private partial class AsyncSuggestedActionsSource : SuggestedActionsSource, ISuggestedActionsSourceExperimental
        {
            public AsyncSuggestedActionsSource(
                IThreadingContext threadingContext,
                IGlobalOptionService globalOptions,
                SuggestedActionsSourceProvider owner,
                ITextView textView,
                ITextBuffer textBuffer,
                ISuggestedActionCategoryRegistryService suggestedActionCategoryRegistry)
                : base(threadingContext, globalOptions, owner, textView, textBuffer, suggestedActionCategoryRegistry)
            {
            }

            public async IAsyncEnumerable<SuggestedActionSet> GetSuggestedActionsAsync(
                ISuggestedActionCategorySet requestedActionCategories,
                SnapshotSpan range,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                AssertIsForeground();

                using var state = SourceState.TryAddReference();
                if (state is null)
                    yield break;

                var workspace = state.Target.Workspace;
                if (workspace is null)
                    yield break;

                var selection = TryGetCodeRefactoringSelection(state, range);
                await workspace.Services.GetRequiredService<IWorkspaceStatusService>().WaitUntilFullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                using (Logger.LogBlock(FunctionId.SuggestedActions_GetSuggestedActionsAsync, cancellationToken))
                {
                    var document = range.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document is null)
                        yield break;

                    // Compute and return the high pri set of fixes and refactorings first so the user
                    // can act on them immediately without waiting on the regular set.
                    //
                    // Don't include suppression/config fixes in the high pri set.  We don't want them showing
                    // up above any fixes/refactorings in the normal pri set.
                    var highPriSet = GetCodeFixesAndRefactoringsAsync(
                        state, requestedActionCategories, document, range, selection, _ => null,
                        includeSuppressionFixes: false, CodeActionRequestPriority.High, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false);
                    await foreach (var set in highPriSet)
                        yield return set;

                    var lowPriSet = GetCodeFixesAndRefactoringsAsync(
                        state, requestedActionCategories, document, range, selection, _ => null,
                        includeSuppressionFixes: true, CodeActionRequestPriority.Normal, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false);
                    await foreach (var set in lowPriSet)
                        yield return set;
                }
            }

            private async IAsyncEnumerable<SuggestedActionSet> GetCodeFixesAndRefactoringsAsync(
                ReferenceCountedDisposable<State> state,
                ISuggestedActionCategorySet requestedActionCategories,
                Document document,
                SnapshotSpan range,
                TextSpan? selection,
                Func<string, IDisposable?> addOperationScope,
                bool includeSuppressionFixes,
                CodeActionRequestPriority priority,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var workspace = document.Project.Solution.Workspace;
                var supportsFeatureService = workspace.Services.GetRequiredService<ITextBufferSupportsFeatureService>();

                var fixesTask = GetCodeFixesAsync(
                    state, supportsFeatureService, requestedActionCategories, workspace, document, range,
                    addOperationScope, includeSuppressionFixes, priority, isBlocking: false, cancellationToken);
                var refactoringsTask = GetRefactoringsAsync(
                    state, supportsFeatureService, requestedActionCategories, GlobalOptions, workspace, document, selection,
                    addOperationScope, priority, isBlocking: false, cancellationToken);

                if (priority == CodeActionRequestPriority.High)
                {
                    // in a high pri scenario, return data as soon as possible so that the user can interact with them.
                    // this is especially important for state-machine oriented refactorings (like rename) where the user
                    // should always have access to them effectively synchronously.
                    var firstTask = await Task.WhenAny(fixesTask, refactoringsTask).ConfigureAwait(false);
                    var secondTask = firstTask == fixesTask ? refactoringsTask : fixesTask;

                    var orderedTasks = new[] { firstTask, secondTask };
                    foreach (var task in orderedTasks)
                    {
                        if (task == fixesTask)
                        {
                            var fixes = await fixesTask.ConfigureAwait(false);
                            foreach (var set in ConvertToSuggestedActionSets(state, selection, fixes, ImmutableArray<UnifiedSuggestedActionSet>.Empty))
                                yield return set;
                        }
                        else
                        {
                            Contract.ThrowIfFalse(task == refactoringsTask);

                            var refactorings = await refactoringsTask.ConfigureAwait(false);
                            foreach (var set in ConvertToSuggestedActionSets(state, selection, ImmutableArray<UnifiedSuggestedActionSet>.Empty, refactorings))
                                yield return set;
                        }
                    }
                }
                else
                {
                    var actionsArray = await Task.WhenAll(fixesTask, refactoringsTask).ConfigureAwait(false);
                    foreach (var set in ConvertToSuggestedActionSets(state, selection, fixes: actionsArray[0], refactorings: actionsArray[1]))
                        yield return set;
                }
            }
        }
    }
}
