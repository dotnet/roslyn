// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteCopilotProposalAdjusterService(
    in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteCopilotProposalAdjusterService
{
    internal sealed class Factory : FactoryBase<IRemoteCopilotProposalAdjusterService>
    {
        protected override IRemoteCopilotProposalAdjusterService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteCopilotProposalAdjusterService(arguments);
    }

    public ValueTask<ProposalAdjustmentResult> TryAdjustProposalAsync(
        ImmutableHashSet<string> allowableAdjustments,
        Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TextChange> textChanges, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = await solution.GetRequiredDocumentAsync(
                documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

            var service = document.GetRequiredLanguageService<ICopilotProposalAdjusterService>();
            return await service.TryAdjustProposalAsync(allowableAdjustments, document, textChanges, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
