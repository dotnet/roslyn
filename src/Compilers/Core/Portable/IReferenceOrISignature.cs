// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used to devirtualize Dictionary/HashSet for EqualityComparer{T}.Default
    /// </summary>
    internal readonly struct IReferenceOrISignatureEquivalent : IEquatable<IReferenceOrISignatureEquivalent>
    {
        private readonly object? _item;

        public IReferenceOrISignatureEquivalent(IReference item)
        {
            _item = item;
        }

        public IReferenceOrISignatureEquivalent(ISignature item)
        {
            _item = item;
        }

        public IReferenceOrISignatureEquivalent(IMethodReference item)
        {
            _item = item;
        }

        public bool Equals(IReferenceOrISignatureEquivalent other)
        {
            object? x = _item;
            object? y = other._item;
            if (x is null)
            {
                return y is null;
            }
            else if (ReferenceEquals(x, y))
            {
                return true;
            }

            return Equals(x, y);
        }

        private new static bool Equals(object? x, object? y)
        {
            if (x is null)
            {
                return y is null;
            }
            else if (ReferenceEquals(x, y))
            {
                return true;
            }
            else if (x is ISymbolInternal sx && y is ISymbolInternal sy)
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

        public override bool Equals(object? obj)
        {
            return obj is IReferenceOrISignatureEquivalent refOrSig ?
                Equals(refOrSig) :
                Equals(_item, obj);
        }

        public override int GetHashCode() => _item?.GetHashCode() ?? 0;

        public override string ToString() => _item?.ToString() ?? "null";

        internal object AsObject() => _item!;
    }

    /// <summary>
    /// Used to devirtualize ConcurrentDictionary for EqualityComparer{T}.Default and ReferenceEquals
    /// </summary>
    internal readonly struct IReferenceOrISignature : IEquatable<IReferenceOrISignature>
    {
        private readonly object? _item;

        public static implicit operator IReferenceOrISignature(IReferenceOrISignatureEquivalent d) => new IReferenceOrISignature(d.AsObject());

        private IReferenceOrISignature(object? item)
        {
            _item = item;
        }

        public IReferenceOrISignature(IReference item)
        {
            _item = item;
        }

        public IReferenceOrISignature(ISignature item)
        {
            _item = item;
        }

        public bool Equals(IReferenceOrISignature other)
        {
            object? x = _item;
            object? y = other._item;

            return ReferenceEquals(x, y);
        }

        public override bool Equals(object? obj)
        {
            return obj is IReferenceOrISignature refOrSig ?
                Equals(refOrSig) :
                ReferenceEquals(_item, obj);
        }

        public override int GetHashCode() => _item?.GetHashCode() ?? 0;

        public override string ToString() => _item?.ToString() ?? "null";
    }
}
