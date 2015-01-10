// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly Symbol container;
            private readonly int ordinal;
            private readonly string name;

            public AnonymousTypeParameterSymbol(Symbol container, int ordinal, string name)
            {
                Debug.Assert((object)container != null);
                Debug.Assert(!string.IsNullOrEmpty(name));

                this.container = container;
                this.ordinal = ordinal;
                this.name = name;
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
                get { return this.ordinal; }
            }

            public override string Name
            {
                get { return this.name; }
            }

            public override bool HasConstructorConstraint
            {
                get { return false; }
            }

            public override bool HasReferenceTypeConstraint
            {
                get { return false; }
            }

            public override bool HasValueTypeConstraint
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

            internal override ImmutableArray<TypeSymbol> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
            {
                return ImmutableArray<TypeSymbol>.Empty;
            }

            public override Symbol ContainingSymbol
            {
                get { return this.container; }
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
