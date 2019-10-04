// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal class CodeGenerationTypeParameterSymbol : CodeGenerationTypeSymbol, ITypeParameterSymbol
    {
        public VarianceKind Variance { get; }
        public ImmutableArray<ITypeSymbol> ConstraintTypes { get; internal set; }
        public bool HasConstructorConstraint { get; }
        public bool HasReferenceTypeConstraint { get; }
        public bool HasValueTypeConstraint { get; }
        public bool HasUnmanagedTypeConstraint { get; }
        public bool HasNotNullConstraint { get; }
        public int Ordinal { get; }

        public CodeGenerationTypeParameterSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            VarianceKind varianceKind,
            string name,
            ImmutableArray<ITypeSymbol> constraintTypes,
            bool hasConstructorConstraint,
            bool hasReferenceConstraint,
            bool hasValueConstraint,
            bool hasUnmanagedConstraint,
            bool hasNotNullConstraint,
            int ordinal)
            : base(containingType, attributes, Accessibility.NotApplicable, default, name, SpecialType.None)
        {
            this.Variance = varianceKind;
            this.ConstraintTypes = constraintTypes;
            this.Ordinal = ordinal;
            this.HasConstructorConstraint = hasConstructorConstraint;
            this.HasReferenceTypeConstraint = hasReferenceConstraint;
            this.HasValueTypeConstraint = hasValueConstraint;
            this.HasUnmanagedTypeConstraint = hasUnmanagedConstraint;
            this.HasNotNullConstraint = hasNotNullConstraint;
        }

        protected override CodeGenerationSymbol Clone()
        {
            return new CodeGenerationTypeParameterSymbol(
                this.ContainingType, this.GetAttributes(), this.Variance, this.Name,
                this.ConstraintTypes, this.HasConstructorConstraint, this.HasReferenceTypeConstraint,
                this.HasValueTypeConstraint, this.HasUnmanagedTypeConstraint, this.HasNotNullConstraint, this.Ordinal);
        }

        public new ITypeParameterSymbol OriginalDefinition => this;

        public ITypeParameterSymbol? ReducedFrom => null;

        public override SymbolKind Kind => SymbolKind.TypeParameter;

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitTypeParameter(this);
        }

        [return: MaybeNull]
        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return visitor.VisitTypeParameter(this);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        public override TypeKind TypeKind => TypeKind.TypeParameter;

        public TypeParameterKind TypeParameterKind
        {
            get
            {
                return this.DeclaringMethod != null
                    ? TypeParameterKind.Method
                    : TypeParameterKind.Type;
            }
        }

        public IMethodSymbol? DeclaringMethod
        {
            get
            {
                return this.ContainingSymbol as IMethodSymbol;
            }
        }

        public INamedTypeSymbol? DeclaringType
        {
            get
            {
                return this.ContainingSymbol as INamedTypeSymbol;
            }
        }

        public NullableAnnotation ReferenceTypeConstraintNullableAnnotation => throw new System.NotImplementedException();

        public ImmutableArray<NullableAnnotation> ConstraintNullableAnnotations => ConstraintTypes.SelectAsArray(t => t.GetNullability());
    }
}
