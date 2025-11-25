// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        private sealed class UnionTypeTypeUnionValueSetFactory : ITypeUnionValueSetFactory
        {
            private readonly NamedTypeSymbol _unionType;

            public UnionTypeTypeUnionValueSetFactory(NamedTypeSymbol unionType)
            {
                Debug.Assert(unionType is NamedTypeSymbol { IsUnionTypeNoUseSiteDiagnostics: true });
                _unionType = unionType;
            }

            private ImmutableArray<TypeSymbol> AdjustedTypesInUnion()
            {
                return _unionType.UnionCaseTypes.SelectAsArray(TypeSymbolExtensions.StrippedType);
            }

            public TypeUnionValueSet AllValues(ConversionsBase conversions)
            {
                return TypeUnionValueSet.AllValues(AdjustedTypesInUnion(), conversions);
            }

            public TypeUnionValueSet FromTypeMatch(TypeSymbol type, ConversionsBase conversions, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                return TypeUnionValueSet.FromTypeMatch(AdjustedTypesInUnion(), type, conversions, ref useSiteInfo);
            }

            public TypeUnionValueSet FromNullMatch(ConversionsBase conversions)
            {
                return TypeUnionValueSet.FromNullMatch(AdjustedTypesInUnion(), conversions);
            }

            public TypeUnionValueSet FromNonNullMatch(ConversionsBase conversions)
            {
                return TypeUnionValueSet.FromNonNullMatch(AdjustedTypesInUnion(), conversions);
            }
        }
    }
}
