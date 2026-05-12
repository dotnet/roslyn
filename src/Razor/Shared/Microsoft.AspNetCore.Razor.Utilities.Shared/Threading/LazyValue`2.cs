// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Threading;

/// <summary>
///  Provides lock-free lazy initialization for a value using a factory function that accepts an argument.
/// </summary>
/// <typeparam name="TArg">The type of the argument passed to the factory function.</typeparam>
/// <typeparam name="T">The type of the lazily initialized value.</typeparam>
/// <remarks>
///  <para>
///   <see cref="LazyValue{TArg, T}"/> implements a thread-safe lazy initialization pattern that guarantees
///   the factory function will be executed exactly once with the provided argument, even when accessed
///   concurrently from multiple threads. The implementation uses atomic operations and does not require locks.
///  </para>
///  <para>
///   This variant is particularly useful when the factory function needs access to external state
///   but you want to avoid closure allocations. By accepting the required state as an argument,
///   you can use static factory methods that don't capture variables.
///  </para>
/// </remarks>
/// <example>
///  <code>
///  private LazyValue&lt;MyCollection, Dictionary&lt;string, int&gt;&gt; _lookup = new(BuildLookupTable);
///  
///  public Dictionary&lt;string, int&gt; GetLookup(MyCollection collection)
///  {
///      return _lookup.GetValue(collection); // Thread-safe, initialized exactly once
///  }
///  
///  private static Dictionary&lt;string, int&gt; BuildLookupTable(MyCollection collection)
///  {
///      // Static method avoids closure allocation
///      return collection.Items.ToDictionary(x => x.Key, x => x.Value);
///  }
///  </code>
/// </example>
internal struct LazyValue<TArg, T>
{
    private readonly Func<TArg, T> _factory;
    private T _value;
    private int _state;

    /// <summary>
    ///  Initializes a new instance of the <see cref="LazyValue{TArg, T}"/> struct with the specified factory function.
    /// </summary>
    /// <param name="factory">A function that creates the value using the provided argument when it is first requested.</param>
    /// <remarks>
    ///  <para>
    ///   The factory function will be called at most once, when <see cref="GetValue(TArg)"/> is first called.
    ///   If multiple threads call <see cref="GetValue(TArg)"/> concurrently, only one thread will execute
    ///   the factory function, and all threads will receive the same result.
    ///  </para>
    ///  <para>
    ///   Using this overload with static factory methods can help avoid delegate and closure allocations:
    ///  </para>
    ///  <code>
    ///  // Preferred: static method avoids closure
    ///  var lazy = new LazyValue&lt;MyData, ProcessedData&gt;(ProcessData);
    ///  
    ///  // Avoid: lambda creates closure allocation
    ///  var lazy = new LazyValue&lt;MyData, ProcessedData&gt;(data => ExpensiveOperation(data, someField));
    ///  </code>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
    public LazyValue(Func<TArg, T> factory)
    {
        _factory = factory;
        Unsafe.SkipInit(out this);
    }

    /// <summary>
    ///  Gets the lazily initialized value, creating it if necessary using the provided argument.
    /// </summary>
    /// <param name="arg">The argument to pass to the factory function if the value needs to be created.</param>
    /// <returns>
    ///  The initialized value.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method is thread-safe. If multiple threads call this method concurrently before
    ///   the value has been initialized, the factory function will be executed exactly once
    ///   with the provided argument, and all threads will receive the same result.
    ///  </para>
    ///  <para>
    ///   After the first call, subsequent calls to this method will return the cached value
    ///   without executing the factory function again. The <paramref name="arg"/> parameter
    ///   is ignored on subsequent calls.
    ///  </para>
    ///  <para>
    ///   The implementation uses <see cref="InterlockedOperations.Initialize{TArg, T}(ref T, ref int, TArg, Func{TArg, T})"/>
    ///   to provide lock-free thread-safe initialization with guaranteed single execution.
    ///  </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///  The factory function is <see langword="null"/> (which can only happen if the struct was created using default construction).
    /// </exception>
    public T GetValue(TArg arg)
        => InterlockedOperations.Initialize(ref _value, ref _state, arg, _factory);
}
