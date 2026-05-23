// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal abstract class LazyContent
{
    private LazyContent()
    {
    }

    public abstract string Value { get; }

    public static LazyContent Create<T>(T arg, Func<T, string> contentFactory)
    {
        ArgHelper.ThrowIfNull(contentFactory);

        return new LazyContentImpl<T>(arg, contentFactory);
    }

    private sealed class LazyContentImpl<T>(T arg, Func<T, string> contentFactory) : LazyContent
    {
        private const int Uninitialized = 0;
        private const int Computing = 1;
        private const int Initialized = 2;

        private T _arg = arg;
        private Func<T, string?> _contentFactory = contentFactory;

        private string _value = null!;
        private int _state;

        public override string Value
        {
            get
            {
                // Return the value if it has already been initialized; otherwise, compute it.
                return Volatile.Read(ref _state) == Initialized
                    ? _value
                    : GetOrComputeAndStoreValue();
            }
        }

        private string GetOrComputeAndStoreValue()
        {
            SpinWait spinner = default;

            while (true)
            {
                switch (Interlocked.CompareExchange(ref _state, Computing, Uninitialized))
                {
                    case Uninitialized:
                        Debug.Assert(_contentFactory is not null, "Content factory should not be null at this point.");

                        // This thread gets to compute the value and clear the references for GC.
                        _value = _contentFactory(_arg) ?? string.Empty;

                        _arg = default!;
                        _contentFactory = null!;

                        Volatile.Write(ref _state, Initialized);

                        return _value;

                    case Computing:
                        // Another thread is already computing the value, wait for it to finish.
                        spinner.SpinOnce();

                        continue;

                    case Initialized:
                        // The value has been initialized by another thread. Return it!
                        return _value;

                    default:
                        return Assumed.Unreachable<string>();
                }
            }
        }
    }
}
