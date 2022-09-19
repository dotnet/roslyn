// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.SolutionEvents;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteSolutionEventsAggregationService : BrokeredServiceBase, IRemoteSolutionEventsAggregationService
    {
        internal sealed class Factory : FactoryBase<IRemoteSolutionEventsAggregationService>
        {
            protected override IRemoteSolutionEventsAggregationService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteSolutionEventsAggregationService(arguments);
        }

        public RemoteSolutionEventsAggregationService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask OnSolutionEventAsync(Checksum solutionChecksum, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var aggregationService = solution.Services.GetRequiredService<ISolutionEventsAggregationService>();
                await aggregationService.OnSolutionEventAsync(solution, reasons, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask OnProjectEventAsync(Checksum solutionChecksum, ProjectId projectId, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var aggregationService = solution.Services.GetRequiredService<ISolutionEventsAggregationService>();
                await aggregationService.OnProjectEventAsync(solution, projectId, reasons, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask OnDocumentEventAsync(Checksum solutionChecksum, DocumentId documentId, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                var aggregationService = solution.Services.GetRequiredService<ISolutionEventsAggregationService>();
                await aggregationService.OnDocumentEventAsync(solution, documentId, reasons, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask OnSolutionChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, CancellationToken cancellationToken)
        {
            return RunServiceAsync(oldSolutionChecksum, newSolutionChecksum,
                async (oldSolution, newSolution) =>
                {
                    var aggregationService = oldSolution.Services.GetRequiredService<ISolutionEventsAggregationService>();
                    await aggregationService.OnSolutionChangedAsync(oldSolution, newSolution, cancellationToken).ConfigureAwait(false);
                }, cancellationToken);
        }

        public ValueTask OnProjectChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, ProjectId projectId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(oldSolutionChecksum, newSolutionChecksum,
                async (oldSolution, newSolution) =>
                {
                    var aggregationService = oldSolution.Services.GetRequiredService<ISolutionEventsAggregationService>();
                    await aggregationService.OnProjectChangedAsync(oldSolution, newSolution, projectId, cancellationToken).ConfigureAwait(false);
                }, cancellationToken);
        }

        public ValueTask OnDocumentChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(oldSolutionChecksum, newSolutionChecksum,
                async (oldSolution, newSolution) =>
                {
                    var aggregationService = oldSolution.Services.GetRequiredService<ISolutionEventsAggregationService>();
                    await aggregationService.OnDocumentChangedAsync(oldSolution, newSolution, documentId, cancellationToken).ConfigureAwait(false);
                }, cancellationToken);
        }
    }
}
