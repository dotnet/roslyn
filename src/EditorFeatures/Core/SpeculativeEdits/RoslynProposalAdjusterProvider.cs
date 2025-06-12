// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.SpeculativeEdits;

// The entire AdjusterProvider api is marked as obsolete since this is a preview API.  So we do the same here as well.
[Obsolete("This is a preview api and subject to change")]
[ContentType(ContentTypeNames.RoslynContentType)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RoslynProposalAdjusterProvider() : ProposalAdjusterProviderBase
{
    public override async Task<ProposalBase> AdjustProposalBeforeDisplayAsync(
        ProposalBase proposal, string providerName, CancellationToken cancellationToken)
    {
        var solution = TryGetAffectedSolution(proposal);
        if (solution is null)
            return proposal;

        using var _2 = PooledObjects.PooledDictionary<Document, IEnumerable<ProposedEdit>>.GetInstance(out var documentToEdits);

        var forkedSolution = solution;
        foreach (var group in proposal.Edits.GroupBy(e => e.Span.Snapshot))
        {
            var document = group.Key.GetOpenDocumentInCurrentContextWithChanges();

            // Verified by TryGetAffectedSolution
            Contract.ThrowIfNull(document);

            var oldText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = oldText.WithChanges(group.Select(e => new TextChange(e.Span.Span.ToTextSpan(), e.ReplacementText)));


        }

        solution.WithDocumentTexts()

        // We're potentially making multiple calls to oop here.  So keep a session alive to avoid
        // resyncing the solution and recomputing compilations.
        using var _ = await RemoteKeepAliveSession.CreateAsync(solution, cancellationToken).ConfigureAwait(false);
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);

        var finalEdits = await ProducerConsumer<ImmutableArray<ProposedEdit>>.RunParallelAsync(
            documentToEdits,
            static async (kvp, callback, client, cancellationToken) =>
            {
                var document = kvp.Key;
                var edits = kvp.Value;

                await client.TryInvokeAsync<IRemoteProposalAdjusterService>(
                    document.Project,
                    (service, checksum, args, cancellationToken) =>
                        service.AdjustProposalAsync(solutionInfo, document.Id, edits.ToImmutableArray(), cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            },
            args: client,
            cancellationToken).ConfigureAwait(false);

        if (finalEdits.Length == proposal.Edits.Count)
        {
            // No edits were added.  Don't touch anything.
            return proposal;
        }

        return proposal with
        {
            Edits = finalEdits.SelectMany(e => e).ToImmutableArray()
        };
    }

    private static Solution? TryGetAffectedSolution(ProposalBase proposal)
    {
        Solution? solution = null;
        foreach (var edit in proposal.Edits)
        {
            var currentSolution = edit.Span.Snapshot.GetOpenDocumentInCurrentContextWithChanges()?.Project.Solution;

            // Edit touches a file roslyn doesn't know about.  Don't touch this.
            if (currentSolution is null)
                return null;

            // Edit touches multiple solutions.  Don't bother with this for now for simplicities sake.
            if (solution != null && solution != currentSolution)
                return null;

            solution = currentSolution;
        }

        return solution;
    }
}

internal interface IRemoteProposalAdjusterService
{
    ImmutableArray<TextChange> AdjustProposalAsync(
        Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TextChange> edits, CancellationToken cancellationToken);
}
