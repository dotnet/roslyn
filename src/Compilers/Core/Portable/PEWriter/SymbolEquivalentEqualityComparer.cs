// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

            var xSymbol = x.GetInternalSymbol();
            var ySymbol = y.GetInternalSymbol();

            if (xSymbol is object && ySymbol is object)
            {
                return xSymbol.Equals(ySymbol);
            }

            return false;
        }

        public int GetHashCode(IReference? obj)
        {
            var objSymbol = obj?.GetInternalSymbol();

            if (objSymbol is object)
            {
                return objSymbol.GetHashCode();
            }

            return RuntimeHelpers.GetHashCode(obj);
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

            var xSymbol = x.GetInternalSymbol();
            var ySymbol = y.GetInternalSymbol();

            if (xSymbol is object && ySymbol is object)
            {
                return xSymbol.Equals(ySymbol);
            }

            return false;
        }

        public int GetHashCode(INamespace? obj)
        {
            var objSymbol = obj?.GetInternalSymbol();

            if (objSymbol is object)
            {
                return objSymbol.GetHashCode();
            }

            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
