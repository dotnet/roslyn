// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private sealed class ClosedClassTypeUnionValueSetFactory : ITypeUnionValueSetFactory
        {
            private readonly TypeSymbol _closedClassOrTypeParameter;

            public ClosedClassTypeUnionValueSetFactory(TypeSymbol closedClassOrTypeParameter)
            {
                Debug.Assert(closedClassOrTypeParameter is NamedTypeSymbol { IsClosed: true } or TypeParameterSymbol { EffectiveBaseClassNoUseSiteDiagnostics.IsClosed: true });
                _closedClassOrTypeParameter = closedClassOrTypeParameter;
            }

            internal static void ExpandClosedSubtypes(TypeSymbol possibleClosedClass, ArrayBuilder<TypeUnionValueSet.CaseInfo> builder, HashSet<TypeSymbol> setBuilder)
            {
                if (possibleClosedClass is not NamedTypeSymbol namedType || !namedType.TryGetClosedSubtypes(out var subtypes) || subtypes.IsEmpty)
                {
                    AddCaseInfo(builder, setBuilder, possibleClosedClass, originalClosedBase: null);
                    return;
                }

                ExpandClosedSubtypesCore(subtypes, originalBase: namedType, builder, setBuilder);
            }

            private static void ExpandClosedSubtypesCore(ImmutableArray<NamedTypeSymbol> subtypes, NamedTypeSymbol originalBase, ArrayBuilder<TypeUnionValueSet.CaseInfo> builder, HashSet<TypeSymbol> setBuilder)
            {
                Debug.Assert(!subtypes.IsDefaultOrEmpty);
                foreach (var subtype in subtypes)
                {
                    if (!subtype.TryGetClosedSubtypes(out var innerSubtypes) || innerSubtypes.IsEmpty)
                    {
                        AddCaseInfo(builder, setBuilder, subtype, originalBase);
                    }
                    else
                    {
                        ExpandClosedSubtypesCore(innerSubtypes, originalBase, builder, setBuilder);
                    }
                }
            }

            private static void AddCaseInfo(ArrayBuilder<TypeUnionValueSet.CaseInfo> builder, HashSet<TypeSymbol> setBuilder, TypeSymbol caseType, NamedTypeSymbol? originalClosedBase)
            {
                // https://github.com/dotnet/roslyn/issues/83617: There may be a need to report diagnostics when "runtime-equivalent" yet distinct caseTypes flow in.
                // For example, when the caseTypes have nullability differences.
                if (setBuilder.Add(caseType))
                {
                    builder.Add(new TypeUnionValueSet.CaseInfo(caseType, originalClosedBase));
                }
            }

            private ImmutableArray<TypeUnionValueSet.CaseInfo> ClosedSubtypes()
            {
                var builder = ArrayBuilder<TypeUnionValueSet.CaseInfo>.GetInstance();
                var setBuilder = TypeSymbol.AllIgnoreOptionsSetPool.Allocate();
                ExpandClosedSubtypes(_closedClassOrTypeParameter, builder, setBuilder);
                setBuilder.Free();
                return builder.ToImmutableAndFree();
            }

            public TypeUnionValueSet AllValues(ConversionsBase conversions)
            {
                return TypeUnionValueSet.AllValues(ClosedSubtypes(), conversions);
            }

            public TypeUnionValueSet FromTypeMatch(TypeSymbol type, ConversionsBase conversions, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                return TypeUnionValueSet.FromTypeMatch(ClosedSubtypes(), type, conversions, ref useSiteInfo);
            }

            public TypeUnionValueSet FromNullMatch(ConversionsBase conversions)
            {
                return TypeUnionValueSet.FromNullMatch(ClosedSubtypes(), conversions);
            }

            public TypeUnionValueSet FromNonNullMatch(ConversionsBase conversions)
            {
                return TypeUnionValueSet.FromNonNullMatch(ClosedSubtypes(), conversions);
            }
        }
    }
}
