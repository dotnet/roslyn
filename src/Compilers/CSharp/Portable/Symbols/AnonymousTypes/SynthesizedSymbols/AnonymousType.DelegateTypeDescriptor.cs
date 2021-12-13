// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        internal readonly struct AnonymousDelegateTypeDescriptor : IEquatable<AnonymousDelegateTypeDescriptor>
        {
            public readonly int TypeParameterCount;
            public readonly Location Location;
            public readonly ImmutableArray<AnonymousTypeField> Fields;

            internal static AnonymousDelegateTypeDescriptor FromTypeDescriptor(ImmutableArray<TypeParameterSymbol> typeParameters, AnonymousTypeDescriptor typeDescr)
            {
                int typeParameterCount = typeParameters.Length;
                if (typeParameterCount > 0)
                {
                    var typeMap = new TypeMap(typeParameters, IndexedTypeParameterSymbol.Take(typeParameterCount), allowAlpha: true);
                    typeDescr = typeDescr.SubstituteTypes(typeMap, out _);
                }
                return new AnonymousDelegateTypeDescriptor(typeParameterCount, typeDescr);
            }

            private AnonymousDelegateTypeDescriptor(int typeParameterCount, AnonymousTypeDescriptor typeDescr)
            {
                Debug.Assert(typeDescr.Fields.All(f => f.Type.VisitType((t, _, _) => t.IsTypeParameter() && t is not IndexedTypeParameterSymbol, arg: (object?)null) is null));

                TypeParameterCount = typeParameterCount;
                Location = typeDescr.Location;
                Fields = typeDescr.Fields;
            }

            public bool Equals(AnonymousDelegateTypeDescriptor other)
            {
                return Equals(other, TypeCompareKind.ConsiderEverything);
            }

            public bool Equals(AnonymousDelegateTypeDescriptor other, TypeCompareKind comparison)
            {
                return other is { } &&
                    TypeParameterCount == other.TypeParameterCount &&
                    Fields.SequenceEqual(
                        other.Fields,
                        comparison,
                        static (x, y, comparison) => x.TypeWithAnnotations.Equals(y.TypeWithAnnotations, comparison) && x.RefKind == y.RefKind);
            }

            public override bool Equals(object? obj)
            {
                return obj is AnonymousDelegateTypeDescriptor typeDescr && this.Equals(typeDescr);
            }

            public override int GetHashCode()
            {
                int value = TypeParameterCount.GetHashCode();
                foreach (var field in Fields)
                {
                    value = Hash.Combine(value, field.Type.GetHashCode());
                }
                return value;
            }
        }
    }
}
