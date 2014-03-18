// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Substitutes all occurances of dynamic type with Object type.
    /// </summary>
    internal sealed class DynamicTypeEraser : AbstractTypeMap
    {
        private readonly TypeSymbol objectType;

        public DynamicTypeEraser(TypeSymbol objectType)
        {
            Debug.Assert((object)objectType != null);
            this.objectType = objectType;
        }

        public TypeSymbol EraseDynamic(TypeSymbol type)
        {
            return SubstituteType(type);
        }

        protected override TypeSymbol SubstituteDynamicType()
        {
            return objectType;
        }
    }
}
