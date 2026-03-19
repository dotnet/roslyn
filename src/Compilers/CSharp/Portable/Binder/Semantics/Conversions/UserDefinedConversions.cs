// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class ConversionsBase
    {
        public static void AddTypesParticipatingInUserDefinedConversion(ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol? ConstrainedToTypeOpt)> result, TypeSymbol type, bool includeBaseTypes, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            // CONSIDER: These sets are usually small; if they are large then this is an O(n^2)
            // CONSIDER: algorithm. We could use a hash table instead to build up the set.

            /* Spec 6.4.4: User-defined implicit conversions 
              
               - Determine the types `S`, `S0` and `T0`.
                 - If `E` has a type, let `S` be that type.
                 - If `S` or `T` are nullable value types, let `Si` and `Ti` be their underlying types,
                   otherwise let `Si` and `Ti` be `S` and `T`, respectively.
                 - If `Si` or `Ti` are type parameters, let `S0` and `T0` be their effective base classes,
                   otherwise let `S0` and `T0` be `Si` and `Ti`, respectively.
               - Find the set of applicable user-defined and lifted conversion operators, `U`. 
                 - Find the set of types, `D1`, from which user-defined conversion operators will be considered.
                   This set consists of `S0` (if `S0` is a class or struct), the base classes of `S0` (if `S0` is a class),
                   and `T0` (if `T0` is a class or struct).
                 - Find the set of applicable user-defined and lifted conversion operators, `U1`.
                   This set consists of the user-defined and lifted implicit conversion operators declared by the classes or 
                   structs in `D1` that convert from a type encompassing `S` to a type encompassed by `T`.
                 - If `U1` is not empty, then `U` is `U1`. Otherwise,
                   - Find the set of types, `D2`, from which user-defined conversion operators will be considered.
                     This set consists of `Si` *effective interface set* and their base interfaces (if `Si` is a type parameter),
                     and `Ti` *effective interface set* (if `Ti` is a type parameter).
                   - Find the set of applicable user-defined and lifted conversion operators, `U2`.
                     This set consists of the user-defined and lifted implicit conversion operators declared by the interfaces
                     Sin `D2` that convert from a type encompassing `S` to a type encompassed by `T`.
                   - If `U2` is not empty, then `U` is `U2`
               - If `U` is empty, the conversion is undefined and a compile-time error occurs.

            */

            /* Spec 6.4.5: User-defined explicit conversions 

               - Determine the types `S`, `S0` and `T0`.
                 - If `E` has a type, let `S` be that type.
                 - If `S` or `T` are nullable value types, let `Si` and `Ti` be their underlying types,
                   otherwise let `Si` and `Ti` be `S` and `T`, respectively.
                 - If `Si` or `Ti` are type parameters, let `S0` and `T0` be their effective base classes,
                   otherwise let `S0` and `T0` be `Si` and `Ti`, respectively.
               - Find the set of applicable user-defined and lifted conversion operators, `U`.
                 - Find the set of types, `D1`, from which user-defined conversion operators will be considered.
                   This set consists of `S0` (if `S0` is a class or struct), the base classes of `S0` (if `S0` is a class),
                   `T0` (if `T0` is a class or struct), and the base classes of `T0` (if `T0` is a class).
                 - Find the set of applicable user-defined and lifted conversion operators, `U1`.
                   This set consists of the user-defined and lifted implicit or explicit conversion operators declared by the classes or
                   structs in `D1` that convert from a type encompassing or encompassed by `S` to a type encompassing or encompassed by `T`.
                 - If `U1` is not empty, then `U` is `U1`. Otherwise,
                   - Find the set of types, `D2`, from which user-defined conversion operators will be considered.
                     This set consists of `Si` *effective interface set* and their base interfaces (if `Si` is a type parameter),
                     and `Ti` *effective interface set* and their base interfaces (if `Ti` is a type parameter).
                   - Find the set of applicable user-defined and lifted conversion operators, `U2`.
                     This set consists of the user-defined and lifted implicit or explicit conversion operators declared by the interfaces
                     in `D2` that convert from a type encompassing or encompassed by `S` to a type encompassing or encompassed by `T`.
                   - If `U2` is not empty, then `U` is `U2`
               - If `U` is empty, the conversion is undefined and a compile-time error occurs.

            */

            // Note, in both cases, specification requires us to build two distinct sets of types, `D1` and `D2`.
            // `D1` contains only classes and structures, `D2` contains only interfaces.
            // However, we are going to put both, interfaces and non-interfaces, in a single set. 
            // Consumers will separate the types as appropriate because the sets cannot contain the same types
            // and interfaces can be easily identified.

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

                ImmutableArray<NamedTypeSymbol> interfaces = includeBaseTypes ?
                    typeParameter.AllEffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo) :
                    typeParameter.EffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo);

                foreach (NamedTypeSymbol iface in interfaces)
                {
                    if (!excludeExisting || !HasIdentityConversionToAny(iface, result))
                    {
                        result.Add((iface, typeParameter));
                    }
                }
            }
            else
            {
                addFromClassOrStruct(result, excludeExisting, type, includeBaseTypes, ref useSiteInfo);
            }

            static void addFromClassOrStruct(ArrayBuilder<(NamedTypeSymbol ParticipatingType, TypeParameterSymbol? ConstrainedToTypeOpt)> result, bool excludeExisting, TypeSymbol type, bool includeBaseTypes, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                if (type.IsClassType() || type.IsStructType())
                {
                    var namedType = (NamedTypeSymbol)type;
                    if (!excludeExisting || !HasIdentityConversionToAny(namedType, result))
                    {
                        result.Add((namedType, null));
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
                        result.Add((t, null));
                    }

                    t = t.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
                }
            }
        }
    }
}
