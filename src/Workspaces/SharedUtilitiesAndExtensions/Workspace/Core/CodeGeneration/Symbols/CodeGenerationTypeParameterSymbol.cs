// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

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
            NullableAnnotation nullableAnnotation,
            ImmutableArray<ITypeSymbol> constraintTypes,
            bool hasConstructorConstraint,
            bool hasReferenceConstraint,
            bool hasValueConstraint,
            bool hasUnmanagedConstraint,
            bool hasNotNullConstraint,
            int ordinal)
            : base(containingType?.ContainingAssembly, containingType, attributes, Accessibility.NotApplicable, default, name, SpecialType.None, nullableAnnotation)
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

        protected override CodeGenerationTypeSymbol CloneWithNullableAnnotation(NullableAnnotation nullableAnnotation)
        {
            return new CodeGenerationTypeParameterSymbol(
                this.ContainingType, this.GetAttributes(), this.Variance, this.Name, nullableAnnotation,
                this.ConstraintTypes, this.HasConstructorConstraint, this.HasReferenceTypeConstraint,
                this.HasValueTypeConstraint, this.HasUnmanagedTypeConstraint, this.HasNotNullConstraint, this.Ordinal);
        }

        public new ITypeParameterSymbol OriginalDefinition => this;

        public ITypeParameterSymbol ReducedFrom => null;

        public override SymbolKind Kind => SymbolKind.TypeParameter;

        public override void Accept(SymbolVisitor visitor)
            => visitor.VisitTypeParameter(this);

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
            => visitor.VisitTypeParameter(this);

        public override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitTypeParameter(this, argument);

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

        public IMethodSymbol DeclaringMethod
        {
            get
            {
                return this.ContainingSymbol as IMethodSymbol;
            }
        }

        public INamedTypeSymbol DeclaringType
        {
            get
            {
                return this.ContainingSymbol as INamedTypeSymbol;
            }
        }

        public NullableAnnotation ReferenceTypeConstraintNullableAnnotation => throw new System.NotImplementedException();

        public ImmutableArray<NullableAnnotation> ConstraintNullableAnnotations => ConstraintTypes.SelectAsArray(t => t.NullableAnnotation);
    }
}
