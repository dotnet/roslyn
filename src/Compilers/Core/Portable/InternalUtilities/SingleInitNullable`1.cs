// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Roslyn.Utilities
{
    [NonCopyable]
    internal struct SingleInitNullable<T>
        where T : struct
    {
        private T _value;
        private int _state;

        public static implicit operator T?(in SingleInitNullable<T> value)
        {
            return value.ReadIfInitialized();
        }

        /// <summary>
        /// Ensure that the given target value is initialized (not null) in a thread-safe manner.
        /// </summary>
        /// <param name="valueFactory">A factory delegate to create a new instance of the target value. Note that this delegate may be called
        /// more than once by multiple threads, but only one of those values will successfully be written to the target.</param>
        /// <returns>The target value.</returns>
        public T Initialize(Func<T> valueFactory)
        {
            return ReadIfInitialized() ?? GetOrStore(valueFactory());
        }

        /// <summary>
        /// Ensure that the given target value is initialized (not null) in a thread-safe manner.
        /// </summary>
        /// <param name="valueFactory">A factory delegate to create a new instance of the target value. Note that this delegate may be called
        /// more than once by multiple threads, but only one of those values will successfully be written to the target.</param>
        /// <returns>The target value.</returns>
        public T Initialize<TArg>(Func<TArg, T> valueFactory, TArg factoryArgument)
        {
            return ReadIfInitialized() ?? GetOrStore(valueFactory(factoryArgument));
        }

        private T GetOrStore(T value)
        {
            while (true)
            {
                switch (Interlocked.CompareExchange(ref _state, 1, 0))
                {
                    case 0:
                        // This thread is responsible for assigning the value.
                        _value = value;
                        Thread.MemoryBarrier();
                        Volatile.Write(ref _state, 2);
                        return value;

                    case 1:
                        // Another thread has already claimed responsibility for writing the value, but that write is
                        // not yet complete.
                        Thread.Yield();
                        continue;

                    case 2:
                        // Another thread has already completed writing the value.
                        return ReadIfInitialized() ?? throw ExceptionUtilities.Unreachable();

                    case var unexpectedValue:
                        throw ExceptionUtilities.UnexpectedValue(unexpectedValue);
                }
            }
        }

        private readonly T? ReadIfInitialized()
        {
            if (Volatile.Read(ref Unsafe.AsRef(in _state)) == 2)
            {
                // The value was fully initialized before 'initialized' was set to 2
                return _value;
            }

            return null;
        }
    }
}
