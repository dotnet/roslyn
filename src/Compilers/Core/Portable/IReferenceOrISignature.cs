// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    // These types are to enable fast-path devirtualization in the Jit. Dictionary<K, V>, HashTable<T>
    // and ConcurrentDictionary<K, V> will devirtualize (and potentially inline) the IEquatable<T>.Equals
    // method for a struct when the Comparer is unspecified in .NET Core, .NET 5; whereas specifying
    // a Comparer will make .Equals and GetHashcode slower interface calls.
#if false
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
            if ((x as Cci.IReference)?.AsSymbol is ISymbolInternal sx && (y as Cci.IReference)?.AsSymbol is ISymbolInternal sy)
            {
                //return sx.Equals(sy, TypeCompareKind.ConsiderEverything);
                throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
            }
            else if (x is ISymbolCompareKindComparableInternal cx && y is ISymbolCompareKindComparableInternal cy)
            {
                //return cx.Equals(cy, TypeCompareKind.ConsiderEverything);
                throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
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
#endif

    /// <summary>
    /// Used to devirtualize ConcurrentDictionary for EqualityComparer{T}.Default and ReferenceEquals
    /// </summary>
    internal readonly struct IReferenceOrISignature : IEquatable<IReferenceOrISignature>
    {
        private readonly object _item;

        public IReferenceOrISignature(IReference item) => _item = item;

        public IReferenceOrISignature(ISignature item) => _item = item;

        // Needed to resolve ambiguity for types that implement both IReference and ISignature
        public IReferenceOrISignature(IMethodReference item) => _item = item;

        // Used by implicit conversion
        private IReferenceOrISignature(object item) => _item = item;
#if false
        public static implicit operator IReferenceOrISignature(IReferenceOrISignatureEquivalent item)
            => new IReferenceOrISignature(item.AsObject());
#endif
        public bool Equals(IReferenceOrISignature other) => ReferenceEquals(_item, other._item);

        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(_item);

        public override string ToString() => _item.ToString() ?? "null";

        internal object AsObject() => _item;
    }
}

namespace Microsoft.Cci
{
    /// <summary>
    /// Allows for the comparison of two <see cref="IReference"/> instances or two <see cref="INamespace"/>
    /// instances based on underlying symbols, if any.
    /// </summary>
    internal sealed class SymbolEquivalentEqualityComparer : IEqualityComparer<IReference?>, IEqualityComparer<INamespace?>
    {
        public static readonly SymbolEquivalentEqualityComparer Instance = new SymbolEquivalentEqualityComparer();

        private SymbolEquivalentEqualityComparer()
        {
        }

        public bool Equals(IReference? x, IReference? y)
        {
            if (x == y)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            var xSymbol = x.AsSymbol;
            var ySymbol = y.AsSymbol;

            if (xSymbol is object && ySymbol is object)
            {
                return xSymbol.Equals(ySymbol);
            }
            else if (xSymbol is object || ySymbol is object)
            {
                return false;
            }

            return x.Equals(y);
        }

        public int GetHashCode(IReference? obj)
        {
            var objSymbol = obj?.AsSymbol;

            if (objSymbol is object)
            {
                return objSymbol.GetHashCode();
            }

            return obj?.GetHashCode() ?? 0;
        }

        public bool Equals(INamespace? x, INamespace? y)
        {
            if (x == y)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            var xSymbol = x.AsSymbol;
            var ySymbol = y.AsSymbol;

            if (xSymbol is object && ySymbol is object)
            {
                return xSymbol.Equals(ySymbol);
            }
            else if (xSymbol is object || ySymbol is object)
            {
                return false;
            }

            return x.Equals(y);
        }

        public int GetHashCode(INamespace? obj)
        {
            var objSymbol = obj?.AsSymbol;

            if (objSymbol is object)
            {
                return objSymbol.GetHashCode();
            }

            return obj?.GetHashCode() ?? 0;
        }
    }
}
