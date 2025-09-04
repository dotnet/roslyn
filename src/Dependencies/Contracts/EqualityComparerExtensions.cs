// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

#if !NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace System.Collections.Generic;

internal static class RoslynEqualityComparerExtensions
{
#if NET8_0_OR_GREATER

    // for binary compatibility
    public static EqualityComparer<T> Create<T>(Func<T?, T?, bool> equals, Func<T, int>? getHashCode = null)
        => EqualityComparer<T>.Create(equals, getHashCode);

#else
    // Implementation based on https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/EqualityComparer.cs

    extension<T>(EqualityComparer<T>)
    {
        /// <summary>
        /// Creates an <see cref="EqualityComparer{T}"/> by using the specified delegates as the implementation of the comparer's
        /// <see cref="EqualityComparer{T}.Equals"/> and <see cref="EqualityComparer{T}.GetHashCode"/> methods.
        /// </summary>
        /// <param name="equals">The delegate to use to implement the <see cref="EqualityComparer{T}.Equals"/> method.</param>
        /// <param name="getHashCode">
        /// The delegate to use to implement the <see cref="EqualityComparer{T}.GetHashCode"/> method.
        /// If no delegate is supplied, calls to the resulting comparer's <see cref="EqualityComparer{T}.GetHashCode"/>
        /// will throw <see cref="NotSupportedException"/>.
        /// </param>
        /// <returns>The new comparer.</returns>
        public static EqualityComparer<T> Create(Func<T?, T?, bool> equals, Func<T, int>? getHashCode = null)
        {
            getHashCode ??= _ => throw new NotSupportedException();
            return new DelegateEqualityComparer<T>(equals, getHashCode);
        }
    }

    private sealed class DelegateEqualityComparer<T>(Func<T?, T?, bool> equals, Func<T, int> getHashCode) : EqualityComparer<T>
    {
        private readonly Func<T?, T?, bool> _equals = equals;
        private readonly Func<T, int> _getHashCode = getHashCode;

        public override bool Equals(T? x, T? y) =>
            _equals(x, y);

        public override int GetHashCode(T obj) =>
            _getHashCode(obj);

        public override bool Equals(object? obj)
            => obj is DelegateEqualityComparer<T> other && _equals == other._equals && _getHashCode == other._getHashCode;

        public override int GetHashCode()
            => unchecked(_equals.GetHashCode() * (int)0xA5555529 + _getHashCode.GetHashCode());
    }
#endif
}

