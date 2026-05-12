// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Utilities;

/// <summary>
///  A thread-safe cache implementation that uses weak references to store values, allowing them to be garbage collected
///  when no longer referenced elsewhere. The cache periodically cleans up dead weak references to prevent unbounded growth.
/// </summary>
/// <typeparam name="TKey">The type of keys used to identify cached values. Must be non-null.</typeparam>
/// <typeparam name="TValue">The type of values stored in the cache. Must be a reference type.</typeparam>
/// <remarks>
///  This cache is designed for scenarios where you want to cache expensive-to-create objects but allow them to be
///  garbage collected when memory pressure occurs. The cache will automatically clean up dead references when the
///  number of add operations reaches the specified cleanup threshold.
/// </remarks>
internal class CleanableWeakCache<TKey, TValue>
    where TKey : notnull
    where TValue : class?
{
    /// <summary>
    ///  The underlying dictionary that maps keys to weak references containing the cached values.
    /// </summary>
    private readonly Dictionary<TKey, WeakReference<TValue>> _cacheMap = [];

    /// <summary>
    ///  Synchronization object to ensure thread-safe access to the cache.
    /// </summary>
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    /// <summary>
    ///  The number of add operations that must occur before triggering a cleanup of dead weak references.
    /// </summary>
    private readonly int _cleanUpThreshold;

    /// <summary>
    ///  Counter tracking the number of add operations since the last cleanup was performed.
    /// </summary>
    private int _addsSinceLastCleanUp;

    /// <summary>
    ///  Initializes a new instance of the <see cref="CleanableWeakCache{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="cleanUpThreshold">
    ///  The number of add operations that must occur before triggering automatic cleanup of dead weak references.
    ///  Must be positive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  Thrown when <paramref name="cleanUpThreshold"/> is zero or negative.
    /// </exception>
    public CleanableWeakCache(int cleanUpThreshold)
    {
        ArgHelper.ThrowIfNegativeOrZero(cleanUpThreshold);

        _cleanUpThreshold = cleanUpThreshold;
    }

    /// <summary>
    ///  Gets the value associated with the specified key, or adds the provided value if the key is not found
    ///  or the previously cached value has been garbage collected.
    /// </summary>
    /// <param name="key">The key of the value to get or add.</param>
    /// <param name="value">The value to add if the key is not found or the cached value is no longer available.</param>
    /// <returns>
    ///  The existing value if found and still alive, otherwise the provided <paramref name="value"/>.
    /// </returns>
    public TValue GetOrAdd(TKey key, TValue value)
    {
        lock (_lock)
        {
            // Try to get the existing value or add the new one.
            return GetOrAdd_NoLock(key, value);
        }
    }

    /// <summary>
    ///  Gets the value associated with the specified key, or adds a new value created by the factory function
    ///  if the key is not found or the previously cached value has been garbage collected.
    /// </summary>
    /// <param name="key">The key of the value to get or add.</param>
    /// <param name="valueFactory">A factory function to create the value if it needs to be added to the cache.</param>
    /// <returns>
    ///  The existing value if found and still alive, otherwise a new value created by <paramref name="valueFactory"/>.
    /// </returns>
    public TValue GetOrAdd(TKey key, Func<TValue> valueFactory)
    {
        // First check without creating the value.
        if (TryGet(key, out var value))
        {
            return value;
        }

        // Create the value outside the lock to avoid holding the lock
        // while creating a potentially expensive object.
        var newValue = valueFactory();

        lock (_lock)
        {
            // Try to add the newly-created value or get the existing one.
            return GetOrAdd_NoLock(key, newValue);
        }
    }

    /// <summary>
    ///  Gets the value associated with the specified key, or adds a new value created by the factory function
    ///  using the provided argument if the key is not found or the previously cached value has been garbage collected.
    /// </summary>
    /// <typeparam name="TArg">The type of the argument passed to the value factory function.</typeparam>
    /// <param name="key">The key of the value to get or add.</param>
    /// <param name="arg">The argument to pass to the value factory function.</param>
    /// <param name="valueFactory">A factory function to create the value using the provided argument.</param>
    /// <returns>
    ///  The existing value if found and still alive, otherwise a new value created by <paramref name="valueFactory"/>.
    /// </returns>
    public TValue GetOrAdd<TArg>(TKey key, TArg arg, Func<TArg, TValue> valueFactory)
    {
        // First check without creating the value.
        if (TryGet(key, out var value))
        {
            return value;
        }

        // Create the value outside the lock to avoid holding the lock
        // while creating a potentially expensive object.
        var newValue = valueFactory(arg);

        lock (_lock)
        {
            // Try to add the newly-created value or get the existing one.
            return GetOrAdd_NoLock(key, newValue);
        }
    }

    /// <summary>
    ///  Attempts to add the specified key-value pair to the cache.
    /// </summary>
    /// <param name="key">The key of the value to add.</param>
    /// <param name="value">The value to add to the cache.</param>
    /// <returns>
    ///  <see langword="true"/> if the key-value pair was successfully added;
    ///  <see langword="false"/> if a live value already exists for the specified key.
    /// </returns>
    public bool TryAdd(TKey key, TValue value)
    {
        lock (_lock)
        {
            // Check if the key exists and the weak reference still has a live target
            if (!_cacheMap.TryGetValue(key, out var weakRef))
            {
                // The key is not in the map. Add a new weak reference.
                _cacheMap.Add(key, new(value));
            }
            else
            {
                // We have a weak reference, check if it is still alive
                if (weakRef.TryGetTarget(out var existingValue))
                {
                    // Yup, we have a live value for this key.
                    // However, we aren't returning it to the caller, so we should keep it alive
                    // until after we return to ensure the existence check remains valid.
                    GC.KeepAlive(existingValue);
                    return false;
                }

                // Set the weak ref's target to the new value.
                weakRef.SetTarget(value);
            }

            // We added an item to the cache.
            // Increment the add counter and trigger cleanup if needed.
            CleanUpIfNeeded_NoLock();
            return true;
        }
    }

    /// <summary>
    ///  Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="value">
    ///  When this method returns, contains the value associated with the specified key if found and still alive;
    ///  otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if a live value was found for the specified key; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        lock (_lock)
        {
            // Check if the key exists and the weak reference still has a live target
            if (_cacheMap.TryGetValue(key, out var weakRef) &&
                weakRef.TryGetTarget(out value))
            {
                return true;
            }

            // Key not found or target was garbage collected
            value = null;
            return false;
        }
    }

    /// <summary>
    ///  Internal method that attempts to retrieve an existing value from the cache or add a new value if none exists.
    ///  This method assumes the caller already holds the lock.
    /// </summary>
    /// <param name="key">The key of the value to get or add.</param>
    /// <param name="value">The value to add if no existing live value is found.</param>
    /// <returns>
    ///  The existing live value if one was found; otherwise, the provided <paramref name="value"/> after adding it to the cache.
    /// </returns>
    /// <remarks>
    ///  This method increments the add counter and triggers cleanup if the threshold is reached when adding a value.
    /// </remarks>
    private TValue GetOrAdd_NoLock(TKey key, TValue value)
    {
        if (_cacheMap.TryGetValue(key, out var weakRef))
        {
            if (weakRef.TryGetTarget(out var existingValue))
            {
                // There was already a value in the map. Return it!
                return existingValue;
            }

            // The key was in the map, but the weak reference was collected.
            // Set its target to the new value.
            weakRef.SetTarget(value);
        }
        else
        {
            // The key is not in the map. Add a new weak reference.
            _cacheMap.Add(key, new(value));
        }

        // We added an item to the cache.
        // Increment the add counter and trigger cleanup if needed.
        CleanUpIfNeeded_NoLock();
        return value;
    }

    /// <summary>
    ///  Increments the add counter and removes all cache entries whose weak references no longer have live targets 
    ///  if the cleanup threshold has been reached. This method assumes the caller already holds the lock.
    /// </summary>
    /// <remarks>
    ///  This method resets the add counter to zero after cleanup is performed.
    /// </remarks>
    private void CleanUpIfNeeded_NoLock()
    {
        if (++_addsSinceLastCleanUp < _cleanUpThreshold)
        {
            return;
        }

        // Use a memory builder to collect keys of dead weak references
        using var deadKeys = new MemoryBuilder<TKey>(initialCapacity: _cacheMap.Count, clearArray: true);

        // Identify all keys with dead weak references
        foreach (var (key, weakRef) in _cacheMap)
        {
            if (!weakRef.TryGetTarget(out _))
            {
                deadKeys.Append(key);
            }
        }

        // Remove all dead entries from the cache
        foreach (var key in deadKeys.AsMemory().Span)
        {
            _cacheMap.Remove(key);
        }

        // Reset the add counter since we just performed cleanup
        _addsSinceLastCleanUp = 0;
    }
}
