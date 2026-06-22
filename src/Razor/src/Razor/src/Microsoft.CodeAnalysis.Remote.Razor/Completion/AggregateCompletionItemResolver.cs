// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Tooltip;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class AggregateCompletionItemResolver(IEnumerable<CompletionItemResolver> completionItemResolvers, ILoggerFactory loggerFactory)
{
    private readonly ImmutableArray<CompletionItemResolver> _completionItemResolvers = [.. completionItemResolvers];
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<AggregateCompletionItemResolver>();

    public async Task<VSInternalCompletionItem?> ResolveAsync(
        VSInternalCompletionItem item,
        VSInternalCompletionList containingCompletionList,
        ICompletionResolveContext originalRequestContext,
        VSInternalClientCapabilities clientCapabilities,
        IComponentAvailabilityService componentAvailabilityService,
        CancellationToken cancellationToken)
    {
        using var completionItemResolverTasks = new PooledArrayBuilder<Task<VSInternalCompletionItem?>>(_completionItemResolvers.Length);

        foreach (var completionItemResolver in _completionItemResolvers)
        {
            try
            {
                var task = completionItemResolver.ResolveAsync(item, containingCompletionList, originalRequestContext, clientCapabilities, componentAvailabilityService, cancellationToken);
                completionItemResolverTasks.Add(task);
            }
            catch (Exception ex) when (ex is not TaskCanceledException)
            {
                _logger.LogError(ex, $"Resolving completion item failed synchronously unexpectedly.");
            }
        }

        // We don't currently handle merging completion items because it's very rare for more than one resolution to take place.
        // Instead we'll prioritized the last completion item resolved.
        VSInternalCompletionItem? lastResolved = null;
        foreach (var completionItemResolverTask in completionItemResolverTasks)
        {
            try
            {
                var resolvedCompletionItem = await completionItemResolverTask.ConfigureAwait(false);
                if (resolvedCompletionItem is not null)
                {
                    lastResolved = resolvedCompletionItem;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, $"Resolving completion item failed unexpectedly.");
            }
        }

        return lastResolved;
    }
}
