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
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ValueTracking;

[ExportWorkspaceService(typeof(IValueTrackingService)), Shared]
internal sealed partial class ValueTrackingService : IValueTrackingService
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
        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var solution = document.Project.Solution;

            var result = await client.TryInvokeAsync<IRemoteValueTrackingService, ImmutableArray<SerializableValueTrackedItem>>(
                solution,
                (service, solutionInfo, cancellationToken) => service.TrackValueSourceAsync(solutionInfo, selection, document.Id, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.HasValue)
            {
                return [];
            }

            return await result.Value.SelectAsArrayAsync(
                static (item, solution, cancellationToken) => item.RehydrateAsync(solution, cancellationToken), solution, cancellationToken).ConfigureAwait(false);
        }

        var progressTracker = new ValueTrackingProgressCollector();
        await ValueTracker.TrackValueSourceAsync(selection, document, progressTracker, cancellationToken).ConfigureAwait(false);
        return progressTracker.GetItems();
    }

    public async Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(
        Solution solution,
        ValueTrackedItem previousTrackedItem,
        CancellationToken cancellationToken)
    {
        using var logger = Logger.LogBlock(FunctionId.ValueTracking_TrackValueSource, cancellationToken, LogLevel.Information);
        var project = solution.GetRequiredProject(previousTrackedItem.DocumentId.ProjectId);
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var dehydratedItem = SerializableValueTrackedItem.Dehydrate(solution, previousTrackedItem, cancellationToken);
            var result = await client.TryInvokeAsync<IRemoteValueTrackingService, ImmutableArray<SerializableValueTrackedItem>>(
                solution,
                (service, solutionInfo, cancellationToken) => service.TrackValueSourceAsync(solutionInfo, dehydratedItem, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.HasValue)
            {
                return [];
            }

            return await result.Value.SelectAsArrayAsync(
                static (item, solution, cancellationToken) => item.RehydrateAsync(solution, cancellationToken), solution, cancellationToken).ConfigureAwait(false);
        }

        var progressTracker = new ValueTrackingProgressCollector();
        await ValueTracker.TrackValueSourceAsync(solution, previousTrackedItem, progressTracker, cancellationToken).ConfigureAwait(false);
        return progressTracker.GetItems();
    }
}
