// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.ValueTracking;

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

        public ValueTask<ImmutableArray<SerializableValueTrackedItem>> TrackValueSourceAsync(Checksum solutionChecksum, TextSpan selection, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
                if (solution is null)
                {
                    throw new InvalidOperationException();
                }

                var document = solution.GetRequiredDocument(documentId);

                var progress = new ValueTrackingProgressCollector();
                await ValueTracker.TrackValueSourceAsync(selection, document, progress, cancellationToken).ConfigureAwait(false);

                var items = progress.GetItems();
                return items.SelectAsArray(item => SerializableValueTrackedItem.Dehydrate(solution, item, cancellationToken));
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<SerializableValueTrackedItem>> TrackValueSourceAsync(Checksum solutionChecksum, SerializableValueTrackedItem previousTrackedItem, CancellationToken cancellationToken)
        {
            return RunServiceAsync(solutionChecksum, async solution =>
            {
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
                await ValueTracker.TrackValueSourceAsync(solution, previousItem, progress, cancellationToken).ConfigureAwait(false);

                var items = progress.GetItems();
                return items.SelectAsArray(item => SerializableValueTrackedItem.Dehydrate(solution, item, cancellationToken));
            }, cancellationToken);
        }
    }
}
