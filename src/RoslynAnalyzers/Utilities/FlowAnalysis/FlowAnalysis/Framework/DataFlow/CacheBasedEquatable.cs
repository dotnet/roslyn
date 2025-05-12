// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Analyzer.Utilities;

#pragma warning disable CA2002

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Abstract cache based equatable implementation for objects that are compared frequently and hence need a performance optimization of using a cached hash code.
    /// </summary>
    public abstract class CacheBasedEquatable<T> : IEquatable<T?>
        where T : class
    {
        private int _lazyHashCode;

        protected CacheBasedEquatable()
        {
        }

        private int GetOrComputeHashCode()
        {
            if (_lazyHashCode == 0)
            {
                var hashCode = new RoslynHashCode();
                ComputeHashCodeParts(ref hashCode);
                var result = hashCode.ToHashCode();
                Interlocked.CompareExchange(ref _lazyHashCode, result, 0);
            }

            return _lazyHashCode;
        }

        protected abstract void ComputeHashCodeParts(ref RoslynHashCode hashCode);

        protected abstract bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<T> obj);

        public sealed override int GetHashCode() => GetOrComputeHashCode();

        public sealed override bool Equals(object? obj) => Equals(obj as T);
        public bool Equals(T? other)
        {
            // Perform fast equality checks first.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            var otherEquatable = other as CacheBasedEquatable<T>;
            if (otherEquatable == null
                || GetType() != otherEquatable.GetType()
                || GetHashCode() != otherEquatable.GetHashCode())
            {
                return false;
            }

            // Now perform slow check that compares individual hash code parts sequences.
            return ComputeEqualsByHashCodeParts(otherEquatable);
        }

        public static bool operator ==(CacheBasedEquatable<T>? value1, CacheBasedEquatable<T>? value2)
        {
            if (value1 is null)
            {
                return value2 is null;
            }
            else if (value2 is null)
            {
                return false;
            }

            return value1.Equals(value2);
        }

        public static bool operator !=(CacheBasedEquatable<T>? value1, CacheBasedEquatable<T>? value2)
        {
            return !(value1 == value2);
        }
    }
}
