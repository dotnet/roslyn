// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Copilot;

// The entire AdjusterProvider api is marked as obsolete since this is a preview API.  So we do the same here as well.
[Obsolete("This is a preview api and subject to change")]
[ContentType(ContentTypeNames.RoslynContentType)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RoslynProposalAdjusterProvider() : ProposalAdjusterProviderBase
{
    public override Task<ProposalBase> AdjustProposalBeforeDisplayAsync(ProposalBase proposal, string providerName, CancellationToken cancellationToken)
        => AdjustProposalAsync(proposal, providerName, before: true, cancellationToken);

    public override Task<ProposalBase> AdjustProposalAfterAcceptAsync(ProposalBase proposal, string providerName, CancellationToken cancellationToken)
        => AdjustProposalAsync(proposal, providerName, before: false, cancellationToken);

    private async Task<ProposalBase> AdjustProposalAsync(
        ProposalBase proposal, string providerName, bool before, CancellationToken cancellationToken)
    {
        // Ensure we're only operating on one solution.  It makes the logic much simpler, as we don't have to
        // worry about edits that touch multiple solutions.
        var solution = CopilotEditorUtilities.TryGetAffectedSolution(proposal);
        if (solution is null)
            return proposal;

        // We're potentially making multiple calls to oop here.  So keep a session alive to avoid
        // resyncing the solution and recomputing compilations.
        using var _1 = await RemoteKeepAliveSession.CreateAsync(solution, cancellationToken).ConfigureAwait(false);
        using var _2 = PooledObjects.ArrayBuilder<ProposedEdit>.GetInstance(out var finalEdits);

        var adjustmentsProposed = false;
        foreach (var editGroup in proposal.Edits.GroupBy(e => e.Span.Snapshot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = editGroup.Key;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            // Checked in TryGetAffectedSolution
            Contract.ThrowIfNull(document);

            var proposedEdits = await TryAdjustProposalDisplayAsync(
                document, CopilotEditorUtilities.TryGetNormalizedTextChanges(editGroup), cancellationToken).ConfigureAwait(false);

            if (proposedEdits.IsDefault)
            {
                // No changes were made to the proposal.  Just add the original edits.
                finalEdits.AddRange(editGroup);
            }
            else
            {
                // Changes were made to the proposal.  Add the new edits.
                adjustmentsProposed = true;
                foreach (var proposedEdit in proposedEdits)
                {
                    finalEdits.Add(new ProposedEdit(
                        new(snapshot, proposedEdit.Span.ToSpan()),
                        proposedEdit.NewText!));
                }
            }
        }

        // No adjustments were made.  Don't touch anything.
        if (!adjustmentsProposed)
        {
            using var _3 = Logger.LogBlock(FunctionId.Copilot_AdjustProposal, KeyValueLogMessage.Create(static (d, args) =>
            {
                var (providerName, before) = args;
                d["ProviderName"] = providerName;
                d["Before"] = before;
                d["AdjustmentsProposed"] = false;
            },
            args: (providerName, before)),
            cancellationToken);

            return proposal;
        }

        var newProposal = Proposal.TryCreateProposal(proposal, finalEdits);

        using var _4 = Logger.LogBlock(FunctionId.Copilot_AdjustProposal, KeyValueLogMessage.Create(static (d, args) =>
        {
            var (providerName, before, newProposal) = args;
            d["ProviderName"] = providerName;
            d["Before"] = before;
            d["AdjustmentsProposed"] = true;
            d["AdjustmentsAccepted"] = newProposal != null;
        },
        args: (providerName, before, newProposal)),
        cancellationToken);

        return newProposal ?? proposal;
    }

    private async Task<ImmutableArray<TextChange>> TryAdjustProposalDisplayAsync(
        Document document, ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken)
    {
        var proposalAdjusterService = document.Project.Solution.Services.GetRequiredService<ICopilotProposalAdjusterService>();
        return await proposalAdjusterService.TryAdjustProposalAsync(
            document, normalizedChanges, cancellationToken).ConfigureAwait(false);
    }
}
