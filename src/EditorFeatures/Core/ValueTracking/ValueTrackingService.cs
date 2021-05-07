// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ValueTracking
{
    [ExportWorkspaceService(typeof(IValueTrackingService)), Shared]
    internal partial class ValueTrackingService : IValueTrackingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ValueTrackingService()
        {
        }

        public async Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(
            TextSpan selection,
            Document document,
            CancellationToken cancellationToken)
        {
            using var logger = Logger.LogBlock(FunctionId.ValueTracking_TrackValueSource, cancellationToken, LogLevel.Information);
            var progressTracker = new ValueTrackingProgressCollector();
            await ValueTracker.TrackValueSourceInternalAsync(selection, document, progressTracker, cancellationToken).ConfigureAwait(false);
            return progressTracker.GetItems();
        }

        public async Task TrackValueSourceAsync(
            TextSpan selection,
            Document document,
            ValueTrackingProgressCollector progressCollector,
            CancellationToken cancellationToken)
        {
            using var logger = Logger.LogBlock(FunctionId.ValueTracking_TrackValueSource, cancellationToken, LogLevel.Information);
            await ValueTracker.TrackValueSourceInternalAsync(
                selection,
                document,
                progressCollector,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(
            ValueTrackedItem previousTrackedItem,
            CancellationToken cancellationToken)
        {
            using var logger = Logger.LogBlock(FunctionId.ValueTracking_TrackValueSource, cancellationToken, LogLevel.Information);
            var progressTracker = new ValueTrackingProgressCollector();
            await ValueTracker.TrackValueSourceInternalAsync(previousTrackedItem, progressTracker, cancellationToken).ConfigureAwait(false);
            return progressTracker.GetItems();
        }

        public async Task TrackValueSourceAsync(
            ValueTrackedItem previousTrackedItem,
            ValueTrackingProgressCollector progressCollector,
            CancellationToken cancellationToken)
        {
            using var logger = Logger.LogBlock(FunctionId.ValueTracking_TrackValueSource, cancellationToken, LogLevel.Information);
            await ValueTracker.TrackValueSourceInternalAsync(
                previousTrackedItem,
                progressCollector,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
