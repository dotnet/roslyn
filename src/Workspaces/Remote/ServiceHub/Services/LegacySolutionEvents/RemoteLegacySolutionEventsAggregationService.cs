// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LegacySolutionEvents;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteLegacySolutionEventsAggregationService : BrokeredServiceBase, IRemoteLegacySolutionEventsAggregationService
    {
        internal sealed class Factory : FactoryBase<IRemoteLegacySolutionEventsAggregationService>
        {
            protected override IRemoteLegacySolutionEventsAggregationService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteLegacySolutionEventsAggregationService(arguments);
        }

        public RemoteLegacySolutionEventsAggregationService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask OnSolutionEventAsync(Checksum solutionChecksum, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var aggregationService = solution.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();
                await aggregationService.OnSolutionEventAsync(solution, reasons, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask OnProjectEventAsync(Checksum solutionChecksum, ProjectId projectId, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var aggregationService = solution.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();
                await aggregationService.OnProjectEventAsync(solution, projectId, reasons, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask OnDocumentEventAsync(Checksum solutionChecksum, DocumentId documentId, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var aggregationService = solution.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();
                await aggregationService.OnDocumentEventAsync(solution, documentId, reasons, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask OnSolutionChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, CancellationToken cancellationToken)
        {
            return RunServiceAsync(oldSolutionChecksum, newSolutionChecksum,
                async (oldSolution, newSolution) =>
                {
                    var aggregationService = oldSolution.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();
                    await aggregationService.OnSolutionChangedAsync(oldSolution, newSolution, cancellationToken).ConfigureAwait(false);
                }, cancellationToken);
        }

        public ValueTask OnProjectChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, ProjectId projectId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(oldSolutionChecksum, newSolutionChecksum,
                async (oldSolution, newSolution) =>
                {
                    var aggregationService = oldSolution.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();
                    await aggregationService.OnProjectChangedAsync(oldSolution, newSolution, projectId, cancellationToken).ConfigureAwait(false);
                }, cancellationToken);
        }

        public ValueTask OnDocumentChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(oldSolutionChecksum, newSolutionChecksum,
                async (oldSolution, newSolution) =>
                {
                    var aggregationService = oldSolution.Services.GetRequiredService<ILegacySolutionEventsAggregationService>();
                    await aggregationService.OnDocumentChangedAsync(oldSolution, newSolution, documentId, cancellationToken).ConfigureAwait(false);
                }, cancellationToken);
        }
    }
}
