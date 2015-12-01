// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            public override bool HasValueTypeConstraint
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

            internal override void EnsureAllConstraintsAreResolved()
            {
            }

            internal override ImmutableArray<TypeSymbolWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
            {
                return ImmutableArray<TypeSymbolWithAnnotations>.Empty;
            }

            internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
            {
                return null;
            }

            internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
            {
                return null;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_container.GetHashCode(), _ordinal);
            }

            internal override bool Equals(TypeSymbol t2, TypeSymbolEqualityOptions options)
            {
                if (ReferenceEquals(this, t2))
                {
                    return true;
                }

                var other = t2 as ErrorTypeParameterSymbol;
                return (object)other != null &&
                    other._ordinal == _ordinal &&
                    other.ContainingType.Equals(this.ContainingType, options);
            }
        }
    }
}
