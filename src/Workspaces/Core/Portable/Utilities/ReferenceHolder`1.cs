// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    internal readonly struct ReferenceHolder<T> : IEquatable<ReferenceHolder<T>>
        where T : class?
    {
        [AllowNull, MaybeNull]
        private readonly T _strongReference;
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
            => new ReferenceHolder<T>(value);

        public static ReferenceHolder<T> Weak(T value)
        {
            if (value is null)
            {
                // Track this as a strong reference so we know 'Equals' should only look at values originally null.
                return Strong(value);
            }

            return new ReferenceHolder<T>(new WeakReference<T>(value), RuntimeHelpers.GetHashCode(value));
        }

        [return: MaybeNull]
        public T TryGetTarget()
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
                    // same runtime hash code.
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

            // RuntimeHelpers.GetHashCode allows null arguments
            return RuntimeHelpers.GetHashCode(_strongReference!);
        }

        internal TestAccessor GetTestAccessor()
        {
            return new TestAccessor(this);
        }

        internal readonly struct TestAccessor
        {
#pragma warning disable IDE0052 // Remove unread private members
            private readonly ReferenceHolder<T> _referenceHolder;
#pragma warning restore IDE0052 // Remove unread private members

            internal TestAccessor(ReferenceHolder<T> referenceHolder)
            {
                _referenceHolder = referenceHolder;
            }

            /// <summary>
            /// Creates a <see cref="ReferenceHolder{T}"/> for a weakly-held reference that has since been collected.
            /// </summary>
            /// <param name="hashCode">The hash code of the collected value.</param>
            /// <returns>A weak <see cref="ReferenceHolder{T}"/> which was already collected.</returns>
            public static ReferenceHolder<T> ReleasedWeak(int hashCode)
                => new ReferenceHolder<T>(new WeakReference<T>(null!), hashCode);
        }
    }
}
