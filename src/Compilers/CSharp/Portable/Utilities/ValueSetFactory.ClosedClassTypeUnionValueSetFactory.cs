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

            private ImmutableArray<TypeSymbol> ClosedSubtypes()
            {
                // Ensure the set consists of the 'leaf nodes' of a closed type hierarchy.
                // This makes it easier to filter the value set based on the tests being performed.
                // TODO2: it's not clear to me if this should be walking thru union case types similarly.
                var builder = ArrayBuilder<TypeSymbol>.GetInstance();
                var toTraverse = ArrayBuilder<NamedTypeSymbol>.GetInstance();
                toTraverse.AddRange(_closedClass.ClosedSubtypes);
                while (!toTraverse.IsEmpty)
                {
                    var subtype = toTraverse.Pop();
                    if (!subtype.IsClosed)
                    {
                        builder.Add(subtype);
                        continue;
                    }

                    toTraverse.AddRange(subtype.ClosedSubtypes);
                }

                toTraverse.Free();
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
