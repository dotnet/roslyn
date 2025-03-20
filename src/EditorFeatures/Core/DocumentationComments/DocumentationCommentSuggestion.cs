// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Copilot;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal class DocumentationCommentSuggestion(CopilotGenerateDocumentationCommentProvider providerInstance,
        SuggestionManagerBase suggestionManager, VisualStudio.Threading.IAsyncDisposable? intellicodeLineCompletionsDisposable) : SuggestionBase
    {
        public SuggestionManagerBase SuggestionManager { get; } = suggestionManager;

        public VisualStudio.Threading.IAsyncDisposable? IntellicodeLineCompletionsDisposable { get; set; } = intellicodeLineCompletionsDisposable;

        public override TipStyle TipStyle => TipStyle.AlwaysShowTip | CopilotConstants.ShowThinkingStateTipStyle;

        public override EditDisplayStyle EditStyle => EditDisplayStyle.GrayText;

        public override bool HasMultipleSuggestions => false;

        public override event PropertyChangedEventHandler PropertyChanged { add { } remove { } }

        private SuggestionSessionBase? _suggestionSession;

        public override async Task OnAcceptedAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal, ReasonForAccept reason, CancellationToken cancel)
        {
            var threadingContext = providerInstance.ThreadingContext;

            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancel);
            await DisposeAsync().ConfigureAwait(false);
            Logger.Log(FunctionId.Copilot_Generate_Documentation_Accepted, logLevel: LogLevel.Information);
        }

        public override Task OnChangeProposalAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal, bool forward, CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        public override async Task OnDismissedAsync(SuggestionSessionBase session, ProposalBase? originalProposal, ProposalBase? currentProposal, ReasonForDismiss reason, CancellationToken cancel)
        {
            var threadingContext = providerInstance.ThreadingContext;
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancel);
            await ClearSuggestionAsync(reason, cancel).ConfigureAwait(false);
            Logger.Log(FunctionId.Copilot_Generate_Documentation_Dismissed, logLevel: LogLevel.Information);
        }

        public override Task OnProposalUpdatedAsync(SuggestionSessionBase session, ProposalBase? originalProposal, ProposalBase? currentProposal, ReasonForUpdate reason, VirtualSnapshotPoint caret, CompletionState? completionState, CancellationToken cancel)
        {
            if (reason.HasFlag(ReasonForUpdate.Diverged))
            {
                Logger.Log(FunctionId.Copilot_Generate_Documentation_Diverged, logLevel: LogLevel.Information);
                return session.DismissAsync(ReasonForDismiss.DismissedAfterBufferChange, cancel);
            }

            return Task.CompletedTask;
        }

        public async Task<SuggestionSessionBase?> GetSuggestionSessionAsync(CancellationToken cancellationToken)
        {
            SuggestionSessionBase? suggestionSession = null;

            await RunWithEnqueueActionAsync(
                "StartWork",
                async () => suggestionSession = await SuggestionManager.TryDisplaySuggestionAsync(this, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

            return suggestionSession;

        }

        public async Task TryDisplaySuggestionAsync(ProposalBase proposal, SuggestionSessionBase suggestionSession, CancellationToken cancellationToken)
        {
            var success = await TryDisplayProposalAsync(suggestionSession, proposal, cancellationToken).ConfigureAwait(false);
            if (success)
            {
                Logger.Log(FunctionId.Copilot_Generate_Documentation_Displayed, logLevel: LogLevel.Information);
            }
        }

        private async Task<bool> TryDisplayProposalAsync(SuggestionSessionBase session, ProposalBase proposal, CancellationToken cancellationToken)
        {
            try
            {
                await RunWithEnqueueActionAsync(
                    "DisplayProposal",
                    async () => await session.DisplayProposalAsync(proposal, cancellationToken).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException)
            {
                Logger.Log(FunctionId.Copilot_Generate_Documentation_Canceled, logLevel: LogLevel.Information);
            }

            return false;
        }

        /// <summary>
        /// In general, calls to a SuggestionManager or SuggestionSession need to be wrapped in an EnqueueAction.
        /// This is the pattern recommended by VS Platform to avoid races.
        /// </summary>
        public async Task RunWithEnqueueActionAsync(string description, Func<Task> action, CancellationToken cancel)
        {
            Assumes.NotNull(SuggestionManager);

            var taskCompletionSource = new TaskCompletionSource<bool>();

            await providerInstance.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancel);
            SuggestionManager.EnqueueAction(description, async () =>
            {
                try
                {
                    await action().ConfigureAwait(false);
                }

                finally
                {
                    taskCompletionSource.SetResult(true);
                }
            });

            if (!taskCompletionSource.Task.IsCompleted)
            {
                await TaskScheduler.Default;
                await taskCompletionSource.Task.WithCancellation(cancel).ConfigureAwait(false);
            }
        }

        private async Task ClearSuggestionAsync(ReasonForDismiss reason, CancellationToken cancellationToken)
        {
            if (_suggestionSession != null)
            {
                await _suggestionSession.DismissAsync(reason, cancellationToken).ConfigureAwait(false);
            }

            _suggestionSession = null;
            await DisposeAsync().ConfigureAwait(false);
        }

        private async Task DisposeAsync()
        {
            if (IntellicodeLineCompletionsDisposable != null)
            {
                await IntellicodeLineCompletionsDisposable.DisposeAsync().ConfigureAwait(false);
                IntellicodeLineCompletionsDisposable = null;
            }
        }
    }
}
