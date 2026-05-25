// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.AspNetCore.Razor;

/// <summary>
///  Provides lock-free atomic operations for thread-safe initialization patterns.
/// </summary>
/// <remarks>
///  This class implements common atomic initialization patterns using <see cref="Interlocked"/> operations
///  and volatile memory access to ensure thread safety without locks. These patterns are useful for
///  lazy initialization scenarios where multiple threads might attempt to initialize the same value
///  concurrently.
/// </remarks>
internal static class InterlockedOperations
{
    // State constants for initialization tracking
    private const int NotInitialized = 0;
    private const int Initializing = 1;
    private const int Initialized = 2;

    /// <summary>
    ///  Atomically initializes a reference type field if it is currently <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The reference type of the target field.</typeparam>
    /// <param name="target">A reference to the field to initialize.</param>
    /// <param name="value">The value to assign to the field if it is currently <see langword="null"/>.</param>
    /// <returns>
    ///  The current value of <paramref name="target"/> if it was not <see langword="null"/>,
    ///  or <paramref name="value"/> if <paramref name="target"/> was <see langword="null"/> and was successfully initialized.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method uses <see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/> to atomically
    ///   check if the target field is <see langword="null"/> and assign the value if so.
    ///   If multiple threads call this method concurrently, only one will successfully set the value,
    ///   and all threads will receive the same result.
    ///  </para>
    ///  <para>
    ///   This is useful for simple lazy initialization scenarios where the initialization logic
    ///   is inexpensive and can be safely executed multiple times.
    ///  </para>
    /// </remarks>
    /// <example>
    ///  <code>
    ///  private static string? _cachedValue;
    /// 
    ///  public static string GetCachedValue()
    ///  {
    ///      return InterlockedOperations.Initialize(ref _cachedValue, ComputeExpensiveValue());
    ///  }
    ///  </code>
    /// </example>
    public static T Initialize<T>([NotNull] ref T? target, T value)
        where T : class
        => Interlocked.CompareExchange(ref target, value, null) ?? value;

    /// <summary>
    ///  Atomically initializes a field using a factory function with guaranteed single execution.
    /// </summary>
    /// <typeparam name="T">The type of the target field.</typeparam>
    /// <param name="target">A reference to the field to initialize.</param>
    /// <param name="state">A reference to an integer field used to track initialization state.</param>
    /// <param name="factory">A function that creates the value to assign to the target field.</param>
    /// <returns>The initialized value of the target field.</returns>
    /// <remarks>
    ///  <para>
    ///   This method implements a lock-free lazy initialization pattern that guarantees the factory
    ///   function will be called exactly once, even when multiple threads attempt initialization
    ///   concurrently. The method uses a three-state protocol:
    ///  </para>
    ///  <list type="number">
    ///   <item><strong>NotInitialized (0)</strong>: The field has not been initialized.</item>
    ///   <item><strong>Initializing (1)</strong>: A thread is currently executing the factory.</item>
    ///   <item><strong>Initialized (2)</strong>: The field has been successfully initialized.</item>
    ///  </list>
    ///  <para>
    ///   When multiple threads call this method:
    ///  </para>
    ///  <list type="bullet">
    ///   <item>One thread wins the race and executes the factory function.</item>
    ///   <item>Other threads wait using <see cref="SpinWait"/> until initialization completes.</item>
    ///   <item>All threads receive the same initialized value.</item>
    ///  </list>
    ///  <para>
    ///   This pattern is ideal for expensive initialization operations that should only be performed once.
    ///  </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <example>
    ///  <code>
    ///  private static Dictionary&lt;string, int&gt;? _lookupTable;
    ///  private static int _lookupTableState;
    /// 
    ///  public static Dictionary&lt;string, int&gt; GetLookupTable()
    ///  {
    ///      return InterlockedOperations.Initialize(
    ///          ref _lookupTable, 
    ///          ref _lookupTableState, 
    ///          () => BuildExpensiveLookupTable());
    ///  }
    ///  </code>
    /// </example>
    public static T Initialize<T>(ref T target, ref int state, Func<T> factory)
    {
        // Fast path: Are we already initialized?
        if (Volatile.Read(ref state) == Initialized)
        {
            return target;
        }

        // Try to claim the right to initialize
        if (Interlocked.CompareExchange(ref state, Initializing, NotInitialized) == NotInitialized)
        {
            try
            {
                // We won the race, so we get to initialize
                var newValue = factory();

                target = newValue;

                // Mark as initialized
                Volatile.Write(ref state, Initialized);

                return newValue;
            }
            catch
            {
                // Reset state so other threads (or retry) can attempt initialization
                Volatile.Write(ref state, NotInitialized);
                throw;
            }
        }

        // Another thread is or was initializing - wait for it to complete.
        var spinWait = new SpinWait();

        while (Volatile.Read(ref state) != Initialized)
        {
            spinWait.SpinOnce();
        }

        return target;
    }

    /// <summary>
    ///  Atomically initializes a field using a factory function with an argument and guaranteed single execution.
    /// </summary>
    /// <typeparam name="TArg">The type of the argument passed to the factory function.</typeparam>
    /// <typeparam name="T">The type of the target field.</typeparam>
    /// <param name="target">A reference to the field to initialize.</param>
    /// <param name="state">A reference to an integer field used to track initialization state.</param>
    /// <param name="arg">The argument to pass to the factory function.</param>
    /// <param name="factory">A function that creates the value using the provided argument.</param>
    /// <returns>The initialized value of the target field.</returns>
    /// <remarks>
    ///  <para>
    ///   This overload extends the basic initialization pattern to support factory functions that
    ///   require an argument. This is useful for avoiding closure allocations when the factory
    ///   needs to access external state.
    ///  </para>
    ///  <para>
    ///   The method follows the same three-state initialization protocol as
    ///   <see cref="Initialize{T}(ref T, ref int, Func{T})"/> and provides the same thread-safety
    ///   guarantees with single execution of the factory function.
    ///  </para>
    ///  <para>
    ///   Using this overload with static factory methods can help avoid delegate allocations:
    ///  </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <example>
    ///  <code>
    ///  private static Dictionary&lt;Checksum, int&gt;? _lookupTable;
    ///  private static int _lookupTableState;
    ///  
    ///  public static Dictionary&lt;Checksum, int&gt; GetLookupTable(IEnumerable&lt;Item&gt; items)
    ///  {
    ///      return InterlockedOperations.Initialize(
    ///          ref _lookupTable, 
    ///          ref _lookupTableState, 
    ///          items,
    ///          static items => BuildLookupTable(items)); // Static method avoids closure
    ///  }
    ///  </code>
    /// </example>
    public static T Initialize<TArg, T>(ref T target, ref int state, TArg arg, Func<TArg, T> factory)
    {
        // Fast path: Are we already initialized?
        if (Volatile.Read(ref state) == Initialized)
        {
            return target;
        }

        // Try to claim the right to initialize
        if (Interlocked.CompareExchange(ref state, Initializing, NotInitialized) == NotInitialized)
        {
            try
            {
                // We won the race, so we get to initialize
                var newValue = factory(arg);

                target = newValue;

                // Mark as initialized
                Volatile.Write(ref state, Initialized);

                return newValue;
            }
            catch
            {
                // Reset state so other threads (or retry) can attempt initialization
                Volatile.Write(ref state, NotInitialized);
                throw;
            }
        }

        // Another thread is or was initializing - wait for it to complete.
        var spinWait = new SpinWait();

        while (Volatile.Read(ref state) != Initialized)
        {
            spinWait.SpinOnce();
        }

        return target;
    }
}
