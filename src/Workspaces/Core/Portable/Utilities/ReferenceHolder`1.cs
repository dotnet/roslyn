// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Utilities;

internal readonly struct ReferenceHolder<T> : IEquatable<ReferenceHolder<T>>
    where T : class?
{
    private readonly T? _strongReference;
    private readonly WeakReference<T>? _weakReference;
    private readonly int _hashCode;

    private ReferenceHolder(T strongReference)
    {
        _strongReference = strongReference;
        _weakReference = null;
        _hashCode = 0;
    }

    private ReferenceHolder(WeakReference<T> weakReference, int hashCode)
    {
        _strongReference = null;
        _weakReference = weakReference;
        _hashCode = hashCode;
    }

    public static ReferenceHolder<T> Strong(T value)
        => new(value);

    public static ReferenceHolder<T> Weak(T value)
    {
        if (value is null)
        {
            // Track this as a strong reference so we know 'Equals' should only look at values originally null.
            return Strong(value);
        }

        return new ReferenceHolder<T>(new WeakReference<T>(value), ReferenceEqualityComparer.GetHashCode(value));
    }

    public T? TryGetTarget()
    {
        if (_weakReference is object)
            return _weakReference.GetTarget();

        return _strongReference;
    }

    public override bool Equals(object? obj)
    {
        return obj is ReferenceHolder<T> other
            && Equals(other);
    }

    public bool Equals(ReferenceHolder<T> other)
    {
        var x = TryGetTarget();
        var y = other.TryGetTarget();
        if (x is null)
        {
            if (_weakReference is object)
            {
                // 'x' is a weak reference that was collected. Verify 'y' is a collected weak reference with the
                // same runtime hash code. This code path can fail in an edge case where the references to two
                // different objects have both been collected, but the runtime hash codes for the objects were
                // equal. Callers can ensure this case is not encountered by structuring equality checks such that
                // at least one of the objects is alive at the time Equals is called.
                return y is null && other._weakReference is object && _hashCode == other._hashCode;
            }
            else
            {
                // Null values are equal iff both were originally references to null.
                return y is null && other._weakReference is null;
            }
        }

        // Intentional reference equality check
        return x == y;
    }

    public override int GetHashCode()
    {
        if (_weakReference is object)
            return _hashCode;

        return ReferenceEqualityComparer.GetHashCode(_strongReference);
    }

    internal static class TestAccessor
    {
        /// <summary>
        /// Creates a <see cref="ReferenceHolder{T}"/> for a weakly-held reference that has since been collected.
        /// </summary>
        /// <param name="hashCode">The hash code of the collected value.</param>
        /// <returns>A weak <see cref="ReferenceHolder{T}"/> which was already collected.</returns>
        public static ReferenceHolder<T> ReleasedWeak(int hashCode)
            => new(new WeakReference<T>(null!), hashCode);
    }
}
