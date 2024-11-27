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
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal class DocumentationCommentSuggestion : SuggestionBase
    {
        public ProposalBase Proposal { get; }

        private readonly AbstractDocumentationCommentCommandHandler _handlerInstance;

        public DocumentationCommentSuggestion(AbstractDocumentationCommentCommandHandler handlerInstance, ProposalBase proposal)
        {
            Proposal = proposal;
            _handlerInstance = handlerInstance;
        }

        public override TipStyle TipStyle => TipStyle.TipRightPlacement;

        public override EditDisplayStyle EditStyle => EditDisplayStyle.GrayText;

        public override bool HasMultipleSuggestions => false;

        public override event PropertyChangedEventHandler PropertyChanged;

        public override Task OnAcceptedAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal, ReasonForAccept reason, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public override Task OnChangeProposalAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal, bool forward, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public override Task OnDismissedAsync(SuggestionSessionBase session, ProposalBase? originalProposal, ProposalBase? currentProposal, ReasonForDismiss reason, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public override Task OnProposalUpdatedAsync(SuggestionSessionBase session, ProposalBase? originalProposal, ProposalBase? currentProposal, ReasonForUpdate reason, VirtualSnapshotPoint caret, CompletionState? completionState, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }
    }

    internal class DocumentationCommentHandlerProposal : ProposalBase
    {
        private readonly VirtualSnapshotPoint _snapshotPoint;
        private readonly IReadOnlyList<ProposedEdit> _edits;

        public DocumentationCommentHandlerProposal(VirtualSnapshotPoint snapshotPoint, IReadOnlyList<ProposedEdit> edits)
        {
            _snapshotPoint = snapshotPoint;
            _edits = edits;
        }

        public override IReadOnlyList<ProposedEdit> Edits => _edits;

        public override VirtualSnapshotPoint Caret => _snapshotPoint;

        public override CompletionState? CompletionState => null;

        public override ProposalFlags Flags => ProposalFlags.SingleTabToAccept;
    }
}
