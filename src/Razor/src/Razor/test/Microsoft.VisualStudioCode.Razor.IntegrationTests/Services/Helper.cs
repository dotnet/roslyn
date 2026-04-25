// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.VisualStudioCode.Razor.IntegrationTests.Services;

/// <summary>
/// Static helper methods for integration tests.
/// </summary>
public static class Helper
{
    // Default polling interval for condition-based waits
    private const int DefaultPollIntervalMs = 100;

    /// <summary>
    /// Polls for a condition to become true, with exponential backoff.
    /// This replaces arbitrary Task.Delay() calls with smart condition-based waiting.
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve and check.</typeparam>
    /// <param name="getValue">Function to retrieve the current value.</param>
    /// <param name="condition">Predicate that returns true when the condition is met.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="initialDelayMs">Initial delay between polls (will increase with backoff).</param>
    /// <param name="callerName">The name of the calling method (automatically populated).</param>
    /// <returns>The value that satisfied the condition.</returns>
    /// <exception cref="TimeoutException">Thrown if the condition is not met within the timeout.</exception>
    public static async Task<T> WaitForConditionAsync<T>(
        Func<Task<T>> getValue,
        Func<T, bool> condition,
        TimeSpan timeout,
        int initialDelayMs = DefaultPollIntervalMs,
        [CallerMemberName] string? callerName = null)
    {
        var deadline = DateTime.UtcNow + timeout;
        var delayMs = initialDelayMs;
        var maxDelayMs = 1000; // Cap backoff at 1 second

        while (DateTime.UtcNow < deadline)
        {
            var value = await getValue();
            if (condition(value))
            {
                return value;
            }

            await Task.Delay(delayMs);
            delayMs = Math.Min(delayMs * 2, maxDelayMs); // Exponential backoff
        }

        throw new TimeoutException($"Condition {callerName} not met within {timeout.TotalSeconds} seconds");
    }

    /// <summary>
    /// Polls for a condition to become true, with exponential backoff.
    /// Overload for simple boolean conditions.
    /// </summary>
    public static async Task WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        int initialDelayMs = DefaultPollIntervalMs)
    {
        await WaitForConditionAsync(condition, result => result, timeout, initialDelayMs);
    }
}
