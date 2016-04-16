// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract class AbstractTypeParameterMap : AbstractTypeMap
    {
        protected readonly SmallDictionary<TypeParameterSymbol, TypeWithModifiers> Mapping;

        protected AbstractTypeParameterMap(SmallDictionary<TypeParameterSymbol, TypeWithModifiers> mapping)
        {
            this.Mapping = mapping;
        }

        protected sealed override TypeWithModifiers SubstituteTypeParameter(TypeParameterSymbol typeParameter)
        {
            // It might need to be substituted directly.
            TypeWithModifiers result;
            if (Mapping.TryGetValue(typeParameter, out result))
            {
                return result;
            }

            return new TypeWithModifiers(typeParameter);
        }

        private string GetDebuggerDisplay()
        {
            var result = new StringBuilder("[");
            result.Append(this.GetType().Name);
            foreach (var kv in Mapping)
            {
                result.Append(" ").Append(kv.Key).Append(":").Append(kv.Value.Type);
            }

            return result.Append("]").ToString();
        }
    }
}
