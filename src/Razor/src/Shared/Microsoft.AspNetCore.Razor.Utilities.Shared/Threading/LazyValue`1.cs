// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Threading;

/// <summary>
///  Provides lock-free lazy initialization for a value using a factory function.
/// </summary>
/// <typeparam name="T">The type of the lazily initialized value.</typeparam>
/// <remarks>
///  <para>
///   <see cref="LazyValue{T}"/> implements a thread-safe lazy initialization pattern that guarantees
///   the factory function will be executed exactly once, even when accessed concurrently from multiple threads.
///   The implementation uses atomic operations and does not require locks.
///  </para>
///  <para>
///   This struct is designed to be used as a field in classes where expensive initialization should be
///   deferred until the value is actually needed. Unlike <see cref="Lazy{T}"/>, this implementation
///   does not allocate additional objects and has minimal overhead.
///  </para>
/// </remarks>
/// <example>
///  <code>
///  private LazyValue&lt;Dictionary&lt;string, int&gt;&gt; _lookup = new(() => BuildExpensiveLookup());
///  
///  public Dictionary&lt;string, int&gt; GetLookup()
///  {
///      return _lookup.GetValue(); // Thread-safe, initialized exactly once
///  }
///  </code>
/// </example>
internal struct LazyValue<T>
{
    private readonly Func<T> _factory;
    private T _value;
    private int _state;

    /// <summary>
    ///  Initializes a new instance of the <see cref="LazyValue{T}"/> struct with the specified factory function.
    /// </summary>
    /// <param name="factory">A function that creates the value when it is first requested.</param>
    /// <remarks>
    ///  The factory function will be called at most once, when <see cref="GetValue"/> is first called.
    ///  If multiple threads call <see cref="GetValue"/> concurrently, only one thread will execute
    ///  the factory function, and all threads will receive the same result.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
    public LazyValue(Func<T> factory)
    {
        _factory = factory;
        Unsafe.SkipInit(out this);
    }

    /// <summary>
    ///  Gets the lazily initialized value, creating it if necessary.
    /// </summary>
    /// <returns>The initialized value.</returns>
    /// <remarks>
    ///  <para>
    ///   This method is thread-safe. If multiple threads call this method concurrently before
    ///   the value has been initialized, the factory function will be executed exactly once,
    ///   and all threads will receive the same result.
    ///  </para>
    ///  <para>
    ///   After the first call, subsequent calls to this method will return the cached value
    ///   without executing the factory function again.
    ///  </para>
    ///  <para>
    ///   The implementation uses <see cref="InterlockedOperations.Initialize{T}(ref T, ref int, Func{T})"/>
    ///   to provide lock-free thread-safe initialization with guaranteed single execution.
    ///  </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///  The factory function is <see langword="null"/> (which can only happen if the struct was created using default construction).
    /// </exception>
    public T GetValue()
        => InterlockedOperations.Initialize(ref _value, ref _state, _factory);
}
