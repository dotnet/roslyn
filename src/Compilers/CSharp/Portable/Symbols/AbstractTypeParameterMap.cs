// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        protected readonly SmallDictionary<TypeParameterSymbol, TypeWithAnnotations> Mapping;

        protected AbstractTypeParameterMap(SmallDictionary<TypeParameterSymbol, TypeWithAnnotations> mapping)
        {
            this.Mapping = mapping;
        }

        protected sealed override TypeWithAnnotations SubstituteTypeParameter(TypeParameterSymbol typeParameter)
        {
            // It might need to be substituted directly.
            TypeWithAnnotations result;
            if (Mapping.TryGetValue(typeParameter, out result))
            {
                return result;
            }

            return TypeWithAnnotations.Create(typeParameter);
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
