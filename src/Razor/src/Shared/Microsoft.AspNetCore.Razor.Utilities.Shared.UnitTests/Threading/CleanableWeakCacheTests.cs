// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.Threading;

public class CleanableWeakCacheTests
{
    private record TestKey(string Value);

    private sealed class TestValue(string value)
    {
        public string Value => value;

        public override string ToString() => value;
    }

    private static void ForceGC()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Fact]
    public void Constructor_ValidCleanupThreshold_CreatesInstance()
    {
        var cache = new CleanableWeakCache<string, TestValue>(10);

        Assert.NotNull(cache);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_InvalidCleanupThreshold_ThrowsArgumentOutOfRangeException(int cleanupThreshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CleanableWeakCache<string, TestValue>(cleanupThreshold));
    }

    [Fact]
    public void GetOrAdd_WithValue_NewKey_ReturnsProvidedValue()
    {
        const string Key = "key1";

        var cache = new CleanableWeakCache<string, TestValue>(10);
        var value = new TestValue("test");

        var result = cache.GetOrAdd(Key, value);

        Assert.Same(value, result);
    }

    [Fact]
    public void GetOrAdd_WithValue_ExistingKey_ReturnsExistingValue()
    {
        const string Key = "key1";

        var cache = new CleanableWeakCache<string, TestValue>(10);
        var existingValue = new TestValue("existing");
        var newValue = new TestValue("new");

        cache.GetOrAdd(Key, existingValue);

        var result = cache.GetOrAdd(Key, newValue);

        Assert.Same(existingValue, result);
        Assert.NotSame(newValue, result);
    }

    [Fact]
    public void GetOrAdd_WithFactory_NewKey_ReturnsFactoryCreatedValue()
    {
        const string Key = "key1";

        var cache = new CleanableWeakCache<string, TestValue>(10);
        var expectedValue = new TestValue("factory");

        var result = cache.GetOrAdd(Key, () => expectedValue);

        Assert.Same(expectedValue, result);
    }

    [Fact]
    public void GetOrAdd_WithFactory_ExistingKey_ReturnsExistingValueWithoutCallingFactory()
    {
        const string Key = "key1";

        var cache = new CleanableWeakCache<string, TestValue>(10);
        var existingValue = new TestValue("existing");
        var factoryCalled = false;

        cache.GetOrAdd(Key, existingValue);

        var result = cache.GetOrAdd(Key, () =>
        {
            factoryCalled = true;
            return new TestValue("factory");
        });

        Assert.Same(existingValue, result);
        Assert.False(factoryCalled);
    }

    [Fact]
    public void GetOrAdd_WithArgAndFactory_NewKey_ReturnsFactoryCreatedValue()
    {
        const string Key = "key1";
        const string Arg = "factory-arg";

        var cache = new CleanableWeakCache<string, TestValue>(10);

        var result = cache.GetOrAdd(Key, Arg, argument => new TestValue(argument));

        Assert.Equal(Arg, result.Value);
    }

    [Fact]
    public void GetOrAdd_WithArgAndFactory_ExistingKey_ReturnsExistingValueWithoutCallingFactory()
    {
        const string Key = "key1";
        const string Arg = "factory-arg";

        var cache = new CleanableWeakCache<string, TestValue>(10);
        var existingValue = new TestValue("existing");
        var factoryCalled = false;

        cache.GetOrAdd(Key, existingValue);

        var result = cache.GetOrAdd(Key, Arg, argument =>
        {
            factoryCalled = true;
            return new TestValue(argument);
        });

        Assert.Same(existingValue, result);
        Assert.False(factoryCalled);
    }

    [Fact]
    public void TryAdd_NewKey_ReturnsTrue()
    {
        const string Key = "key1";

        var cache = new CleanableWeakCache<string, TestValue>(10);
        var value = new TestValue("test");

        var result = cache.TryAdd(Key, value);

        Assert.True(result);
    }

    [Fact]
    public void TryAdd_ExistingKey_ReturnsFalse()
    {
        const string Key = "key1";

        var cache = new CleanableWeakCache<string, TestValue>(10);
        var existingValue = new TestValue("existing");
        var newValue = new TestValue("new");

        cache.TryAdd(Key, existingValue);

        var result = cache.TryAdd(Key, newValue);

        Assert.False(result);
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue()
    {
        const string Key = "key1";

        var cache = new CleanableWeakCache<string, TestValue>(10);
        var value = new TestValue("test");

        cache.TryAdd(Key, value);

        Assert.True(cache.TryGet(Key, out var retrievedValue));
        Assert.Same(value, retrievedValue);
    }

    [Fact]
    public void TryGet_NonExistentKey_ReturnsFalseAndNull()
    {
        var cache = new CleanableWeakCache<string, TestValue>(10);

        Assert.False(cache.TryGet("nonexistent", out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Cache_HandlesGarbageCollectedValues()
    {
        const string Key = "key1";

        var cache = new CleanableWeakCache<string, TestValue>(10);

        // Add a value that will be garbage collected - use local static method to ensure object goes out of scope
        AddTemporaryValue(cache, Key);

        ForceGC();

        // Try to get the value after GC
        Assert.False(cache.TryGet(Key, out var value));
        Assert.Null(value);

        static void AddTemporaryValue(CleanableWeakCache<string, TestValue> cache, string key)
        {
            // This ensures the TestValue object goes out of scope after this method returns
            cache.TryAdd(key, new TestValue("temporary"));
        }
    }

    [Fact]
    public void Cache_ReplacesGarbageCollectedValue_WithNewValue()
    {
        const string Key = "key1";

        var cache = new CleanableWeakCache<string, TestValue>(10);

        // Add a value that will be garbage collected - use local static method to ensure object goes out of scope
        AddTemporaryValue(cache, Key);

        ForceGC();

        var newValue = new TestValue("new");

        // Add a new value for the same key
        var result = cache.GetOrAdd(Key, newValue);

        Assert.Same(newValue, result);

        static void AddTemporaryValue(CleanableWeakCache<string, TestValue> cache, string key)
        {
            // This ensures the TestValue object goes out of scope after this method returns
            cache.TryAdd(key, new TestValue("temporary"));
        }
    }

    [Fact]
    public void Cache_PerformsCleanupAtThreshold()
    {
        const string Key1 = "key1";
        const string Key2 = "key2";
        const string Key3 = "key3";
        const string Key4 = "key4";

        var cache = new CleanableWeakCache<string, TestValue>(3);

        // Add values that will be garbage collected - use local static method to ensure objects go out of scope
        AddTemporaryValues(cache, Key1, Key2);

        ForceGC();

        // Keep a strong reference to this value
        var persistentValue = new TestValue("persistent");
        cache.TryAdd(Key3, persistentValue);

        // This should trigger cleanup (3rd add operation)
        var newValue = new TestValue("new");
        cache.TryAdd(Key4, newValue);

        // Verify the dead references were cleaned up
        Assert.False(cache.TryGet(Key1, out _));
        Assert.False(cache.TryGet(Key2, out _));
        Assert.True(cache.TryGet(Key3, out var key3Value));
        Assert.Same(persistentValue, key3Value);
        Assert.True(cache.TryGet(Key4, out var key4Value));
        Assert.Same(newValue, key4Value);

        static void AddTemporaryValues(CleanableWeakCache<string, TestValue> cache, string key1, string key2)
        {
            // These objects will go out of scope after this method returns
            cache.TryAdd(key1, new TestValue("temp1"));
            cache.TryAdd(key2, new TestValue("temp2"));
        }
    }

    [Fact]
    public async Task Cache_ThreadSafety_ConcurrentAccess()
    {
        const int CleanUpThreshold = 1000;
        const int IterationsPerTask = 100;

        var cache = new CleanableWeakCache<int, TestValue>(CleanUpThreshold);
        var taskCount = Environment.ProcessorCount * 2;
        var tasks = new Task[taskCount];

        for (var i = 0; i < taskCount; i++)
        {
            var taskId = i;
            tasks[i] = Task.Run(() =>
            {
                for (var j = 0; j < IterationsPerTask; j++)
                {
                    var key = (taskId * IterationsPerTask) + j;
                    var value = new TestValue($"Task{taskId}-Value{j}");

                    // Perform various operations concurrently
                    cache.TryAdd(key, value);
                    cache.TryGet(key, out _);
                    cache.GetOrAdd(key, () => new TestValue("Factory"));
                    cache.GetOrAdd(key, "arg", arg => new TestValue(arg));
                }
            });
        }

        await Task.WhenAll(tasks);

        // No deadlocks or exceptions should occur
        // Verify that we can still access the cache
        var testKey = 0;
        var testValue = new TestValue("test");
        var result = cache.GetOrAdd(testKey, testValue);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Cache_ThreadSafety_ConcurrentFactoryExecution()
    {
        const string Key = "shared-key";

        var cache = new CleanableWeakCache<string, TestValue>(100);
        var taskCount = Environment.ProcessorCount * 2;
        var tasks = new Task<TestValue>[taskCount];

        // Multiple threads trying to create the same value
        for (var i = 0; i < taskCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                return cache.GetOrAdd(Key, () =>
                {
                    Thread.Sleep(10); // Simulate some work
                    return new TestValue("shared-value");
                });
            });
        }

        var results = await Task.WhenAll(tasks);

        // All results should be the same instance (the important guarantee)
        var firstResult = results[0];
        Assert.NotNull(firstResult);
        Assert.Equal("shared-value", firstResult.Value);
        
        for (var i = 1; i < results.Length; i++)
        {
            Assert.Same(firstResult, results[i]);
        }

        // Verify the value is properly cached
        Assert.True(cache.TryGet(Key, out var cachedValue));
        Assert.Same(firstResult, cachedValue);
    }

    [Fact]
    public void Cache_HandlesNullKeys_ThrowsException()
    {
        var cache = new CleanableWeakCache<string, TestValue>(10);
        var value = new TestValue("test");

        Assert.Throws<ArgumentNullException>(() => cache.GetOrAdd(null!, value));
        Assert.Throws<ArgumentNullException>(() => cache.GetOrAdd(null!, () => value));
        Assert.Throws<ArgumentNullException>(() => cache.GetOrAdd(null!, "arg", _ => value));
        Assert.Throws<ArgumentNullException>(() => cache.TryAdd(null!, value));
        Assert.Throws<ArgumentNullException>(() => cache.TryGet(null!, out _));
    }

    [Fact]
    public void Cache_HandlesNullValues()
    {
        var cache = new CleanableWeakCache<string, TestValue?>(10);

        // These should work without throwing (null values are allowed for reference types)
        var result1 = cache.GetOrAdd("key1", (TestValue?)null);
        var result2 = cache.GetOrAdd("key2", () => null);
        var result3 = cache.GetOrAdd("key3", "arg", _ => null);
        var added = cache.TryAdd("key4", null);

        Assert.Null(result1);
        Assert.Null(result2);
        Assert.Null(result3);
        Assert.True(added);
    }

    [Fact]
    public void Cache_DifferentKeyTypes_WorkCorrectly()
    {
        // Test with int keys
        var intCache = new CleanableWeakCache<int, TestValue>(10);
        var intValue = new TestValue("int-value");
        intCache.TryAdd(42, intValue);
        Assert.True(intCache.TryGet(42, out var retrievedIntValue));
        Assert.Same(intValue, retrievedIntValue);

        // Test with custom object keys
        var record = new TestKey("test");
        var recordCache = new CleanableWeakCache<TestKey, TestValue>(10);
        var recordValue = new TestValue("record-value");
        recordCache.TryAdd(record, recordValue);
        Assert.True(recordCache.TryGet(record, out var retrievedRecordValue));
        Assert.Same(recordValue, retrievedRecordValue);
    }

    [Fact]
    public void Cache_LargeNumberOfItems_PerformsWell()
    {
        const int CleanUpThreshold = 1000;
        const int ItemCount = 10000;

        var cache = new CleanableWeakCache<int, TestValue>(CleanUpThreshold);
        var values = new TestValue[ItemCount];

        // Add many items
        for (var i = 0; i < ItemCount; i++)
        {
            values[i] = new TestValue($"Value{i}");
            cache.TryAdd(i, values[i]);
        }

        // Verify all items can be retrieved
        for (var i = 0; i < ItemCount; i++)
        {
            Assert.True(cache.TryGet(i, out var value));
            Assert.Same(values[i], value);
        }
    }

    [Fact]
    public void GetOrAdd_Factory_ExceptionInFactory_PropagatesException()
    {
        const string Key = "key1";

        var cache = new CleanableWeakCache<string, TestValue>(10);
        var expectedException = new InvalidOperationException("Test exception");

        var actualException = Assert.Throws<InvalidOperationException>(() =>
            cache.GetOrAdd(Key, () => throw expectedException));

        Assert.Same(expectedException, actualException);
    }

    [Fact]
    public void GetOrAdd_FactoryWithArg_ExceptionInFactory_PropagatesException()
    {
        const string Key = "key1";
        const string Arg = "arg";

        var cache = new CleanableWeakCache<string, TestValue>(10);
        var expectedException = new InvalidOperationException("Test exception");

        var actualException = Assert.Throws<InvalidOperationException>(() =>
            cache.GetOrAdd(Key, Arg, _ => throw expectedException));

        Assert.Same(expectedException, actualException);
    }
}
