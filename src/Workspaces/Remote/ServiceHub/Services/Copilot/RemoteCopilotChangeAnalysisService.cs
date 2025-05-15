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

internal sealed partial class RemoteCopilotChangeAnalysisService(
    in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteCopilotChangeAnalysisService
{
    internal sealed class Factory : FactoryBase<IRemoteCopilotChangeAnalysisService>
    {
        protected override IRemoteCopilotChangeAnalysisService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteCopilotChangeAnalysisService(arguments);
    }

    public ValueTask<CopilotChangeAnalysis> AnalyzeChangeAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        ImmutableArray<TextChange> edits,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = await solution.GetRequiredDocumentAsync(
                documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

            var service = solution.Services.GetRequiredService<ICopilotChangeAnalysisService>();
            return await service.AnalyzeChangeAsync(document, edits, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
