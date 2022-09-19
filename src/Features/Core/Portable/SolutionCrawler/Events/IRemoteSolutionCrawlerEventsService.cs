// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal interface IRemoteSolutionCrawlerEventsAggregationService
    {
        ValueTask OnSolutionEventAsync(Checksum solutionChecksum, InvocationReasons reasons, CancellationToken cancellationToken);
        ValueTask OnDocumentEventAsync(Checksum solutionChecksum, DocumentId documentId, InvocationReasons reasons, CancellationToken cancellationToken);
        ValueTask OnProjectEventAsync(Checksum solutionChecksum, ProjectId projectId, InvocationReasons reasons, CancellationToken cancellationToken);

        ValueTask OnSolutionChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, CancellationToken cancellationToken);
        ValueTask OnProjectChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, ProjectId projectId, CancellationToken cancellationToken);
        ValueTask OnDocumentChangedAsync(Checksum oldSolutionChecksum, Checksum newSolutionChecksum, DocumentId documentId, CancellationToken cancellationToken);
    }

    internal interface ISolutionCrawlerEventsAggregationService
    {
        ValueTask OnSolutionEventAsync(Solution solution, InvocationReasons reasons, CancellationToken cancellationToken);
        ValueTask OnProjectEventAsync(Solution solution, ProjectId projectId, InvocationReasons reasons, CancellationToken cancellationToken);
        ValueTask OnDocumentEventAsync(Solution solution, DocumentId documentId, InvocationReasons reasons, CancellationToken cancellationToken);

        ValueTask OnSolutionChangedAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken);
        ValueTask OnProjectChangedAsync(Solution oldSolution, Solution newSolution, ProjectId projectId, CancellationToken cancellationToken);
        ValueTask OnDocumentChangedAsync(Solution oldSolution, Solution newSolution, DocumentId documentId, CancellationToken cancellationToken);
    }

    internal interface ISolutionCrawlerEventsService
    {
        ValueTask OnSolutionEventAsync(Solution solution, InvocationReasons reasons, CancellationToken cancellationToken);
        ValueTask OnDocumentEventAsync(Solution solution, DocumentId documentId, InvocationReasons reasons, CancellationToken cancellationToken);
        ValueTask OnProjectEventAsync(Solution solution, ProjectId projectId, InvocationReasons reasons, CancellationToken cancellationToken);

        ValueTask OnSolutionChangedAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken);
        ValueTask OnProjectChangedAsync(Solution oldSolution, Solution newSolution, ProjectId projectId, CancellationToken cancellationToken);
        ValueTask OnDocumentChangedAsync(Solution oldSolution, Solution newSolution, DocumentId documentId, CancellationToken cancellationToken);
    }

    [Export(typeof(ISolutionCrawlerEventsAggregationService)), Shared]
    internal class DefaultSolutionCrawlerEventsAggregationService : ISolutionCrawlerEventsAggregationService
    {
        private readonly ImmutableArray<Lazy<ISolutionCrawlerEventsService>> _eventsServices;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultSolutionCrawlerEventsAggregationService(
            [ImportMany] IEnumerable<Lazy<ISolutionCrawlerEventsService>> eventsServices)
        {
            _eventsServices = eventsServices.ToImmutableArray();
        }

        public async ValueTask OnSolutionEventAsync(Solution solution, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            foreach (var service in _eventsServices)
                await service.Value.OnSolutionEventAsync(solution, reasons, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnProjectEventAsync(Solution solution, ProjectId projectId, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            foreach (var service in _eventsServices)
                await service.Value.OnProjectEventAsync(solution, projectId, reasons, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnDocumentEventAsync(Solution solution, DocumentId documentId, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            foreach (var service in _eventsServices)
                await service.Value.OnDocumentEventAsync(solution, documentId, reasons, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnSolutionChangedAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
        {
            foreach (var service in _eventsServices)
                await service.Value.OnSolutionChangedAsync(oldSolution, newSolution, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnProjectChangedAsync(Solution oldSolution, Solution newSolution, ProjectId projectId, CancellationToken cancellationToken)
        {
            foreach (var service in _eventsServices)
                await service.Value.OnProjectChangedAsync(oldSolution, newSolution, projectId, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnDocumentChangedAsync(Solution oldSolution, Solution newSolution, DocumentId documentId, CancellationToken cancellationToken)
        {
            foreach (var service in _eventsServices)
                await service.Value.OnDocumentChangedAsync(oldSolution, newSolution, documentId, cancellationToken).ConfigureAwait(false);
        }
    }
}
