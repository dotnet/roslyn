// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A covariant interface form of <see cref="ReferenceCountedDisposable{T}"/> that lets you re-cast an <see cref="ReferenceCountedDisposable{T}"/>
    /// to a more base type. This can include types that do not implement <see cref="IDisposable"/> if you want to prevent a caller from accidentally
    /// disposing <see cref="Target"/> directly.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IReferenceCountedDisposable<out T> : IDisposable
#if !CODE_STYLE
        , IAsyncDisposable
#endif
        where T : class
    {
        /// <summary>
        /// Gets the target object.
        /// </summary>
        /// <remarks>
        /// <para>This call is not valid after <see cref="IDisposable.Dispose"/> is called. If this property or the target
        /// object is used concurrently with a call to <see cref="IDisposable.Dispose"/>, it is possible for the code to be
        /// using a disposed object. After the current instance is disposed, this property throws
        /// <see cref="ObjectDisposedException"/>. However, the exact time when this property starts throwing after
        /// <see cref="IDisposable.Dispose"/> is called is unspecified; code is expected to not use this property or the object
        /// it returns after any code invokes <see cref="IDisposable.Dispose"/>.</para>
        /// </remarks>
        /// <value>The target object.</value>
        T Target { get; }

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
        IReferenceCountedDisposable<T>? TryAddReference();
    }
}
