// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Substitutes all occurrences of dynamic type with Object type.
    /// </summary>
    internal sealed class DynamicTypeEraser : AbstractTypeMap
    {
        private readonly TypeSymbol _objectType;

        public DynamicTypeEraser(TypeSymbol objectType)
        {
            Debug.Assert((object)objectType != null);
            _objectType = objectType;
        }

        public TypeSymbol EraseDynamic(TypeSymbol type)
        {
            return SubstituteType(type).AsTypeSymbolOnly();
        }

        protected override TypeSymbol SubstituteDynamicType()
        {
            return _objectType;
        }
    }
}
