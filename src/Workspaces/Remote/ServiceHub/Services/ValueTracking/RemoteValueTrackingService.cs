// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.ValueTracking;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteValueTrackingService : BrokeredServiceBase, IRemoteValueTrackingService
    {
        internal sealed class Factory : FactoryBase<IRemoteValueTrackingService>
        {
            protected override IRemoteValueTrackingService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteValueTrackingService(arguments);
        }

        public RemoteValueTrackingService(ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }


        public ValueTask<ImmutableArray<SerializableValueTrackedItem>> TrackValueSourceAsync(PinnedSolutionInfo solutionInfo, TextSpan selection, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                if (solution is null)
                {
                    throw new InvalidOperationException();
                }

                var document = solution.GetRequiredDocument(documentId);

                var progress = new ValueTrackingProgressCollector();
                await ValueTracker.TrackValueSourceInternalAsync(selection, document, progress, cancellationToken).ConfigureAwait(false);

                var items = progress.GetItems();
                return items.SelectAsArray(item => SerializableValueTrackedItem.Dehydrate(item, cancellationToken));
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<SerializableValueTrackedItem>> TrackValueSourceAsync(SerializableValueTrackedItem previousTrackedItem, PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                if (solution is null)
                {
                    throw new InvalidOperationException();
                }

                var previousItem = await previousTrackedItem.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                if (previousItem is null)
                {
                    return ImmutableArray<SerializableValueTrackedItem>.Empty;
                }

                var progress = new ValueTrackingProgressCollector();
                await ValueTracker.TrackValueSourceInternalAsync(previousItem, progress, cancellationToken).ConfigureAwait(false);

                var items = progress.GetItems();
                return items.SelectAsArray(item => SerializableValueTrackedItem.Dehydrate(item, cancellationToken));
            }, cancellationToken);
        }
    }
}
