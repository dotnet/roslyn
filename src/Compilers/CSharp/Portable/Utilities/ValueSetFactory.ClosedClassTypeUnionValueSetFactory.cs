// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
            internal static void ExpandClosedSubtypes(TypeSymbol possibleClosedClass, ArrayBuilder<TypeSymbol> builder)
            {
                // PROTOTYPE(cc): We should not require non-empty 'ClosedSubtypes' here.
                // i.e. a closed class with no subtypes, should probably expand into an empty type set.
                // However, if we produce an empty type set as a result, we fail the assertion at 'TypeUnionValueSet..ctor' via 'SamplePatternForTemp().tryHandleTypeUnionLimits()'.
                if (possibleClosedClass is not NamedTypeSymbol { IsClosed: true, ClosedSubtypes: [_, ..] subtypes })
                {
                    builder.Add(possibleClosedClass);
                    return;
                }

                var toVisit = ArrayBuilder<NamedTypeSymbol>.GetInstance();
                toVisit.AddRange(subtypes);
                while (!toVisit.IsEmpty)
                {
                    var subtype = toVisit.Pop();
                    if (!subtype.IsClosed || subtype.ClosedSubtypes.IsEmpty)
                    {
                        builder.Add(subtype);
                        continue;
                    }

                    toVisit.AddRange(subtype.ClosedSubtypes);
                }

                toVisit.Free();
            }

            private ImmutableArray<TypeSymbol> ClosedSubtypes()
            {
                var builder = ArrayBuilder<TypeSymbol>.GetInstance();
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
