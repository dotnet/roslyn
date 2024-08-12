// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents an anonymous type template's type parameter.
        /// </summary>
        internal sealed class AnonymousTypeParameterSymbol : TypeParameterSymbol
        {
            private readonly AnonymousTypeOrDelegateTemplateSymbol _container;
            private readonly int _ordinal;
            private readonly string _name;
            private readonly Lazy<bool> _LazyAllowsRefLikeType;

            public AnonymousTypeParameterSymbol(AnonymousTypeOrDelegateTemplateSymbol container, int ordinal, string name)
            {
                Debug.Assert((object)container != null);
                Debug.Assert(!string.IsNullOrEmpty(name));

                _container = container;
                _ordinal = ordinal;
                _name = name;
                _LazyAllowsRefLikeType = new(DoGetAllowsRefLikeType);
            }

            public override TypeParameterKind TypeParameterKind
            {
                get
                {
                    return TypeParameterKind.Type;
                }
            }

            public override ImmutableArray<Location> Locations
            {
                get { return ImmutableArray<Location>.Empty; }
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return ImmutableArray<SyntaxReference>.Empty;
                }
            }

            public override int Ordinal
            {
                get { return _ordinal; }
            }

            public override string Name
            {
                get { return _name; }
            }

            public override bool HasConstructorConstraint
            {
                get { return false; }
            }

            public override bool HasReferenceTypeConstraint
            {
                get { return false; }
            }

            public override bool IsReferenceTypeFromConstraintTypes
            {
                get { return false; }
            }

            internal override bool? ReferenceTypeConstraintIsNullable
            {
                get { return false; }
            }

            public override bool HasNotNullConstraint => false;

            internal override bool? IsNotNullable => null;

            public override bool HasValueTypeConstraint
            {
                get { return false; }
            }

            public override bool AllowsRefLikeType
            {
                get
                {
                    return _LazyAllowsRefLikeType.Value;
                }
            }

            private bool DoGetAllowsRefLikeType()
            {
                if (_container.IsDelegateType() && ContainingAssembly.RuntimeSupportsByRefLikeGenerics)
                {
                    //return false if the type parameter is used as params array
                    var visitor = new AllowsRefLikeTypeSymbolVisitor()
                    {
                        TypeParameter = this.GetPublicSymbol()
                    };
                    visitor.Visit(_container.GetMembers("Invoke")[0].GetPublicSymbol());
                    return visitor.Result;
                }
                return false;
            }

            public override bool IsValueTypeFromConstraintTypes
            {
                get { return false; }
            }

            public override bool HasUnmanagedTypeConstraint
            {
                get { return false; }
            }

            public override bool IsImplicitlyDeclared
            {
                get { return true; }
            }

            public override VarianceKind Variance
            {
                get { return VarianceKind.None; }
            }

            internal override void EnsureAllConstraintsAreResolved()
            {
            }

            internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
            {
                return ImmutableArray<TypeWithAnnotations>.Empty;
            }

            public override Symbol ContainingSymbol
            {
                get { return _container; }
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
        }
    }
}
