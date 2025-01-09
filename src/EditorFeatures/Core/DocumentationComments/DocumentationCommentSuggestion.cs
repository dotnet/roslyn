// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal class DocumentationCommentSuggestion(AbstractDocumentationCommentCommandHandler handlerInstance, ProposalBase proposal) : SuggestionBase
    {
        public ProposalBase Proposal { get; } = proposal;

        public override TipStyle TipStyle => TipStyle.AlwaysShowTip;

        public override EditDisplayStyle EditStyle => EditDisplayStyle.GrayText;

        public override bool HasMultipleSuggestions => false;

        public override event PropertyChangedEventHandler? PropertyChanged;

        public override Task OnAcceptedAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal, ReasonForAccept reason, CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        public override Task OnChangeProposalAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal, bool forward, CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        public override async Task OnDismissedAsync(SuggestionSessionBase session, ProposalBase? originalProposal, ProposalBase? currentProposal, ReasonForDismiss reason, CancellationToken cancel)
        {
            await handlerInstance._threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancel);

            await handlerInstance.ClearSuggestionAsync(reason, cancel).ConfigureAwait(false);

        }

        public override Task OnProposalUpdatedAsync(SuggestionSessionBase session, ProposalBase? originalProposal, ProposalBase? currentProposal, ReasonForUpdate reason, VirtualSnapshotPoint caret, CompletionState? completionState, CancellationToken cancel)
        {
            if (currentProposal is null)
            {
                return session.DismissAsync(ReasonForDismiss.DismissedAfterBufferChange, cancel);
            }

            return Task.CompletedTask;
        }
    }

    internal class DocumentationCommentHandlerProposal(VirtualSnapshotPoint snapshotPoint, IReadOnlyList<ProposedEdit> edits) : ProposalBase
    {
        public override IReadOnlyList<ProposedEdit> Edits => edits;

        public override VirtualSnapshotPoint Caret => snapshotPoint;

        public override CompletionState? CompletionState => null;

        public override ProposalFlags Flags => ProposalFlags.SingleTabToAccept;
    }
}
