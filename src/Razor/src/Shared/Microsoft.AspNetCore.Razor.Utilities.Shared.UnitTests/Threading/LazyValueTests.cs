// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.Threading;

public class LazyValueTests
{
    [Fact]
    public void LazyValue_GetValue_CallsFactoryOnce()
    {
        // Arrange
        var callCount = 0;
        var lazy = new LazyValue<string>(() =>
        {
            Interlocked.Increment(ref callCount);
            return "test-value";
        });

        // Act
        var result1 = lazy.GetValue();
        var result2 = lazy.GetValue();
        var result3 = lazy.GetValue();

        // Assert
        Assert.Equal("test-value", result1);
        Assert.Equal("test-value", result2);
        Assert.Equal("test-value", result3);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void LazyValue_GetValue_ReturnsFactoryResult()
    {
        // Arrange
        var expectedValue = new object();
        var lazy = new LazyValue<object>(() => expectedValue);

        // Act
        var result = lazy.GetValue();

        // Assert
        Assert.Same(expectedValue, result);
    }

    [Fact]
    public void LazyValue_GetValue_WorksWithValueTypes()
    {
        // Arrange
        var lazy = new LazyValue<int>(() => 42);

        // Act
        var result = lazy.GetValue();

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void LazyValue_GetValue_WorksWithNullValues()
    {
        // Arrange
        var lazy = new LazyValue<string?>(() => null);

        // Act
        var result = lazy.GetValue();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void LazyValue_GetValue_PropagatesFactoryException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var lazy = new LazyValue<string>(() => throw expectedException);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => lazy.GetValue());
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public void LazyValue_GetValue_ExceptionDoesNotCacheValue()
    {
        // Arrange
        var callCount = 0;
        var lazy = new LazyValue<string>(() =>
        {
            var count = Interlocked.Increment(ref callCount);
            if (count == 1)
            {
                throw new InvalidOperationException("First call fails");
            }

            return "success";
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => lazy.GetValue());
        
        // Second call should succeed and call factory again
        var result = lazy.GetValue();
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task LazyValue_ConcurrentAccess_CallsFactoryOnce()
    {
        // Arrange
        var callCount = 0;
        using var barrier = new Barrier(10);
        var lazy = new LazyValue<string>(() =>
        {
            Interlocked.Increment(ref callCount);
            Thread.Sleep(10); // Simulate some work
            return "concurrent-value";
        });

        var results = new ConcurrentBag<string>();
        var tasks = new Task[10];

        // Act
        for (var i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait(); // Ensure all threads start at the same time
                var result = lazy.GetValue();
                results.Add(result);
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, callCount);
        Assert.Equal(10, results.Count);
        Assert.All(results, result => Assert.Equal("concurrent-value", result));
    }

    [Fact]
    public async Task LazyValue_StressTest_MaintainsConsistency()
    {
        // Arrange
        const int ThreadCount = 100;
        const int IterationsPerThread = 100;
        var callCount = 0;
        var lazy = new LazyValue<int>(() => Interlocked.Increment(ref callCount));
        var allResults = new ConcurrentBag<int>();

        // Act
        var tasks = new Task[ThreadCount];
        for (var i = 0; i < ThreadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (var j = 0; j < IterationsPerThread; j++)
                {
                    allResults.Add(lazy.GetValue());
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, callCount);
        Assert.Equal(ThreadCount * IterationsPerThread, allResults.Count);
        Assert.All(allResults, result => Assert.Equal(1, result));
    }

    [Fact]
    public void LazyValueWithArg_GetValue_CallsFactoryOnce()
    {
        // Arrange
        var callCount = 0;
        var lazy = new LazyValue<string, string>(arg =>
        {
            Interlocked.Increment(ref callCount);
            return $"processed-{arg}";
        });

        // Act
        var result1 = lazy.GetValue("input");
        var result2 = lazy.GetValue("different-input"); // Should ignore this arg
        var result3 = lazy.GetValue("another-input");   // Should ignore this arg too

        // Assert
        Assert.Equal("processed-input", result1);
        Assert.Equal("processed-input", result2);
        Assert.Equal("processed-input", result3);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void LazyValueWithArg_GetValue_ReturnsFactoryResult()
    {
        // Arrange
        var inputArg = new object();
        var expectedResult = new object();
        var lazy = new LazyValue<object, object>(arg =>
        {
            Assert.Same(inputArg, arg);
            return expectedResult;
        });

        // Act
        var result = lazy.GetValue(inputArg);

        // Assert
        Assert.Same(expectedResult, result);
    }

    [Fact]
    public void LazyValueWithArg_GetValue_WorksWithValueTypes()
    {
        // Arrange
        var lazy = new LazyValue<int, string>(multiplier => $"Value: {multiplier * 10}");

        // Act
        var result = lazy.GetValue(5);

        // Assert
        Assert.Equal("Value: 50", result);
    }

    [Fact]
    public void LazyValueWithArg_GetValue_WorksWithNullArg()
    {
        // Arrange
        var lazy = new LazyValue<string?, string>(arg => $"Input was: {arg ?? "null"}");

        // Act
        var result = lazy.GetValue(null);

        // Assert
        Assert.Equal("Input was: null", result);
    }

    [Fact]
    public void LazyValueWithArg_GetValue_WorksWithNullResult()
    {
        // Arrange
        var lazy = new LazyValue<string, string?>(arg => null);

        // Act
        var result = lazy.GetValue("test");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void LazyValueWithArg_GetValue_PropagatesFactoryException()
    {
        // Arrange
        var expectedException = new ArgumentException("Test exception");
        var lazy = new LazyValue<string, int>(arg => throw expectedException);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => lazy.GetValue("test"));
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public void LazyValueWithArg_GetValue_ExceptionDoesNotCacheValue()
    {
        // Arrange
        var callCount = 0;
        var lazy = new LazyValue<string, string>(arg =>
        {
            var count = Interlocked.Increment(ref callCount);
            if (count == 1)
            {
                throw new InvalidOperationException("First call fails");
            }

            return $"success-{arg}";
        });

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => lazy.GetValue("test"));
        
        // Second call should succeed and call factory again
        var result = lazy.GetValue("test");
        Assert.Equal("success-test", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task LazyValueWithArg_ConcurrentAccess_CallsFactoryOnce()
    {
        // Arrange
        var callCount = 0;
        using var barrier = new Barrier(10);
        var lazy = new LazyValue<string, string>(arg =>
        {
            Interlocked.Increment(ref callCount);
            Thread.Sleep(10); // Simulate some work
            return $"concurrent-{arg}";
        });

        var results = new ConcurrentBag<string>();
        var tasks = new Task[10];

        // Act
        for (var i = 0; i < 10; i++)
        {
            var threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait(); // Ensure all threads start at the same time
                // Each thread passes a different argument, but only the first should be used
                var result = lazy.GetValue($"input-{threadIndex}");
                results.Add(result);
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, callCount);
        Assert.Equal(10, results.Count);
        // All results should be the same, using the argument from whichever thread won the race
        var expectedPrefix = "concurrent-input-";
        Assert.All(results, result => Assert.StartsWith(expectedPrefix, result));
        
        // All results should be identical
        var firstResult = results.First();
        Assert.All(results, result => Assert.Equal(firstResult, result));
    }

    [Fact]
    public void LazyValueWithArg_ExceptionRecovery()
    {
        var callCount = 0;
        var lazy = new LazyValue<int, string>(arg =>
        {
            var count = Interlocked.Increment(ref callCount);
            if (count == 1)
            {
                throw new InvalidOperationException($"First call fails with arg: {arg}");
            }

            return $"success-{arg}";
        });

        // First call fails
        Assert.Throws<InvalidOperationException>(() => lazy.GetValue(100));

        // Second call succeeds
        var result = lazy.GetValue(200);
        Assert.Equal("success-200", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void LazyValueWithArg_SubsequentCallsIgnoreArgument()
    {
        // Arrange
        var receivedArgs = new ConcurrentBag<string>();
        var lazy = new LazyValue<string, string>(arg =>
        {
            receivedArgs.Add(arg);
            return $"result-{arg}";
        });

        // Act
        var result1 = lazy.GetValue("first");
        var result2 = lazy.GetValue("second");
        var result3 = lazy.GetValue("third");

        // Assert
        Assert.Equal("result-first", result1);
        Assert.Equal("result-first", result2); // Same result, ignores "second"
        Assert.Equal("result-first", result3); // Same result, ignores "third"
        Assert.Single(receivedArgs);
        Assert.Equal("first", receivedArgs.First());
    }

    [Fact]
    public async Task LazyValueWithArg_StressTest_MaintainsConsistency()
    {
        // Arrange
        const int ThreadCount = 50;
        const int IterationsPerThread = 50;
        var callCount = 0;
        var lazy = new LazyValue<int, string>(multiplier =>
        {
            Interlocked.Increment(ref callCount);
            return $"computed-{multiplier * 2}";
        });
        var allResults = new ConcurrentBag<string>();

        // Act
        var tasks = new Task[ThreadCount];
        for (var i = 0; i < ThreadCount; i++)
        {
            var threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                for (var j = 0; j < IterationsPerThread; j++)
                {
                    // Each thread uses a different argument, but only the first should matter
                    allResults.Add(lazy.GetValue(threadIndex + j));
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, callCount);
        Assert.Equal(ThreadCount * IterationsPerThread, allResults.Count);
        
        // All results should be identical (from the winning thread's argument)
        var firstResult = allResults.First();
        Assert.All(allResults, result => Assert.Equal(firstResult, result));
        Assert.StartsWith("computed-", firstResult);
    }

    [Fact]
    public void LazyValue_FactoryReturningLargeObject_WorksCorrectly()
    {
        // Arrange
        var lazy = new LazyValue<byte[]>(() => new byte[1024 * 1024]); // 1MB array

        // Act
        var result1 = lazy.GetValue();
        var result2 = lazy.GetValue();

        // Assert
        Assert.Same(result1, result2); // Should be the exact same instance
        Assert.Equal(1024 * 1024, result1.Length);
    }

    [Fact]
    public void LazyValueWithArg_ComplexArgumentType_WorksCorrectly()
    {
        // Arrange
        var complexArg = new { Name = "Test", Count = 42 };
        var lazy = new LazyValue<object, string>(arg => $"Processed: {arg}");

        // Act
        var result = lazy.GetValue(complexArg);

        // Assert
        Assert.Contains("Test", result);
        Assert.Contains("42", result);
        Assert.StartsWith("Processed: { Name = Test, Count = 42 }", result);
    }

    [Fact]
    public void LazyValue_FactoryAccessingClosureVariable_WorksCorrectly()
    {
        // Arrange
        var capturedValue = "captured";
        var lazy = new LazyValue<string>(() => $"Factory with {capturedValue}");

        // Act
        var result = lazy.GetValue();

        // Assert
        Assert.Equal("Factory with captured", result);
    }

    [Fact]
    public void LazyValueWithArg_StaticMethodFactory_AvoidsClosure()
    {
        // Arrange - This test demonstrates the closure-avoiding pattern
        var lazy = new LazyValue<int, string>(StaticFactoryMethod);

        // Act
        var result = lazy.GetValue(42);

        // Assert
        Assert.Equal("Static: 42", result);

        static string StaticFactoryMethod(int value)
        {
            return $"Static: {value}";
        }
    }
}
