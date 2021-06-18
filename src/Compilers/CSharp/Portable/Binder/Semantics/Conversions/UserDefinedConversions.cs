// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        public static void AddTypesParticipatingInUserDefinedConversion(ArrayBuilder<TypeSymbol> result, TypeSymbol type, bool includeBaseTypes, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // CONSIDER: These sets are usually small; if they are large then this is an O(n^2)
            // CONSIDER: algorithm. We could use a hash table instead to build up the set.

            // Spec 6.4.4: User-defined implicit conversions 
            // Spec 6.4.5: User-defined explicit conversions 
            // 
            // Determine the types S0 and T0. 
            //   * If S or T are nullable types, let Su and Tu be their underlying types, otherwise let Su and Tu be S and T, respectively. 
            //   * If Su or Tu are type parameters, S0 and T0 are their effective base types, otherwise S0 and T0 are equal to Su and Tu, respectively.

            // Spec 6.4.4: User-defined implicit conversions
            //   Find the set of types D from which user-defined conversion operators
            //   will be considered. This set consists of S0 (if S0 is a class or struct),
            //   the base classes of S0 (if S0 is a class), and T0 (if T0 is a class or struct).
            //
            // Spec 6.4.5: User-defined explicit conversions
            //   Find the set of types, D, from which user-defined conversion operators will be considered. 
            //   This set consists of S0 (if S0 is a class or struct), the base classes of S0 (if S0 is a class),
            //   T0 (if T0 is a class or struct), and the base classes of T0 (if T0 is a class).

            // https://github.com/dotnet/roslyn/issues/53798: Adjust the above specification quote appropriately.

            if ((object)type == null)
            {
                return;
            }

            type = type.StrippedType();

            // optimization:
            bool excludeExisting = result.Count > 0;

            if (type is TypeParameterSymbol typeParameter)
            {
                NamedTypeSymbol effectiveBaseClass = typeParameter.EffectiveBaseClass(ref useSiteInfo);

                addFromClassOrStruct(result, excludeExisting, effectiveBaseClass, includeBaseTypes, ref useSiteInfo);

                switch (effectiveBaseClass.SpecialType)
                {
                    case SpecialType.System_ValueType:
                    case SpecialType.System_Enum:
                    case SpecialType.System_Array:
                    case SpecialType.System_Object:
                        if (!excludeExisting || !HasIdentityConversionToAny(typeParameter, result))
                        {
                            // Add the type parameter to the set as well. This will be treated equivalent to adding its 
                            // effective interfaces to the set. We are not doing that here because we still need to know 
                            // the originating type parameter as "constrained to" type.
                            result.Add(typeParameter);
                        }
                        break;
                }
            }
            else
            {
                addFromClassOrStruct(result, excludeExisting, type, includeBaseTypes, ref useSiteInfo);
            }

            static void addFromClassOrStruct(ArrayBuilder<TypeSymbol> result, bool excludeExisting, TypeSymbol type, bool includeBaseTypes, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                if (type.IsClassType() || type.IsStructType())
                {
                    if (!excludeExisting || !HasIdentityConversionToAny(type, result))
                    {
                        result.Add(type);
                    }
                }

                if (!includeBaseTypes)
                {
                    return;
                }

                NamedTypeSymbol t = type.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
                while ((object)t != null)
                {
                    if (!excludeExisting || !HasIdentityConversionToAny(t, result))
                    {
                        result.Add(t);
                    }

                    t = t.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
                }
            }
        }
    }
}
