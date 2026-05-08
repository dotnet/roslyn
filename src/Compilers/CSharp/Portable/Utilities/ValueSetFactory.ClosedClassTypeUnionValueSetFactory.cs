// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            private readonly NamedTypeSymbol _closedClass;

            public ClosedClassTypeUnionValueSetFactory(NamedTypeSymbol closedClass)
            {
                Debug.Assert(closedClass is NamedTypeSymbol { IsClosed: true });
                _closedClass = closedClass;
            }

            internal static void ExpandClosedSubtypes(TypeSymbol possibleClosedClass, ArrayBuilder<TypeUnionValueSet.CaseInfo> builder)
            {
                if (possibleClosedClass is not NamedTypeSymbol namedType || !namedType.TryGetClosedSubtypes(out var subtypes) || subtypes.IsEmpty)
                {
                    AddCaseInfo(builder, possibleClosedClass, originalClosedBase: null);
                    return;
                }

                ExpandClosedSubtypesCore(subtypes, originalBase: namedType, builder);
            }

            private static void ExpandClosedSubtypesCore(ImmutableArray<NamedTypeSymbol> subtypes, NamedTypeSymbol originalBase, ArrayBuilder<TypeUnionValueSet.CaseInfo> builder)
            {
                Debug.Assert(!subtypes.IsDefaultOrEmpty);
                foreach (var subtype in subtypes)
                {
                    if (!subtype.TryGetClosedSubtypes(out var innerSubtypes) || innerSubtypes.IsEmpty)
                    {
                        AddCaseInfo(builder, subtype, originalBase);
                    }
                    else
                    {
                        ExpandClosedSubtypesCore(innerSubtypes, originalBase, builder);
                    }
                }
            }

            private static void AddCaseInfo(ArrayBuilder<TypeUnionValueSet.CaseInfo> builder, TypeSymbol caseType, NamedTypeSymbol? originalClosedBase)
            {
                // https://github.com/dotnet/roslyn/issues/83617: There may be a need to report diagnostics when "runtime-equivalent" yet distinct caseTypes flow in.
                // For example, when the caseTypes have nullability differences.
                if (!builder.Any(static (existing, caseType) => existing.CaseType.Equals(caseType, TypeCompareKind.AllIgnoreOptions), caseType))
                {
                    builder.Add(new TypeUnionValueSet.CaseInfo(caseType, originalClosedBase));
                }
            }

            private ImmutableArray<TypeUnionValueSet.CaseInfo> ClosedSubtypes()
            {
                var builder = ArrayBuilder<TypeUnionValueSet.CaseInfo>.GetInstance();
                ExpandClosedSubtypes(_closedClass, builder);
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
