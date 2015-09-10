// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
