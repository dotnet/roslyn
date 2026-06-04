// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if DEBUG

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class PoolTrackingTests
{
    [Fact]
    public void DetectsLeak()
    {
        PoolTracker.StartTracking(out var context);
        var builder = ArrayBuilder<int>.GetInstance();
        // Intentionally not freeing builder.
        PoolTracker.StopTracking();
        Assert.True(context.HasLeaks);
        Assert.Contains("ArrayBuilder", context.GetLeakSummary());
        builder.Free();
    }

    [Fact]
    public void NoLeakWhenFreed()
    {
        PoolTracker.StartTracking(out var context);
        var builder = ArrayBuilder<int>.GetInstance();
        builder.Free();
        PoolTracker.StopTracking();
        Assert.False(context.HasLeaks);
    }

    [Fact]
    public void ForgiveLeaksClearsOutstanding()
    {
        PoolTracker.StartTracking(out var context);
        var builder = ArrayBuilder<int>.GetInstance();
        // Intentionally not freeing builder.
        PoolTracker.ForgiveLeaks();
        PoolTracker.StopTracking();
        Assert.False(context.HasLeaks);
        builder.Free();
    }

    [Fact]
    public async Task WaitForOutstandingObjectsToBeFreed_SeesAsyncFree()
    {
        PoolTracker.StartTracking(out var context);
        var builder = ArrayBuilder<int>.GetInstance();
        using var allowFree = new SemaphoreSlim(0, 1);
        var task = Task.Run(() =>
        {
            allowFree.Wait();
            builder.Free();
        });

        Assert.True(context.HasLeaks);
        allowFree.Release();
        Assert.True(context.WaitForOutstandingObjectsToBeFreed(TimeSpan.FromSeconds(5)));
        PoolTracker.StopTracking();
        await task;
        Assert.False(context.HasLeaks);
    }

    [Fact]
    public void WaitForOutstandingObjectsToBeFreed_TimesOutForLeak()
    {
        PoolTracker.StartTracking(out var context);
        var builder = ArrayBuilder<int>.GetInstance();

        Assert.False(context.WaitForOutstandingObjectsToBeFreed(TimeSpan.Zero));
        PoolTracker.StopTracking();
        Assert.True(context.HasLeaks);
        builder.Free();
    }

    [Fact]
    public void TrackingFlowsIntoParallelFor()
    {
        PoolTracker.StartTracking(out var context);

        // Allocate and free across multiple parallel iterations.
        // Leak exactly one (at a random index) to verify tracking works across threads.
        int leakedIndex = new Random().Next(100);
        ArrayBuilder<int>? leaked = null;
        Parallel.For(0, 100, i =>
        {
            var builder = ArrayBuilder<int>.GetInstance();
            if (i == leakedIndex)
                leaked = builder;
            else
                builder.Free();
        });

        PoolTracker.StopTracking();
        Assert.True(context.HasLeaks);
        leaked!.Free();
    }

    [Fact]
    public void TrackingFlowsIntoParallelFor_NoLeakWhenFreed()
    {
        PoolTracker.StartTracking(out var context);

        Parallel.For(0, 100, i =>
        {
            var builder = ArrayBuilder<int>.GetInstance();
            builder.Free();
        });

        PoolTracker.StopTracking();
        Assert.False(context.HasLeaks);
    }

    [Fact]
    public void LeakSummaryIncludesPoolName()
    {
        PoolTracker.StartTracking(out var context);
        var builder = ArrayBuilder<int>.GetInstance();
        // Intentionally not freeing builder.
        PoolTracker.StopTracking();
        var summary = context.GetLeakSummary();
        Assert.Contains("ArrayBuilder.cs", summary);
        builder.Free();
    }

    [Fact]
    public void TrackLeaksFalse_NotTracked()
    {
        var pool = new ObjectPool<object>(() => new object(), trackLeaks: false);
        PoolTracker.StartTracking(out var context);
        var obj = pool.Allocate();
        // Intentionally not freeing.
        PoolTracker.StopTracking();
        Assert.False(context.HasLeaks);
        pool.Free(obj);
    }

    [Fact]
    public void DoubleFree_DetectedByAssert()
    {
        // Use a pool with size 1 so the freed object is guaranteed to be stored.
        var pool = new ObjectPool<object>(() => new object(), size: 1);
        var obj = pool.Allocate();
        pool.Free(obj);
        // Second Free should trigger Debug.Assert "freeing twice?"
        var ex = Assert.ThrowsAny<Exception>(() => pool.Free(obj));
        Assert.Contains("freeing twice", ex.Message);
    }
}

#endif
