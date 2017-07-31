// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class ConversionsBase
    {
        private static TypeSymbol GetUnderlyingEffectiveType(TypeSymbol type, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Spec 6.4.4: User-defined implicit conversions 
            // Spec 6.4.5: User-defined explicit conversions 
            // 
            // Determine the types S0 and T0. 
            //   * If S or T are nullable types, let Su and Tu be their underlying types, otherwise let Su and Tu be S and T, respectively. 
            //   * If Su or Tu are type parameters, S0 and T0 are their effective base types, otherwise S0 and T0 are equal to Su and Tu, respectively.

            if ((object)type != null)
            {
                type = type.StrippedType();

                if (type.IsTypeParameter())
                {
                    type = ((TypeParameterSymbol)type).EffectiveBaseClass(ref useSiteDiagnostics);
                }
            }

            return type;
        }

        public static void AddTypesParticipatingInUserDefinedConversion(ArrayBuilder<NamedTypeSymbol> result, TypeSymbol type, bool includeBaseTypes, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if ((object)type == null)
            {
                return;
            }

            // CONSIDER: These sets are usually small; if they are large then this is an O(n^2)
            // CONSIDER: algorithm. We could use a hash table instead to build up the set.

            Debug.Assert(!type.IsTypeParameter());

            // optimization:
            bool excludeExisting = result.Count > 0;

            if (type.IsClassType() || type.IsStructType())
            {
                var namedType = (NamedTypeSymbol)type;
                if (!excludeExisting || !HasIdentityConversionToAny(namedType, result))
                {
                    result.Add(namedType);
                }
            }

            if (!includeBaseTypes)
            {
                return;
            }

            NamedTypeSymbol t = type.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            while ((object)t != null)
            {
                if (!excludeExisting || !HasIdentityConversionToAny(t, result))
                {
                    result.Add(t);
                }

                t = t.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            }
        }
    }
}
