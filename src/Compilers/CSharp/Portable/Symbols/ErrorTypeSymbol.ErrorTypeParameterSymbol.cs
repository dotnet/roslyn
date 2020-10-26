﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class ErrorTypeSymbol
    {
        protected sealed class ErrorTypeParameterSymbol : TypeParameterSymbol
        {
            private readonly ErrorTypeSymbol _container;
            private readonly string _name;
            private readonly int _ordinal;

            public ErrorTypeParameterSymbol(ErrorTypeSymbol container, string name, int ordinal)
            {
                _container = container;
                _name = name;
                _ordinal = ordinal;
            }

            public override string Name
            {
                get
                {
                    return _name;
                }
            }

            public override TypeParameterKind TypeParameterKind
            {
                get
                {
                    return TypeParameterKind.Type;
                }
            }

            public override Symbol ContainingSymbol
            {
                get
                {
                    return _container;
                }
            }

            public override bool HasConstructorConstraint
            {
                get
                {
                    return false;
                }
            }

            public override bool HasReferenceTypeConstraint
            {
                get
                {
                    return false;
                }
            }

            internal override bool? ReferenceTypeConstraintIsNullable
            {
                get
                {
                    return false;
                }
            }

            public override bool HasNotNullConstraint => false;

            internal override bool? IsNotNullable => null;

            public override bool HasValueTypeConstraint
            {
                get
                {
                    return false;
                }
            }

            public override bool HasUnmanagedTypeConstraint
            {
                get
                {
                    return false;
                }
            }

            public override int Ordinal
            {
                get
                {
                    return _ordinal;
                }
            }

            public override ImmutableArray<Location> Locations
            {
                get
                {
                    return ImmutableArray<Location>.Empty;
                }
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return ImmutableArray<SyntaxReference>.Empty;
                }
            }

            public override VarianceKind Variance
            {
                get
                {
                    return VarianceKind.None;
                }
            }

            public override bool IsImplicitlyDeclared
            {
                get
                {
                    return true;
                }
            }

            internal override void EnsureAllConstraintsAreResolved(bool canIgnoreNullableContext)
            {
            }

            internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress, bool canIgnoreNullableContext)
            {
                return ImmutableArray<TypeWithAnnotations>.Empty;
            }

            internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal override NamedTypeSymbol? GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
            {
                return null;
            }

            internal override TypeSymbol? GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
            {
                return null;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_container.GetHashCode(), _ordinal);
            }

            internal override bool Equals(TypeSymbol? t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool>? isValueTypeOverrideOpt = null)
            {
                if (ReferenceEquals(this, t2))
                {
                    return true;
                }

                var other = t2 as ErrorTypeParameterSymbol;
                return (object?)other != null &&
                    other._ordinal == _ordinal &&
                    other.ContainingType.Equals(this.ContainingType, comparison, isValueTypeOverrideOpt);
            }
        }
    }
}
