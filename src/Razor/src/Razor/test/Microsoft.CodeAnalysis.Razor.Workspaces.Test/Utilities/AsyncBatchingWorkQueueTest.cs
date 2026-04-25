// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

public class AsyncBatchingWorkQueueTest(ITestOutputHelper output) : ToolingTestBase(output)
{
    [Fact]
    public async Task AddItemsAndWaitForBatch()
    {
        var list = new List<int>();

        var workQueue = new AsyncBatchingWorkQueue<int>(
            delay: TimeSpan.FromMilliseconds(1),
            processBatchAsync: (items, cancellationToken) =>
            {
                foreach (var item in items)
                {
                    list.Add(item);
                }

                return default;
            },
            DisposalToken);

        for (var i = 0; i < 1000; i++)
        {
            workQueue.AddWork(i);
        }

        await workQueue.WaitUntilCurrentBatchCompletesAsync();

        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(i, list[i]);
        }
    }

    [Fact]
    public async Task DedupesItems()
    {
        var uniqueItems = new HashSet<int>();

        var workQueue = new AsyncBatchingWorkQueue<int>(
            delay: TimeSpan.FromMilliseconds(1),
            processBatchAsync: (items, cancellationToken) =>
            {
                // Verify that items doesn't contain any duplicates.
                // We use a local set to verify the items that were
                // passed to us, since there could be duplicates
                // across batches.
                var set = new HashSet<int>();

                foreach (var item in items)
                {
                    Assert.True(set.Add(item));

                    // Add to the final set that we'll check at the very end.
                    uniqueItems.Add(item);
                }

                return default;
            },
            equalityComparer: EqualityComparer<int>.Default,
            DisposalToken);

        for (var i = 0; i < 1000; i++)
        {
            workQueue.AddWork(i);
            workQueue.AddWork(i);
        }

        await workQueue.WaitUntilCurrentBatchCompletesAsync();

        for (var i = 0; i < 1000; i++)
        {
            Assert.Contains(i, uniqueItems);
        }
    }

    [Fact]
    public async Task CancelExistingWork()
    {
        var cancelled = 0;
        var batchStarted = 0;
        var done = 0;

        var workQueue = new AsyncBatchingWorkQueue(
            TimeSpan.FromMilliseconds(1),
            cancellationToken =>
            {
                // If the batch was already started once, bail.
                if (batchStarted == 1)
                {
                    return default;
                }

                batchStarted++;

                while (true) // infinite loop
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancelled++;
                        break;
                    }
                }

                done++;

                return default;
            },
            DisposalToken);

        // Add first bit of work
        workQueue.AddWork();

        // Wait until the batch is started.
        while (batchStarted == 0)
        {
            await Task.Delay(10);
        }

        // Add another bit of work, cancelling the previous work.
        workQueue.AddWork(cancelExistingWork: true);

        // Wait until the batch finishes.
        while (done == 0)
        {
            await Task.Delay(10);
        }

        // Assert that the batch was cancelled.
        Assert.Equal(1, batchStarted);
        Assert.Equal(1, cancelled);
        Assert.Equal(1, done);
    }
}
