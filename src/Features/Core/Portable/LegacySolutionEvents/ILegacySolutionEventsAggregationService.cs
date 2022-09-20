// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LegacySolutionEvents
{
    /// <summary>
    /// This is a legacy api intended only for existing SolutionCrawler partners to continue to function (albeit with
    /// ownership of that crawling task now belonging to the partner team, not roslyn).  It should not be used for any
    /// new services.
    /// </summary>
    internal interface ILegacySolutionEventsAggregationService : IWorkspaceService
    {
        ValueTask OnWorkspaceChangedAsync(ILegacyWorkspaceDescriptor descriptor, WorkspaceChangeEventArgs args, CancellationToken cancellationToken);
        ValueTask OnTextDocumentOpenedAsync(ILegacyWorkspaceDescriptor descriptor, TextDocumentEventArgs args, CancellationToken cancellationToken);
        ValueTask OnTextDocumentClosedAsync(ILegacyWorkspaceDescriptor descriptor, TextDocumentEventArgs args, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(ILegacySolutionEventsAggregationService)), Shared]
    internal class DefaultLegacySolutionEventsAggregationService : ILegacySolutionEventsAggregationService
    {
        private readonly ImmutableArray<Lazy<ILegacySolutionEventsListener>> _eventsServices;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultLegacySolutionEventsAggregationService(
            [ImportMany] IEnumerable<Lazy<ILegacySolutionEventsListener>> eventsServices)
        {
            _eventsServices = eventsServices.ToImmutableArray();
        }

        public async ValueTask OnWorkspaceChangedAsync(ILegacyWorkspaceDescriptor descriptor, WorkspaceChangeEventArgs args, CancellationToken cancellationToken)
        {
            foreach (var service in _eventsServices)
                await service.Value.OnWorkspaceChangedAsync(descriptor, args, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnTextDocumentOpenedAsync(ILegacyWorkspaceDescriptor descriptor, TextDocumentEventArgs args, CancellationToken cancellationToken)
        {
            foreach (var service in _eventsServices)
                await service.Value.OnTextDocumentOpenedAsync(descriptor, args, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnTextDocumentClosedAsync(ILegacyWorkspaceDescriptor descriptor, TextDocumentEventArgs args, CancellationToken cancellationToken)
        {
            foreach (var service in _eventsServices)
                await service.Value.OnTextDocumentClosedAsync(descriptor, args, cancellationToken).ConfigureAwait(false);
        }
    }
}
