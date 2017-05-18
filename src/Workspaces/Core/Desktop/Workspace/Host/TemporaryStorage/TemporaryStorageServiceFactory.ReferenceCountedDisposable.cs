// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

#if DEBUG
using System.Diagnostics;
#endif

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class TemporaryStorageServiceFactory
    {
        private sealed class ReferenceCountedDisposable<T> : IDisposable
            where T : IDisposable
        {
            private T _instance;
            private StrongBox<int> _referenceCount;

            /// <summary>
            /// Initializes a new reference counting wrapper around an <see cref="IDisposable"/> object.
            /// </summary>
            /// <remarks>
            /// <para>The reference count is initialized to 1.</para>
            /// </remarks>
            /// <param name="instance">The object owned by this wrapper.</param>
            public ReferenceCountedDisposable(T instance)
            {
                _instance = instance;
                _referenceCount = new StrongBox<int>(1);
            }

            private ReferenceCountedDisposable(T instance, StrongBox<int> referenceCount)
            {
                _instance = instance;

                // The reference count has already been incremented for this instance
                _referenceCount = referenceCount;
            }

            /// <summary>
            /// Gets the target object.
            /// </summary>
            /// <remarks>
            /// <para>This call is not valid after <see cref="Dispose"/> is called.</para>
            /// </remarks>
            public T Target => _instance;

            /// <summary>
            /// Increments the reference count for the disposable object, and returns a new disposable reference to it.
            /// </summary>
            /// <remarks>
            /// <para>The returned object is an independent reference to the same underlying object. Disposing of the
            /// returned value multiple times will only cause the reference count to be decreased once.</para>
            /// </remarks>
            /// <returns>A new <see cref="ReferenceCountedDisposable{T}"/> pointing to the same underlying object, if it
            /// has not yet been disposed; otherwise, <see langword="null"/> if this reference to the underlying object
            /// has already been disposed.</returns>
            public ReferenceCountedDisposable<T> TryAddReference()
            {
                // This value can be set in Dispose. However, even if we have a race condition with Dispose where it's
                // set to null in the middle of the call, the reference count will be incremented by this method if and
                // only if a valid new reference was created. This is because the validity of this instance for creating
                // new references is determined by _referenceCount.
                var target = _instance;
                Interlocked.MemoryBarrier();
                var referenceCount = Volatile.Read(ref _referenceCount);

                // Cannot use Interlocked.Increment because we need to latch the reference count when it reaches 0.
                while (true)
                {
                    var currentValue = Volatile.Read(ref referenceCount.Value);
                    if (currentValue == 0)
                    {
                        // The target is already disposed, and cannot be reused
                        return null;
                    }

                    if (Interlocked.CompareExchange(ref referenceCount.Value, currentValue + 1, currentValue) == currentValue)
                    {
                        return new ReferenceCountedDisposable<T>(target, referenceCount);
                    }
                }
            }

            public void Dispose()
            {
                var referenceCount = Interlocked.Exchange(ref _referenceCount, null);
                if (referenceCount == null)
                {
                    // Already disposed; allow multiple without error.
                    return;
                }

                // This is the thread which will write to _instance, so this will act as an atomic read
                var instance = _instance;

                // Set the instance back to its default value, so in case someone forgets to dispose of one of the
                // counted references the finalizer of the underlying disposable object will have a chance to clean up
                // the unmanaged resources as soon as possible.
                Interlocked.MemoryBarrier();
                _instance = default(T);

#if !DEBUG
                var decrementedValue = Interlocked.Decrement(ref referenceCount.Value);
                if (decrementedValue == 0)
                {
                    instance.Dispose();
                }
#else
                while (true)
                {
                    var currentValue = Volatile.Read(ref referenceCount.Value);
                    Debug.Assert(currentValue > 0, "Dispose should have protected itself against races.");

                    if (Interlocked.CompareExchange(ref referenceCount.Value, currentValue - 1, currentValue) == currentValue)
                    {
                        if (currentValue == 1)
                        {
                            // Reference count hit 0 for this call
                            instance.Dispose();
                        }

                        break;
                    }
                }
#endif
            }

            /// <summary>
            /// Represents a weak reference to a <see cref="ReferenceCountedDisposable{T}"/> which is capable of
            /// obtaining a new counted reference up until the point when the object is no longer accessible.
            /// </summary>
            public struct WeakReference
            {
                /// <summary>
                /// DO NOT DISPOSE OF THE TARGET.
                /// </summary>
                private readonly WeakReference<ReferenceCountedDisposable<T>> _instance;

                public WeakReference(ReferenceCountedDisposable<T> reference)
                    : this()
                {
                    var instance = reference._instance;
                    Interlocked.MemoryBarrier();
                    var referenceCount = Volatile.Read(ref reference._referenceCount);
                    if (referenceCount == null)
                    {
                        // The specified reference is already not valid.
                        return;
                    }

                    if (referenceCount.Value == 0)
                    {
                        // We were able to read the reference, but it's already disposed.
                        return;
                    }

                    var innerReference = new ReferenceCountedDisposable<T>(instance, referenceCount);
                    _instance = new WeakReference<ReferenceCountedDisposable<T>>(innerReference);
                }

                /// <summary>
                /// Increments the reference count for the disposable object, and returns a new disposable reference to
                /// it.
                /// </summary>
                /// <remarks>
                /// <para>Unlike <see cref="ReferenceCountedDisposable{T}.TryAddReference"/>, this method is capable of
                /// adding a reference to the underlying instance all the way up to the point where it is finally
                /// disposed.</para>
                ///
                /// <para>The returned object is an independent reference to the same underlying object. Disposing of
                /// the returned value multiple times will only cause the reference count to be decreased once.</para>
                /// </remarks>
                /// <returns>A new <see cref="ReferenceCountedDisposable{T}"/> pointing to the same underlying object,
                /// if it has not yet been disposed; otherwise, <see langword="null"/> if the underlying object has
                /// already been disposed.</returns>
                public ReferenceCountedDisposable<T> TryAddReference()
                {
                    var instance = _instance;
                    if (instance == null)
                    {
                        return null;
                    }

                    if (!_instance.TryGetTarget(out var target))
                    {
                        return null;
                    }

                    return target.TryAddReference();
                }
            }
        }
    }
}
