// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static ITypeParameterSymbol TypeParameter(
            string name,
            VarianceKind variance,
            bool hasReferenceTypeConstraint,
            bool hasValueTypeConstraint,
            bool hasUnmanagedTypeConstraint,
            bool hasNotNullConstraint,
            bool hasConstructorConstraint,
            ImmutableArray<ITypeSymbol> constraintTypes,
            ImmutableArray<AttributeData> attributes,
            NullableAnnotation nullableAnnotation = NullableAnnotation.None)
        {
            return new TypeParameterSymbol(
                name,
                variance,
                hasReferenceTypeConstraint,
                hasValueTypeConstraint,
                hasUnmanagedTypeConstraint,
                hasNotNullConstraint,
                hasConstructorConstraint,
                constraintTypes,
                attributes,
                nullableAnnotation);
        }

        public static ITypeParameterSymbol With(
            this ITypeParameterSymbol typeParameter,
            Optional<string> name = default,
            Optional<VarianceKind> variance = default,
            Optional<bool> hasReferenceTypeConstraint = default,
            Optional<bool> hasValueTypeConstraint = default,
            Optional<bool> hasUnmanagedTypeConstraint = default,
            Optional<bool> hasNotNullConstraint = default,
            Optional<bool> hasConstructorConstraint = default,
            Optional<ImmutableArray<ITypeSymbol>> constraintTypes = default,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<NullableAnnotation> nullableAnnotation = default)
        {
            return new TypeParameterSymbol(
                name.GetValueOr(typeParameter.Name),
                variance.GetValueOr(typeParameter.Variance),
                hasReferenceTypeConstraint.GetValueOr(typeParameter.HasReferenceTypeConstraint),
                hasValueTypeConstraint.GetValueOr(typeParameter.HasValueTypeConstraint),
                hasUnmanagedTypeConstraint.GetValueOr(typeParameter.HasUnmanagedTypeConstraint),
                hasNotNullConstraint.GetValueOr(typeParameter.HasNotNullConstraint),
                hasConstructorConstraint.GetValueOr(typeParameter.HasConstructorConstraint),
                constraintTypes.GetValueOr(typeParameter.ConstraintTypes),
                attributes.GetValueOr(typeParameter.GetAttributes()),
                nullableAnnotation.GetValueOr(typeParameter.NullableAnnotation));
        }

        private class TypeParameterSymbol : TypeSymbol, ITypeParameterSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public TypeParameterSymbol(
                string name,
                VarianceKind variance,
                bool hasReferenceTypeConstraint,
                bool hasValueTypeConstraint,
                bool hasUnmanagedTypeConstraint,
                bool hasNotNullConstraint,
                bool hasConstructorConstraint,
                ImmutableArray<ITypeSymbol> constraintTypes,
                ImmutableArray<AttributeData> attributes,
                NullableAnnotation nullableAnnotation)
            {
                Name = name;
                Variance = variance;
                HasReferenceTypeConstraint = hasReferenceTypeConstraint;
                HasValueTypeConstraint = hasValueTypeConstraint;
                HasUnmanagedTypeConstraint = hasUnmanagedTypeConstraint;
                HasNotNullConstraint = hasNotNullConstraint;
                HasConstructorConstraint = hasConstructorConstraint;
                ConstraintTypes = constraintTypes;
                _attributes = attributes;
                NullableAnnotation = nullableAnnotation;
            }

            public override SymbolKind Kind => SymbolKind.TypeParameter;
            public override string Name { get; }
            public override NullableAnnotation NullableAnnotation { get; }
            public override TypeKind TypeKind => TypeKind.TypeParameter;

            public VarianceKind Variance { get; }
            public bool HasReferenceTypeConstraint { get; }
            public bool HasValueTypeConstraint { get; }
            public bool HasUnmanagedTypeConstraint { get; }
            public bool HasNotNullConstraint { get; }
            public bool HasConstructorConstraint { get; }
            public ImmutableArray<ITypeSymbol> ConstraintTypes { get; }

            public override ImmutableArray<AttributeData> GetAttributes()
                => _attributes;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitTypeParameter(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitTypeParameter(this);

            #region default implementation

            public int Ordinal => throw new NotImplementedException();
            public TypeParameterKind TypeParameterKind => throw new NotImplementedException();
            public IMethodSymbol DeclaringMethod => throw new NotImplementedException();
            public INamedTypeSymbol DeclaringType => throw new NotImplementedException();
            public NullableAnnotation ReferenceTypeConstraintNullableAnnotation => throw new NotImplementedException();
            public ImmutableArray<NullableAnnotation> ConstraintNullableAnnotations => throw new NotImplementedException();
            public ITypeParameterSymbol ReducedFrom => throw new NotImplementedException();
            ITypeParameterSymbol ITypeParameterSymbol.OriginalDefinition => throw new NotImplementedException();

            #endregion
        }
    }
}
