// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UtilityTest;

public sealed class AsyncPriorityWorkQueueTests
{
    [Fact]
    public async Task ProcessesItemsInDescendingPriorityOrderInSingleBatch()
    {
        var processed = new List<string>();
        var callbackCount = 0;
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<string>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                callbackCount++;
                processed.AddRange(await DrainAsync(workToProcess));
            },
            equalityComparer: EqualityComparer<string>.Default,
            asyncListener: listener);

        queue.AddWork("low", priority: 0);
        queue.AddWork("high", priority: 2);
        queue.AddWork("middle", priority: 1);

        await listener.ExpeditedWaitAsync();

        Assert.Equal(1, callbackCount);
        Assert.Equal(new[] { "high", "middle", "low" }, processed);
    }

    [Fact]
    public async Task ProcessesAllItemsAtSamePriority()
    {
        var processed = new List<int>();
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) => processed.AddRange(await DrainAsync(workToProcess)),
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        queue.AddWork(1, priority: 1);
        queue.AddWork(2, priority: 1);
        queue.AddWork(3, priority: 1);

        await listener.ExpeditedWaitAsync();

        Assert.Equal(new[] { 1, 2, 3 }, processed.OrderBy(static item => item));
    }

    [Fact]
    public async Task ComparerEquivalentItemsAreProcessedOnce()
    {
        var processed = new List<string>();
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<string>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) => processed.AddRange(await DrainAsync(workToProcess)),
            equalityComparer: StringComparer.OrdinalIgnoreCase,
            asyncListener: listener);

        queue.AddWork("item", priority: 1);
        queue.AddWork("ITEM", priority: 1);

        await listener.ExpeditedWaitAsync();

        Assert.Equal("item", Assert.Single(processed));
    }

    [Fact]
    public async Task ReAddingAtHigherPriorityPromotesWithoutDuplicating()
    {
        var processed = new List<string>();
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<string>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) => processed.AddRange(await DrainAsync(workToProcess)),
            equalityComparer: EqualityComparer<string>.Default,
            asyncListener: listener);

        queue.AddWork("item", priority: 0);
        queue.AddWork("middle", priority: 1);
        queue.AddWork("item", priority: 2);

        await listener.ExpeditedWaitAsync();

        Assert.Equal(new[] { "item", "middle" }, processed);
    }

    [Fact]
    public async Task ReAddingAtLowerPriorityDoesNotDemote()
    {
        var processed = new List<string>();
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<string>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) => processed.AddRange(await DrainAsync(workToProcess)),
            equalityComparer: EqualityComparer<string>.Default,
            asyncListener: listener);

        queue.AddWork("item", priority: 2);
        queue.AddWork("item", priority: 0);
        queue.AddWork("middle", priority: 1);

        await listener.ExpeditedWaitAsync();

        Assert.Equal(new[] { "item", "middle" }, processed);
    }

    [Fact]
    public async Task AdditionsWhileBatchRunsAreVisibleToCurrentBatch()
    {
        using var callbackStarted = new Barrier(participantCount: 2);
        using var continueCallback = new Barrier(participantCount: 2);
        var processed = new List<int>();
        var callbackCount = 0;
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromMilliseconds(1),
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                callbackCount++;
                callbackStarted.SignalAndWait(cancellationToken);
                continueCallback.SignalAndWait(cancellationToken);
                processed.AddRange(await DrainAsync(workToProcess));
            },
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);
        queue.AddWork(1, priority: 0);
        callbackStarted.SignalAndWait();

        queue.AddWork(2, priority: 1);
        continueCallback.SignalAndWait();
        await listener.ExpeditedWaitAsync();

        Assert.Equal(1, callbackCount);
        Assert.Equal(new[] { 2, 1 }, processed);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    public async Task ChangePriorityMovesScheduledItemWithoutDuplicating(int originalPriority, int newPriority)
    {
        var processed = new List<string>();
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<string>(
            maximumPriority: 2,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) => processed.AddRange(await DrainAsync(workToProcess)),
            equalityComparer: EqualityComparer<string>.Default,
            asyncListener: listener);

        queue.AddWork("item", originalPriority);
        queue.AddWork("middle", priority: 1);
        queue.ChangeWorkPriorityIfScheduled("item", newPriority);

        await listener.ExpeditedWaitAsync();

        Assert.Equal(newPriority > 1 ? new[] { "item", "middle" } : new[] { "middle", "item" }, processed);
    }

    [Fact]
    public async Task ChangePriorityUsesEqualityComparer()
    {
        var processed = new List<string>();
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<string>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) => processed.AddRange(await DrainAsync(workToProcess)),
            equalityComparer: StringComparer.OrdinalIgnoreCase,
            asyncListener: listener);

        queue.AddWork("item", priority: 3);
        queue.AddWork("middle", priority: 1);
        queue.ChangeWorkPriorityIfScheduled("ITEM", newPriority: 0);

        await listener.ExpeditedWaitAsync();

        Assert.Equal(new[] { "middle", "item" }, processed, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangePriorityDoesNotScheduleAbsentItem()
    {
        var processed = new List<string>();
        var callbackCount = 0;
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<string>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                callbackCount++;
                processed.AddRange(await DrainAsync(workToProcess));
            },
            equalityComparer: EqualityComparer<string>.Default,
            asyncListener: listener);

        queue.ChangeWorkPriorityIfScheduled("absent", newPriority: 3);
        await listener.ExpeditedWaitAsync();
        Assert.Equal(0, callbackCount);

        queue.AddWork("scheduled", priority: 0);
        queue.ChangeWorkPriorityIfScheduled("absent", newPriority: 3);
        await listener.ExpeditedWaitAsync();

        Assert.Equal(1, callbackCount);
        Assert.Equal(new[] { "scheduled" }, processed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void ChangePriorityRejectsOutOfRangePriority(int newPriority)
    {
        using var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1),
            processBatchAsync: async (workToProcess, cancellationToken) => { },
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: AsynchronousOperationListenerProvider.NullListener);

        Assert.Throws<ArgumentOutOfRangeException>("newPriority", () => queue.ChangeWorkPriorityIfScheduled(1, newPriority));
    }

    [Fact]
    public async Task ChangePriorityAfterDisposeDoesNotRun()
    {
        var processed = false;
        var listener = new AsynchronousOperationListener();
        var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) => processed = true,
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        queue.AddWork(1, priority: 0);
        queue.Dispose();
        queue.ChangeWorkPriorityIfScheduled(1, newPriority: 3);

        await listener.ExpeditedWaitAsync();

        Assert.False(processed);
    }

    [Fact]
    public async Task PendingItemCanBeReprioritizedWhileBatchRuns()
    {
        using var firstItemProcessed = new Barrier(participantCount: 2);
        using var continueCallback = new Barrier(participantCount: 2);
        var processed = new List<int>();
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                await workToProcess.TryProcessNextItemAsync(async item => processed.Add(item));

                firstItemProcessed.SignalAndWait(cancellationToken);
                continueCallback.SignalAndWait(cancellationToken);
                processed.AddRange(await DrainAsync(workToProcess));
            },
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        queue.AddWork(10, priority: 0);
        queue.AddWork(20, priority: 2);
        queue.AddWork(30, priority: 3);
        var waitTask = listener.ExpeditedWaitAsync();
        firstItemProcessed.SignalAndWait();

        queue.AddWork(10, priority: 3);
        continueCallback.SignalAndWait();
        await waitTask;

        Assert.Equal(new[] { 30, 10, 20 }, processed);
    }

    [Fact]
    public async Task WorkAddedAfterDrainingUsesNewCallback()
    {
        using var firstBatchDrained = new Barrier(participantCount: 2);
        using var allowFirstCallbackToReturn = new Barrier(participantCount: 2);
        var firstBatch = new List<int>();
        var secondBatch = new List<int>();
        var callbackCount = 0;
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                if (++callbackCount == 1)
                {
                    firstBatch.AddRange(await DrainAsync(workToProcess));
                    firstBatchDrained.SignalAndWait(cancellationToken);
                    allowFirstCallbackToReturn.SignalAndWait(cancellationToken);
                }
                else
                {
                    secondBatch.AddRange(await DrainAsync(workToProcess));
                }
            },
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        queue.AddWork(1, priority: 0);
        var waitTask = listener.ExpeditedWaitAsync();
        firstBatchDrained.SignalAndWait();

        queue.AddWork(2, priority: 0);
        allowFirstCallbackToReturn.SignalAndWait();
        await waitTask;

        Assert.Equal(2, callbackCount);
        Assert.Equal(new[] { 1 }, firstBatch);
        Assert.Equal(new[] { 2 }, secondBatch);
    }

    [Fact]
    public async Task ConcurrentConsumersProcessWithoutDuplicates()
    {
        var processed = new List<int>();
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                using var consumersReady = new Barrier(participantCount: 2);

                var firstConsumer = Task.Run(() =>
                {
                    consumersReady.SignalAndWait();
                    return DrainAsync(workToProcess);
                });

                var secondConsumer = Task.Run(() =>
                {
                    consumersReady.SignalAndWait();
                    return DrainAsync(workToProcess);
                });

                var results = await Task.WhenAll(firstConsumer, secondConsumer);
                processed.AddRange(results.SelectMany(static result => result));
            },
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        for (var item = 0; item < 100; item++)
            queue.AddWork(item, priority: 0);

        await listener.ExpeditedWaitAsync();

        Assert.Equal(Enumerable.Range(0, 100), processed.OrderBy(static item => item));
    }

    [Fact]
    public async Task RequeuedItemIsNotProcessedWhileAlreadyInFlight()
    {
        using var firstItemStarted = new Barrier(participantCount: 2);
        using var additionalWorkQueued = new Barrier(participantCount: 2);
        using var completeFirstItem = new Barrier(participantCount: 2);
        var processedWhileFirstItemInFlight = new List<string>();
        var processedAfterFirstItem = new List<string>();
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<string>(
            maximumPriority: 1,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                var firstProcessing = Task.Run(async () =>
                    await workToProcess.TryProcessNextItemAsync(async item =>
                    {
                        firstItemStarted.SignalAndWait(cancellationToken);

                        // Don't let the first item complete until we've drained the rest of all items
                        completeFirstItem.SignalAndWait(cancellationToken);
                    }));

                try
                {
                    additionalWorkQueued.SignalAndWait(cancellationToken);
                    processedWhileFirstItemInFlight.AddRange(await DrainAsync(workToProcess));
                }
                finally
                {
                    completeFirstItem.SignalAndWait(cancellationToken);
                    await firstProcessing;
                }

                processedAfterFirstItem.AddRange(await DrainAsync(workToProcess));
            },
            equalityComparer: EqualityComparer<string>.Default,
            asyncListener: listener);

        queue.AddWork("requeued", priority: 1);
        var waitTask = listener.ExpeditedWaitAsync();
        firstItemStarted.SignalAndWait();

        queue.AddWork("requeued", priority: 1);
        queue.AddWork("unrelated", priority: 0);
        additionalWorkQueued.SignalAndWait();
        await waitTask;

        Assert.Equal(new[] { "unrelated" }, processedWhileFirstItemInFlight);
        Assert.Equal(new[] { "requeued" }, processedAfterFirstItem);
    }

    [Fact]
    public async Task ReturningBeforeDrainingSchedulesRemainingWorkInLaterBatch()
    {
        var processed = new List<int>();
        var callbackCount = 0;
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                callbackCount++;
                await workToProcess.TryProcessNextItemAsync(async item => processed.Add(item));
            },
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        queue.AddWork(1, priority: 0);
        queue.AddWork(2, priority: 1);

        await listener.ExpeditedWaitAsync();

        Assert.Equal(2, callbackCount);
        Assert.Equal(new[] { 2, 1 }, processed);
    }

    [Fact]
    public async Task DrainedBatchAllowsLaterWork()
    {
        var processed = new List<int>();
        var callbackCount = 0;
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                callbackCount++;
                await workToProcess.TryProcessNextItemAsync(async item => processed.Add(item));
            },
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        queue.AddWork(1, priority: 0);
        await listener.ExpeditedWaitAsync();

        queue.AddWork(2, priority: 0);
        await listener.ExpeditedWaitAsync();

        Assert.Equal(2, callbackCount);
        Assert.Equal(new[] { 1, 2 }, processed);
    }

    [Fact]
    public async Task FaultedDrainedBatchDoesNotBlockLaterWork()
    {
        var processed = new List<int>();
        var callbackCount = 0;
        var listener = new AsynchronousOperationListener();
        using var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.Zero,
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                processed.AddRange(await DrainAsync(workToProcess));
                if (++callbackCount == 1)
                    throw new InvalidOperationException("Expected test exception.");
            },
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        queue.AddWork(1, priority: 0);
        await listener.ExpeditedWaitAsync();

        queue.AddWork(2, priority: 0);
        await listener.ExpeditedWaitAsync();

        Assert.Equal(2, callbackCount);
        Assert.Equal(new[] { 1, 2 }, processed);
    }

    [Fact]
    public async Task DisposeBeforeDelayedBatchDropsQueuedWork()
    {
        var processed = false;
        var listener = new AsynchronousOperationListener();
        var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) => processed = true,
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        queue.AddWork(1, priority: 0);
        queue.Dispose();

        await listener.ExpeditedWaitAsync();

        Assert.False(processed);
    }

    [Fact]
    public async Task DisposeDuringBatchCancelsTokenAndClearsUnconsumedWork()
    {
        using var callbackStarted = new Barrier(participantCount: 2);
        var cancellationObserved = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var processed = new List<int>();
        var foundItemAfterDispose = true;
        var listener = new AsynchronousOperationListener();
        var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.FromDays(1), // ExpeditedWaitAsync skips this delay
            processBatchAsync: async (workToProcess, cancellationToken) =>
            {
                using var registration = cancellationToken.Register(
                    static state => ((TaskCompletionSource<bool>)state!).SetResult(true),
                    cancellationObserved);

                await workToProcess.TryProcessNextItemAsync(async item => processed.Add(item));

                callbackStarted.SignalAndWait(cancellationToken);
                await cancellationObserved.Task;
                foundItemAfterDispose = await workToProcess.TryProcessNextItemAsync(async item => { });
            },
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        queue.AddWork(1, priority: 0);
        queue.AddWork(2, priority: 1);
        var waitTask = listener.ExpeditedWaitAsync();
        callbackStarted.SignalAndWait();

        queue.Dispose();
        await waitTask;

        Assert.Equal(new[] { 2 }, processed);
        Assert.False(foundItemAfterDispose);
    }

    [Fact]
    public async Task AdditionsAfterDisposeDoNotRun()
    {
        var processed = false;
        var listener = new AsynchronousOperationListener();
        var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.Zero,
            processBatchAsync: async (workToProcess, cancellationToken) => processed = true,
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: listener);

        queue.Dispose();
        queue.AddWork(1, priority: 0);

        await listener.ExpeditedWaitAsync();

        Assert.False(processed);
    }

    [Fact]
    public void DisposeIsIdempotent()
    {
        var queue = new AsyncPriorityWorkQueue<int>(
            maximumPriority: 3,
            delay: TimeSpan.Zero,
            processBatchAsync: async (workToProcess, cancellationToken) => { },
            equalityComparer: EqualityComparer<int>.Default,
            asyncListener: AsynchronousOperationListenerProvider.NullListener);

        queue.Dispose();
        queue.Dispose();
    }

    private static async Task<List<TItem>> DrainAsync<TItem>(AsyncPriorityWorkQueue<TItem>.WorkToProcess workToProcess)
        where TItem : notnull
    {
        var result = new List<TItem>();
        while (await workToProcess.TryProcessNextItemAsync(async item => result.Add(item)))
        {
        }

        return result;
    }
}
