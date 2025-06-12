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
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

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
        // Ensure we're only operating on one solution.  It makes the logic much simpler, as we don't have to
        // worry about edits that touch multiple solutions.
        var solution = TryGetAffectedSolution(proposal);
        if (solution is null)
            return proposal;

        var forkedSolution = solution;

        using var _1 = PooledObjects.PooledDictionary<DocumentId, (ImmutableArray<TextChange> originalChanges, ImmutableArray<TextSpan> updatedSpans)>.GetInstance(out var documentToEdits);

        foreach (var editGroup in proposal.Edits.GroupBy(e => e.Span.Snapshot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = editGroup.Key;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            // Checked in TryGetAffectedSolution
            Contract.ThrowIfNull(document);

            using var _2 = PooledObjects.ArrayBuilder<TextSpan>.GetInstance(out var newSpans);

            var textChanges = editGroup.SelectAsArray(edit => new TextChange(edit.Span.Span.ToTextSpan(), edit.ReplacementText));
            var oldText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = CopilotChangeAnalysisUtilities.GetNewText(oldText, textChanges, newSpans);

            forkedSolution = forkedSolution.WithDocumentText(document.Id, newText, PreservationMode.PreserveIdentity);
            documentToEdits.Add(document.Id, (textChanges, newSpans.ToImmutableAndClear()));
        }

        // We're potentially making multiple calls to oop here.  So keep a session alive to avoid
        // resyncing the solution and recomputing compilations.
        using var _3 = await RemoteKeepAliveSession.CreateAsync(forkedSolution, cancellationToken).ConfigureAwait(false);

        var client = await RemoteHostClient.TryGetClientAsync(forkedSolution.Services, cancellationToken).ConfigureAwait(false);

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
    }
}

internal interface IRemoteProposalAdjusterService
{
    ImmutableArray<TextChange> AdjustProposalAsync(
        Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TextChange> edits, CancellationToken cancellationToken);
}
