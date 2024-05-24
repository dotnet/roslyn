// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Roslyn.Utilities;

/// <summary>
/// A lazily initialized version of <see cref="Nullable{T}"/> which uses the same space as a <see cref="Nullable{T}"/>.
/// </summary>
[NonCopyable]
internal struct SingleInitNullable<T>
    where T : struct
{
    /// <summary>
    /// One of three values:
    /// <list type="bullet">
    /// <item>0. <see cref="_value"/> is not initialized yet.</item>
    /// <item>1. <see cref="_value"/> is currently being initialized by some thread.</item>
    /// <item>2. <see cref="_value"/> has been initialized.</item>
    /// </list>
    /// </summary>
    private int _initialized;

    /// <summary>
    /// Actual stored value.  Only safe to read once <see cref="_initialized"/> is set to 2.
    /// </summary>
    private T _value;

    /// <summary>
    /// Ensure that the given target value is initialized in a thread-safe manner.
    /// </summary>
    /// <param name="valueFactory">A factory delegate to create a new instance of the target value. Note that this
    /// delegate may be called more than once by multiple threads, but only one of those values will successfully be
    /// written to the target.</param>
    /// <returns>The target value.</returns>
    /// <remarks>
    /// An alternative approach here would be to pass <paramref name="valueFactory"/> and <paramref name="arg"/> into
    /// <see cref="GetOrStore"/>, and to only compute the value if the winning thread.  However, this has two potential
    /// downsides.  First, the computation of the value might take an indeterminate amount of time.  This would require
    /// other threads to then busy-spin for that same amount of time.  Second, we would have to make the code very
    /// resilient to failure paths (including cancellation), ensuring that the type reset itself <em>safely</em> to the
    /// initial state so that other threads were not perpetually stuck in the busy state.
    /// </remarks>
    public T Initialize<TArg>(Func<TArg, T> valueFactory, TArg arg)
        => ReadIfInitialized() ?? GetOrStore(valueFactory(arg));

    private T? ReadIfInitialized()
        => Volatile.Read(ref _initialized) == 2 ? _value : null;

    private T GetOrStore(T value)
    {
        SpinWait spinWait = default;
        while (true)
        {
            switch (Interlocked.CompareExchange(ref _initialized, value: 1, comparand: 0))
            {
                case 0:
                    // This thread is responsible for assigning the value to _value.
                    _value = value;
                    Volatile.Write(ref _initialized, 2);
                    return value;

                case 1:
                    // Another thread has already claimed responsibility for writing to target, but that write is
                    // not yet complete.  Spin until we see the value finally transition to the '2' state.
                    spinWait.SpinOnce();
                    continue;

                case 2:
                    // Another thread has already completed writing to _value.  Because we use a CompareExchange, we can
                    // only get here once the VolatileWrite to _initialized has happened.  Which means the write to
                    // _value must be seen (as writes can't be reordered across these calls).
                    return ReadIfInitialized() ?? throw ExceptionUtilities.Unreachable();

                case var unexpectedValue:
                    throw ExceptionUtilities.UnexpectedValue(unexpectedValue);
            }
        }
    }
}
