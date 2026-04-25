// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

public class MemoryCacheTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static string GetNewKey() => Guid.NewGuid().ToString();

    [Fact]
    public async Task ConcurrentSets_DoesNotThrow()
    {
        // Arrange
        var cache = new MemoryCache<string, IReadOnlyList<int>>();
        var entries = Enumerable.Range(0, 500);
        var repeatCount = 4;

        // 1111 2222 3333 4444 ...
        var repeatedEntries = entries.SelectMany(entry => Enumerable.Repeat(entry, repeatCount));
        var tasks = repeatedEntries.Select(async entry =>
        {
            // 2 is an arbitrarily low number, we're just trying to emulate concurrency
            await Task.Delay(2);
            cache.Set(entry.ToString(), value: []);
        });

        // Act & Assert
        await Task.WhenAll(tasks);
    }

    [Fact]
    public void LastAccessTime_IsUpdatedOnGet()
    {
        var cache = new MemoryCache<string, IReadOnlyList<int>>();
        var cacheAccessor = cache.GetTestAccessor();
        var key = GetNewKey();

        cache.Set(key, value: []);
        Assert.True(cacheAccessor.TryGetLastAccess(key, out var oldAccessTime));

        Thread.Sleep(millisecondsTimeout: 10);

        Assert.True(cache.TryGetValue(key, out _));
        Assert.True(cacheAccessor.TryGetLastAccess(key, out var newAccessTime));

        Assert.True(newAccessTime > oldAccessTime, "New AccessTime should be greater than old");
    }

    [Fact]
    public void SetAndGet_WithValidKeyAndValue_ReturnsExpectedValue()
    {
        var cache = new MemoryCache<string, IReadOnlyList<int>>();
        var key = GetNewKey();
        var value = new List<int> { 1, 2, 3 };

        cache.Set(key, value);

        Assert.True(cache.TryGetValue(key, out var result));
        Assert.Same(value, result);
    }

    [Fact]
    public void Compaction_TriggersWhenSizeLimitReached()
    {
        const int SizeLimit = 10;
        var cache = new MemoryCache<string, IReadOnlyList<int>>(SizeLimit);
        var cacheAccessor = cache.GetTestAccessor();

        var wasCompacted = false;
        cacheAccessor.Compacted += () => wasCompacted = true;

        for (var i = 0; i < SizeLimit; i++)
        {
            cache.Set(GetNewKey(), [i]);
            Assert.False(wasCompacted, "It got compacted early.");
        }

        cache.Set(GetNewKey(), [SizeLimit]);
        Assert.True(wasCompacted, "Compaction is not happening");
    }

    [Fact]
    public void TryGetValue_WithMissingKey_ReturnsFalse()
    {
        var cache = new MemoryCache<string, IReadOnlyList<int>>();
        var key = GetNewKey();

        Assert.False(cache.TryGetValue(key, out _));
    }

    [Fact]
    public void NullKey_ThrowsArgumentNullException()
    {
        var cache = new MemoryCache<string, IReadOnlyList<int>>();

        Assert.Throws<ArgumentNullException>(() => cache.TryGetValue(key: null!, out var result));
        Assert.Throws<ArgumentNullException>(() => cache.Set(key: null!, []));
    }

    [Fact]
    public void CompactionWithSizeLimitOne_BehavesCorrectly()
    {
        const int SizeLimit = 1;
        var cache = new MemoryCache<string, IReadOnlyList<int>>(SizeLimit);
        var cacheAccessor = cache.GetTestAccessor();

        var compactionCount = 0;
        cacheAccessor.Compacted += () => compactionCount++;

        // First entry should not trigger compaction
        cache.Set("key1", [1]);
        Assert.Equal(0, compactionCount);

        // Second entry should trigger compaction (removes at least 1 entry)
        cache.Set("key2", [2]);
        Assert.Equal(1, compactionCount);

        // Only one entry should remain
        var keys = new[] { "key1", "key2" };
        Assert.Single(keys.Where(key => cache.TryGetValue(key, out _)));
    }

    [Fact]
    public async Task ConcurrentSetsWithSameKey_LastWriterWins()
    {
        var cache = new MemoryCache<string, IReadOnlyList<int>>();
        const string Key = "same-key";
        const int TaskCount = 100;

        var tasks = Enumerable.Range(0, TaskCount)
            .Select(i => Task.Run(() => cache.Set(Key, [i])))
            .ToArray();

        await Task.WhenAll(tasks);

        // Should have exactly one entry with some value 0-99
        Assert.True(cache.TryGetValue(Key, out var result));
        Assert.Single(result);
        Assert.True(result[0] >= 0 && result[0] < TaskCount);
    }

    [Fact]
    public async Task RapidCompactionTriggers_DoesNotCauseExcessiveCompaction()
    {
        const int SizeLimit = 10;
        var cache = new MemoryCache<string, IReadOnlyList<int>>(SizeLimit);
        var cacheAccessor = cache.GetTestAccessor();

        // Fill to capacity
        for (var i = 0; i < SizeLimit; i++)
        {
            cache.Set($"initial-{i}", [i]);
        }

        var compactionCount = 0;
        cacheAccessor.Compacted += () => Interlocked.Increment(ref compactionCount);

        // Rapidly trigger many compactions
        var tasks = Enumerable.Range(0, 30)
            .Select(i => Task.Run(() => cache.Set($"rapid-{i}", [i])))
            .ToArray();

        await Task.WhenAll(tasks);

        // Compaction should happen, but not excessively due to double-checked locking
        Assert.True(compactionCount > 0);
        Assert.True(compactionCount < 10, $"Too many compactions: {compactionCount}");
    }

    [Fact]
    public async Task AccessingEntryDuringRemoval_IsThreadSafe()
    {
        const int SizeLimit = 3;
        var cache = new MemoryCache<string, IReadOnlyList<int>>(SizeLimit);
        var cacheAccessor = cache.GetTestAccessor();

        // Fill cache
        var keys = new List<string>();
        for (var i = 0; i < SizeLimit; i++)
        {
            var key = $"key-{i}";
            keys.Add(key);
            cache.Set(key, [i]);
        }

        using var compactionStarted = new ManualResetEventSlim();
        using var continueCompaction = new ManualResetEventSlim();

        cacheAccessor.Compacted += () =>
        {
            compactionStarted.Set();
            continueCompaction.Wait(TimeSpan.FromSeconds(1));
        };

        // Start compaction
        var compactionTask = Task.Run(() => cache.Set("trigger", [999]));

        // Wait for compaction to start, then hammer the first key
        compactionStarted.Wait(TimeSpan.FromSeconds(1));

        var accessTask = Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                _ = cache.TryGetValue(keys[0], out _);
            }
        });

        continueCompaction.Set();

        // Should complete without exceptions
        await Task.WhenAll(compactionTask, accessTask);
    }

    [Fact]
    public void LargeValues_DoNotCauseIssues()
    {
        var cache = new MemoryCache<string, IReadOnlyList<int>>();
        var largeValue = Enumerable.Range(0, 100_000).ToList();

        cache.Set("large", largeValue);

        Assert.True(cache.TryGetValue("large", out var result));
        Assert.Same(largeValue, result);
        Assert.Equal(100_000, result.Count);
    }

    [Fact]
    public async Task ConcurrentAccess_DuringCompaction_DoesNotLoseHotEntries()
    {
        const int SizeLimit = 10;
        var cache = new MemoryCache<string, IReadOnlyList<int>>(SizeLimit);
        var cacheAccessor = cache.GetTestAccessor();

        // Fill cache to trigger compaction
        var keys = new List<string>();
        for (var i = 0; i < SizeLimit; i++)
        {
            var key = GetNewKey();
            keys.Add(key);
            cache.Set(key, [i]);
        }

        // Set up compaction monitoring
        using var compactionStarted = new ManualResetEventSlim();
        using var continueCompaction = new ManualResetEventSlim();

        cacheAccessor.Compacted += () =>
        {
            compactionStarted.Set();
            continueCompaction.Wait(); // Block compaction
        };

        // Start compaction in background
        var compactionTask = Task.Run(() => cache.Set("trigger-compaction", [999]));

        // Wait for compaction to start, then access entries concurrently
        compactionStarted.Wait();

        var accessTasks = keys.Select(key => Task.Run(() =>
        {
            for (var i = 0; i < 10; i++)
            {
                _ = cache.TryGetValue(key, out _);
                Thread.Sleep(1); // Small delay to increase race condition chances
            }
        })).ToArray();

        // Allow compaction to complete
        continueCompaction.Set();

        await Task.WhenAll([compactionTask, .. accessTasks]);

        // Verify frequently accessed entries weren't removed
        var survivingCount = keys.Count(key => cache.TryGetValue(key, out _));
        Assert.True(survivingCount > 0, "Some frequently accessed entries should survive compaction");
    }

    [Fact]
    public async Task ConcurrentSets_DuringCompaction_AreThreadSafe()
    {
        const int SizeLimit = 5;
        var cache = new MemoryCache<string, IReadOnlyList<int>>(SizeLimit);

        using var cancellationSource = new CancellationTokenSource();

        // Fill cache to near capacity
        for (var i = 0; i < SizeLimit - 1; i++)
        {
            cache.Set($"initial-{i}", [i]);
        }

        // Start continuous Set operations
        var setTask = Task.Run(async () =>
        {
            var counter = 0;
            try
            {
                while (!cancellationSource.IsCancellationRequested)
                {
                    cache.Set($"concurrent-{counter}", [counter]);
                    counter++;
                    await Task.Delay(1, cancellationSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation token is canceled
            }
        });

        // Trigger compaction
        var compactionTask = Task.Run(() =>
        {
            cache.Set("trigger-compaction", [999]); // This should trigger compaction
        });

        await Task.Delay(50); // Let operations run concurrently
        cancellationSource.Cancel();

        await Task.WhenAll(setTask, compactionTask);

        // Verify cache is still functional
        cache.Set("final-test", [123]);
        Assert.True(cache.TryGetValue("final-test", out var result));
        Assert.Equal([123], result);
    }

    [Fact]
    public async Task LastAccessTime_UnderHighContention_IsReasonablyAccurate()
    {
        var cache = new MemoryCache<string, IReadOnlyList<int>>();
        var cacheAccessor = cache.GetTestAccessor();
        var key = GetNewKey();

        cache.Set(key, [1]);

        var accessTimes = new ConcurrentBag<DateTime>();
        var accessCount = 100;

        // Concurrent access from multiple threads
        var tasks = Enumerable.Range(0, accessCount)
            .Select(x => Task.Run(() =>
            {
                _ = cache.TryGetValue(key, out _);

                if (cacheAccessor.TryGetLastAccess(key, out var accessTime))
                {
                    accessTimes.Add(accessTime);
                }
            }));

        await Task.WhenAll(tasks);

        // Verify we got reasonable access times (no default DateTime values)
        Assert.False(accessTimes.IsEmpty);
        Assert.All(accessTimes, time => Assert.True(time > DateTime.MinValue));

        // Verify the final access time is recent
        Assert.True(cacheAccessor.TryGetLastAccess(key, out var finalTime));
        Assert.True(DateTime.UtcNow - finalTime < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task LRUEviction_WithConcurrentAccess_BehavesReasonably()
    {
        const int SizeLimit = 5;
        var cache = new MemoryCache<string, IReadOnlyList<int>>(SizeLimit);
        var cacheAccessor = cache.GetTestAccessor();

        var hotAccessEstablished = new TaskCompletionSource<bool>();
        var coldEntryAdded = false;

        // Add initial entries
        var initialKeys = new string[SizeLimit];

        for (var i = 0; i < SizeLimit; i++)
        {
            var key = $"initial-{i}";
            initialKeys[i] = key;
            cache.Set(key, [i]);
        }

        // Continuously access first couple entries to make them "hot"
        var hotKeys = initialKeys.Take(2).ToArray();
        var keepHotTask = Task.Run(async () =>
        {
            try
            {
                // Establish hot key access
                for (var i = 0; i < 10; i++)
                {
                    await AccessHotKeysAsync(hotKeys, cache);
                }

                hotAccessEstablished.TrySetResult(true);

                // Continue accessing hot keys while waiting for cold entry to be added
                while (!coldEntryAdded)
                {
                    await AccessHotKeysAsync(hotKeys, cache);
                }

                // Continue accessing hot keys until finished.
                for (var i = 0; i < 100; i++)
                {
                    await AccessHotKeysAsync(hotKeys, cache);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation token is canceled
            }

            async Task AccessHotKeysAsync(string[] hotKeys, MemoryCache<string, IReadOnlyList<int>> cache)
            {
                foreach (var key in hotKeys)
                {
                    _ = cache.TryGetValue(key, out _);
                }

                await Task.Delay(1, DisposalToken);
            }
        });

        // Trigger compaction
        await hotAccessEstablished.Task;
        cache.Set("trigger-compaction", [999]);

        // Signal that the cold entry was added
        coldEntryAdded = true;

        await keepHotTask;

        // Verify hot entries are more likely to survive
        var hotSurvivalCount = hotKeys.Count(key => cache.TryGetValue(key, out _));
        var coldSurvivalCount = initialKeys.Skip(2).Count(key => cache.TryGetValue(key, out _));

        // Hot entries should have better survival rate
        Assert.True(hotSurvivalCount >= coldSurvivalCount,
            $"Hot entries ({hotSurvivalCount}) should survive at least as well as cold entries ({coldSurvivalCount})");
    }

    [Fact]
    public async Task CompactionLocking_PreventsMultipleSimultaneousCompactions()
    {
        const int SizeLimit = 5;
        var cache = new MemoryCache<string, IReadOnlyList<int>>(SizeLimit);
        var cacheAccessor = cache.GetTestAccessor();

        // Fill to capacity
        for (var i = 0; i < SizeLimit; i++)
        {
            cache.Set($"initial-{i}", [i]);
        }

        var compactionCount = 0;
        cacheAccessor.Compacted += () => Interlocked.Increment(ref compactionCount);

        // Trigger multiple compactions simultaneously
        var compactionTasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(() => cache.Set($"trigger-{i}", [i])))
            .ToArray();

        await Task.WhenAll(compactionTasks);

        // Should have compacted at least once, but likely not 10 times due to double-checked locking
        Assert.True(compactionCount >= 1, "At least one compaction should have occurred");
        Assert.True(compactionCount < 10, "Double-checked locking should prevent excessive compactions");
    }

    [Fact]
    public async Task Clear_WhileConcurrentOperations_IsThreadSafe()
    {
        var cache = new MemoryCache<string, IReadOnlyList<int>>();
        var testDuration = TimeSpan.FromMilliseconds(100);
        using var cancellationSource = new CancellationTokenSource(testDuration);

        // Continuous operations
        var tasks = new[]
        {
            // Continuous sets
            Task.Run(async () =>
            {
                var counter = 0;
                try
                {
                    while (!cancellationSource.IsCancellationRequested)
                    {
                        cache.Set($"key-{counter}", [counter]);
                        counter++;
                        await Task.Delay(1, cancellationSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation token times out
                }
            }),

            // Continuous gets
            Task.Run(async () =>
            {
                try
                {
                    while (!cancellationSource.IsCancellationRequested)
                    {
                        _ = cache.TryGetValue("key-0", out _);
                        await Task.Delay(1, cancellationSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation token times out
                }
            }),

            // Periodic clears
            Task.Run(async () =>
            {
                try
                {
                    while (!cancellationSource.IsCancellationRequested)
                    {
                        await Task.Delay(10, cancellationSource.Token);
                        cache.Clear();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation token times out
                }
            })
        };

        // Should complete without unhandled exceptions
        await Task.WhenAll(tasks);

        // Cache should still be functional after all the chaos
        cache.Set("final-test", [123]);
        Assert.True(cache.TryGetValue("final-test", out var result));
        Assert.Equal([123], result);
    }
}
