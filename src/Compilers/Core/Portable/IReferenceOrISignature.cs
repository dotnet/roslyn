// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    // These types are to enable fast-path devirtualization in the Jit. Dictionary<K, V>, HashTable<T>
    // and ConcurrentDictionary<K, V> will devirtualize (and potentially inline) the IEquatable<T>.Equals
    // method for a struct when the Comparer is unspecified in .NET Core, .NET 5; whereas specifying
    // a Comparer will make .Equals and GetHashcode slower interface calls.

    /// <summary>
    /// Used to devirtualize Dictionary/HashSet for EqualityComparer{T}.Default
    /// </summary>
    internal readonly struct IReferenceOrISignatureEquivalent : IEquatable<IReferenceOrISignatureEquivalent>
    {
        private readonly object _item;

        public IReferenceOrISignatureEquivalent(IReference item) => _item = item;

        public IReferenceOrISignatureEquivalent(ISignature item) => _item = item;

        // Needed to resolve ambiguity for types that implement both IReference and ISignature
        public IReferenceOrISignatureEquivalent(IMethodReference item) => _item = item;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(IReferenceOrISignatureEquivalent other)
        {
            // Fast inlinable ReferenceEquals
            var x = _item;
            var y = other._item;
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            return EqualsSlow(x, y);
        }

        private static bool EqualsSlow(object x, object y)
        {
            if (x is ISymbolInternal sx && y is ISymbolInternal sy)
            {
                return sx.Equals(sy, TypeCompareKind.ConsiderEverything);
            }
            else if (x is ISymbolCompareKindComparableInternal cx && y is ISymbolCompareKindComparableInternal cy)
            {
                return cx.Equals(cy, TypeCompareKind.ConsiderEverything);
            }
            else
            {
                return x.Equals(y);
            }
        }

        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => _item.GetHashCode();

        public override string ToString() => _item.ToString() ?? "null";

        internal object AsObject() => _item;
    }

    /// <summary>
    /// Used to devirtualize ConcurrentDictionary for EqualityComparer{T}.Default and ReferenceEquals
    /// </summary>
    internal readonly struct IReferenceOrISignature : IEquatable<IReferenceOrISignature>
    {
        private readonly object _item;

        public IReferenceOrISignature(IReference item) => _item = item;

        public IReferenceOrISignature(ISignature item) => _item = item;

        // Used by implicit conversion
        private IReferenceOrISignature(object item) => _item = item;

        public static implicit operator IReferenceOrISignature(IReferenceOrISignatureEquivalent item)
            => new IReferenceOrISignature(item.AsObject());

        public bool Equals(IReferenceOrISignature other) => ReferenceEquals(_item, other._item);

        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => _item.GetHashCode();

        public override string ToString() => _item.ToString() ?? "null";
    }
}
