// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        VSCommanding.CommandState IChainedCommandHandler<CommitUniqueCompletionListItemCommandArgs>.GetCommandState(CommitUniqueCompletionListItemCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void IChainedCommandHandler<CommitUniqueCompletionListItemCommandArgs>.ExecuteCommand(
            CommitUniqueCompletionListItemCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();

            if (sessionOpt == null)
            {
                // User hit ctrl-space.  If there was no completion up then we want to trigger
                // completion. 
                var completionService = this.GetCompletionService();
                if (completionService == null)
                {
                    return;
                }

                var trigger = new CompletionTrigger(CompletionTriggerKind.InvokeAndCommitIfUnique);
                if (!StartNewModelComputation(completionService, trigger))
                {
                    return;
                }
            }

            if (sessionOpt.InitialUnfilteredModel == null && !ShouldBlockForCompletionItems())
            {
                // We're in a language that doesn't want to block, but hasn't computed the initial
                // set of completion items.  In this case, we asynchronously wait for the items to
                // be computed.  And if nothing has happened between now and that point, we proceed
                // with committing the items.
                CommitUniqueCompletionListItemAsynchronously();
                return;
            }

            // We're either in a language that is ok with blocking, or we have the initial set
            // of items.  Wait until we're done filtering them, then get the selected item.  If 
            // it's unique, then we want to commit it.
            var model = WaitForModel();
            if (model == null)
            {
                // Computation failed.  Just pass this command on.
                nextHandler();
                return;
            }

            CommitIfUnique(model);
        }

        private void CommitUniqueCompletionListItemAsynchronously()
        {
            var currentSession = sessionOpt;
            var currentTask = currentSession.Computation.ModelTask;

            // We're kicking off async work.  Track this with an async token for test purposes.
            var token = ((IController<Model>)this).BeginAsyncOperation(nameof(CommitUniqueCompletionListItemAsynchronously));

            var task = currentTask.ContinueWith(async t =>
            {
                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true);

                if (this.sessionOpt == currentSession &&
                    this.sessionOpt.Computation.ModelTask == currentTask)
                {
                    // Nothing happened between when we were invoked and now.
                    CommitIfUnique(t.Result);
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();

            task.CompletesAsyncOperation(token);
        }

        private void CommitIfUnique(Model model)
        {
            this.AssertIsForeground();

            if (model == null)
            {
                return;
            }

            // Note: Dev10 behavior seems to be that if there is no unique item that filtering is
            // turned off.  However, i do not know if this is desirable behavior, or merely a bug
            // with how that convoluted code worked.  So I'm not maintaining that behavior here.  If
            // we do want it through, it would be easy to get again simply by asking the model
            // computation to remove all filtering.

            if (model.IsUnique && model.SelectedItemOpt != null)
            {
                // We had a unique item in the list.  Commit it and dismiss this session.
                this.CommitOnNonTypeChar(model.SelectedItemOpt, model);
            }
        }
    }
}
