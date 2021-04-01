// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace CSharpSyntaxGenerator
{
    /// <summary>
    /// This class behaves like <see cref="WeakReference{T}"/>, with the added constraint that the referenced object
    /// will not be reclaimed prior to the next Generation 2 garbage collection operation.
    /// </summary>
    /// <remarks>
    /// The Generation 2 constraint only applies to the current target of the soft reference. If the target is replaced
    /// by calling <see cref="SetTarget(T)"/>, the previous target may be immediately eligible for garbage collection.
    /// </remarks>
    /// <typeparam name="T">The type of object referenced.</typeparam>
    internal sealed class SoftReference<T>
        where T : class?
    {
        private readonly WeakReference<T?> _weakReference = new(null);
        private T? _strongReference;

        public SoftReference(T? target)
        {
            Gen2GcCallback.Register(
                static self => ((SoftReference<T>)self).OnGen2GC(),
                this);

            if (target is not null)
                SetTarget(target);
        }

        public void SetTarget(T target)
        {
            // Set _weakReference prior to setting _strongReference. TryGetTarget reads them in the opposite order.
            _weakReference.SetTarget(target);
            if (target is not null && GC.GetGeneration(target) < 2)
                Volatile.Write(ref _strongReference, target);
            else
                Volatile.Write(ref _strongReference, null);
        }

        public bool TryGetTarget([NotNullWhen(true)] out T? target)
        {
            target = Volatile.Read(ref _strongReference);
            if (target is not null)
                return true;

            return _weakReference.TryGetTarget(out target);
        }

        private bool OnGen2GC()
        {
            if (_weakReference.TryGetTarget(out var target)
                && GC.GetGeneration(target) >= 2)
            {
                // Clear the strong reference if the weak reference is in Gen 2
                Interlocked.CompareExchange(ref _strongReference, null, target);
            }

            // Return true to keep receiving Gen 2 callbacks
            return true;
        }
    }
}
