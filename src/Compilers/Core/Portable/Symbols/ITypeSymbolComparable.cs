using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Exposes type comparison considering compare kind as an internal api
    /// </summary>
    internal interface ITypeSymbolComparable
    {
        internal bool Equals(ITypeSymbol other, TypeCompareKind compareKind);
    }
}
