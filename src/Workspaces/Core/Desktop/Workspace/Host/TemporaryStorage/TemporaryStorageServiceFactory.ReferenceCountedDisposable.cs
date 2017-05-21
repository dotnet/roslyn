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
        /// <summary>
        /// A reference-counting wrapper which allows multiple uses of a single disposable object in code, which is
        /// deterministically released (by calling <see cref="IDisposable.Dispose"/>) when the last reference is
        /// disposed.
        /// </summary>
        /// <remarks>
        /// <para>While each instance of <see cref="ReferenceCountedDisposable{T}"/> should be explicitly disposed when
        /// the object is no longer needed by the code owning the reference, this implementation will not leak resources
        /// in the event one or more callers fail to do so. The underlying object will be deterministically released
        /// when the last reference to it is disposed. However, the underlying object is eligible for non-deterministic
        /// release (should it have a finalizer) when each reference to it is <em>either</em> disposed <em>or</em>
        /// eligible for garbage collection itself.</para>
        ///
        /// <para>All public methods on this type adhere to their pre- and post-conditions and will not invalidate state
        /// even in concurrent execution. The implementation of <see cref="TryAddReference"/> is lock-free; all other
        /// methods are wait-free, with the exception of the specific call to <see cref="Dispose"/> which results in the
        /// underlying object getting disposed. For that case, the implementation will not hold any locks and will
        /// return when the call to <see cref="Dispose"/> completes.</para>
        /// </remarks>
        /// <typeparam name="T">The type of disposable object.</typeparam>
        internal sealed class ReferenceCountedDisposable<T> : IDisposable
            where T : class, IDisposable
        {
            /// <summary>
            /// The target of this reference. This value is initialized to a non-<see langword="null"/> value in the
            /// constructor, and set to <see langword="null"/> when the current reference is disposed.
            /// </summary>
            /// <remarks>
            /// <para>This value is only cleared in order to support cases where one or more references is garbage
            /// collected without having <see cref="Dispose"/> called. It is cleared <em>after</em> the
            /// <see cref="_boxedReferenceCount"/> field is cleared, which is leveraged to ensure that concurrent calls
            /// to <see cref="Dispose"/> and <see cref="TryAddReference"/> cannot result in a "valid" reference pointing
            /// to a disposed underlying object.</para>
            /// </remarks>
            private T _instance;

            /// <summary>
            /// The boxed reference count, which is shared by all references with the same <see cref="Target"/> object.
            /// </summary>
            /// <remarks>
            /// <para>Only use equality operators to compare this value with 0. The actual reference count is allowed to
            /// be a negative integer in order to support a reference count full 32-bit number of reference. Ideally it
            /// would be represented as a <see cref="uint"/>, but some <see cref="Interlocked"/> operations are not
            /// implemented for this type.</para>
            ///
            /// <para>This field is set to <see langword="null"/> at the point in time when this reference is disposed.
            /// This occurs prior to clearing the <see cref="_instance"/> field in order to support concurrent
            /// code.</para>
            /// </remarks>
            private StrongBox<int> _boxedReferenceCount;

            /// <summary>
            /// Initializes a new reference counting wrapper around an <see cref="IDisposable"/> object.
            /// </summary>
            /// <remarks>
            /// <para>The reference count is initialized to 1.</para>
            /// </remarks>
            /// <param name="instance">The object owned by this wrapper.</param>
            public ReferenceCountedDisposable(T instance)
            {
                _instance = instance ?? throw new ArgumentNullException(nameof(instance));
                _boxedReferenceCount = new StrongBox<int>(1);
            }

            private ReferenceCountedDisposable(T instance, StrongBox<int> referenceCount)
            {
                _instance = instance;

                // The reference count has already been incremented for this instance
                _boxedReferenceCount = referenceCount;
            }

            /// <summary>
            /// Gets the target object.
            /// </summary>
            /// <remarks>
            /// <para>This call is not valid after <see cref="Dispose"/> is called. If this property or the target
            /// object is used concurrently with a call to <see cref="Dispose"/>, it is possible for the code to be
            /// using a disposed object. After the current instance is disposed, this property returns
            /// <see langword="null"/>. However, the exact time when this property starts returning null after
            /// <see cref="Dispose"/> is called is unspecified; code is expected to not use this property or the object
            /// it returns after any code invokes <see cref="Dispose"/>.</para>
            /// </remarks>
            /// <value>The target object.</value>
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
                var (target, referenceCount) = AtomicReadState();
                if (referenceCount == null)
                {
                    return null;
                }

                return TryAddReferenceImpl(target, referenceCount);
            }

            /// <summary>
            /// Provides the implementation for <see cref="TryAddReference"/> and
            /// <see cref="WeakReference.TryAddReference"/>.
            /// </summary>
            private static ReferenceCountedDisposable<T> TryAddReferenceImpl(T target, StrongBox<int> referenceCount)
            {
                // Cannot use Interlocked.Increment because we need to latch the reference count when it reaches 0 (i.e.
                // once it reaches zero, it is no longer allowed to ever be non-zero).
                //
                // Note: This block can execute concurrently with Dispose().
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
                        // Must return a new instance, in order for the Dispose operation on each individual instance to
                        // be idempotent.
                        return new ReferenceCountedDisposable<T>(target, referenceCount);
                    }
                }
            }

            /// <summary>
            /// Performs an read of <see cref="_instance"/> and <see cref="_boxedReferenceCount"/>, where the mutation
            /// performed by <see cref="Dispose"/> behaves as an atomic operation.
            /// </summary>
            /// <returns>
            /// <para>If the returned reference count box is non-<see langword="null"/>, then the target will be set to
            /// the object this reference was initialized with. However, the target will only be valid prior to the
            /// reference count being set to zero.</para>
            ///
            /// <para>If the returned reference count box is <see langword="null"/>, then the target will also be
            /// <see langword="null"/>.</para>
            /// </returns>
            private (T target, StrongBox<int> referenceCount) AtomicReadState()
            {
                // This value can be set in Dispose. A memory barrier is used to ensure _boxedReferenceCount is read
                // after _instance (a stale read of _instance is not an error).
                var target = _instance;
                Interlocked.MemoryBarrier();
                var referenceCount = Volatile.Read(ref _boxedReferenceCount);
                if (referenceCount == null)
                {
                    return (null, null);
                }
                else
                {
                    return (target, referenceCount);
                }
            }

            /// <summary>
            /// Releases the current reference, causing the underlying object to be disposed if this was the last
            /// reference.
            /// </summary>
            /// <remarks>
            /// <para>After this instance is disposed, the <see cref="TryAddReference"/> method can no longer be used to
            /// object a new reference to the target, even if other references to the target object are still in
            /// use.</para>
            /// </remarks>
            public void Dispose()
            {
                var referenceCount = Interlocked.Exchange(ref _boxedReferenceCount, null);
                if (referenceCount == null)
                {
                    // Already disposed; allow multiple without error.
                    return;
                }

                // Set the instance back to its default value, so in case someone forgets to dispose of one of the
                // counted references the finalizer of the underlying disposable object will have a chance to clean up
                // the unmanaged resources as soon as possible. The value read during this exchange cannot be null; only
                // one thread will observe a non-null value for _boxedReferenceCount above, and that is the same thread
                // which will exchange _instance on this line.
                var instance = Interlocked.Exchange(ref _instance, null);

                var decrementedValue = Interlocked.Decrement(ref referenceCount.Value);
                if (decrementedValue == 0)
                {
                    instance.Dispose();
                }
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
                private readonly WeakReference<T> _weakInstance;
                private readonly StrongBox<int> _boxedReferenceCount;

                public WeakReference(ReferenceCountedDisposable<T> reference)
                    : this()
                {
                    if (reference == null)
                    {
                        throw new ArgumentNullException(nameof(reference));
                    }

                    var (instance, referenceCount) = reference.AtomicReadState();
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

                    _weakInstance = new WeakReference<T>(instance);
                    _boxedReferenceCount = referenceCount;
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
                    var weakInstance = _weakInstance;
                    if (weakInstance == null || !_weakInstance.TryGetTarget(out var target))
                    {
                        return null;
                    }

                    var referenceCount = _boxedReferenceCount;
                    if (referenceCount == null)
                    {
                        return null;
                    }

                    return TryAddReferenceImpl(target, referenceCount);
                }
            }
        }
    }
}
