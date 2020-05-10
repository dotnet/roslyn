// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static ITypeParameterSymbol TypeParameter(
            string name,
            ImmutableArray<AttributeData> attributes = default,
            VarianceKind variance = default,
            bool referenceTypeConstraint = default,
            bool valueTypeConstraint = default,
            bool unmanagedTypeConstraint = default,
            bool notNullConstraint = default,
            bool constructorConstraint = default,
            ImmutableArray<ITypeSymbol> constraintTypes = default,
            NullableAnnotation nullableAnnotation = NullableAnnotation.None)
        {
            return new TypeParameterSymbol(
                attributes,
                variance,
                name,
                referenceTypeConstraint,
                valueTypeConstraint,
                unmanagedTypeConstraint,
                notNullConstraint,
                constructorConstraint,
                constraintTypes,
                nullableAnnotation);
        }

        public static ITypeParameterSymbol WithAttributes(this ITypeParameterSymbol symbol, params AttributeData[] attributes)
            => WithAttributes(symbol, (IEnumerable<AttributeData>)attributes);

        public static ITypeParameterSymbol WithAttributes(this ITypeParameterSymbol symbol, IEnumerable<AttributeData> attributes)
            => WithAttributes(symbol, attributes.ToImmutableArray());

        public static ITypeParameterSymbol WithAttributes(this ITypeParameterSymbol symbol, ImmutableArray<AttributeData> attributes)
            => With(symbol, attributes: ToOptional(attributes));

        public static ITypeParameterSymbol WithVariance(this ITypeParameterSymbol symbol, VarianceKind variance)
            => With(symbol, variance: ToOptional(variance));

        public static ITypeParameterSymbol WithName(this ITypeParameterSymbol symbol, string name)
            => With(symbol, name: ToOptional(name));

        public static ITypeParameterSymbol WithReferenceTypeConstraint(this ITypeParameterSymbol symbol, bool referenceTypeConstraint)
            => With(symbol, referenceTypeConstraint: ToOptional(referenceTypeConstraint));

        public static ITypeParameterSymbol WithValueTypeConstraint(this ITypeParameterSymbol symbol, bool valueTypeConstraint)
            => With(symbol, valueTypeConstraint: ToOptional(valueTypeConstraint));

        public static ITypeParameterSymbol WithUnmanagedTypeConstraint(this ITypeParameterSymbol symbol, bool unmanagedTypeConstraint)
            => With(symbol, unmanagedTypeConstraint: ToOptional(unmanagedTypeConstraint));

        public static ITypeParameterSymbol WithNotNullConstraint(this ITypeParameterSymbol symbol, bool notNullConstraint)
            => With(symbol, notNullConstraint: ToOptional(notNullConstraint));

        public static ITypeParameterSymbol WithConstructorConstraint(this ITypeParameterSymbol symbol, bool constructorConstraint)
            => With(symbol, constructorConstraint: ToOptional(constructorConstraint));

        public static ITypeParameterSymbol WithConstraintTypes(this ITypeParameterSymbol symbol, params ITypeSymbol[] constraintTypes)
            => WithConstraintTypes(symbol, (IEnumerable<ITypeSymbol>)constraintTypes);

        public static ITypeParameterSymbol WithConstraintTypes(this ITypeParameterSymbol symbol, IEnumerable<ITypeSymbol> constraintTypes)
            => WithConstraintTypes(symbol, constraintTypes.ToImmutableArray());

        public static ITypeParameterSymbol WithConstraintTypes(this ITypeParameterSymbol symbol, ImmutableArray<ITypeSymbol> constraintTypes)
            => With(symbol, constraintTypes: ToOptional(constraintTypes));

        public static ITypeParameterSymbol WithNullableAnnotation(this ITypeParameterSymbol symbol, NullableAnnotation nullableAnnotation)
            => With(symbol, nullableAnnotation: ToOptional(nullableAnnotation));

        public static ITypeParameterSymbol With(
            this ITypeParameterSymbol typeParameter,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<VarianceKind> variance = default,
            Optional<string> name = default,
            Optional<bool> referenceTypeConstraint = default,
            Optional<bool> valueTypeConstraint = default,
            Optional<bool> unmanagedTypeConstraint = default,
            Optional<bool> notNullConstraint = default,
            Optional<bool> constructorConstraint = default,
            Optional<ImmutableArray<ITypeSymbol>> constraintTypes = default,
            Optional<NullableAnnotation> nullableAnnotation = default)
        {
            return new TypeParameterSymbol(
                attributes.GetValueOr(typeParameter.GetAttributes()),
                variance.GetValueOr(typeParameter.Variance),
                name.GetValueOr(typeParameter.Name),
                referenceTypeConstraint.GetValueOr(typeParameter.HasReferenceTypeConstraint),
                valueTypeConstraint.GetValueOr(typeParameter.HasValueTypeConstraint),
                unmanagedTypeConstraint.GetValueOr(typeParameter.HasUnmanagedTypeConstraint),
                notNullConstraint.GetValueOr(typeParameter.HasNotNullConstraint),
                constructorConstraint.GetValueOr(typeParameter.HasConstructorConstraint),
                constraintTypes.GetValueOr(typeParameter.ConstraintTypes),
                nullableAnnotation.GetValueOr(typeParameter.NullableAnnotation));
        }

        private class TypeParameterSymbol : TypeSymbol, ITypeParameterSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public TypeParameterSymbol(
                ImmutableArray<AttributeData> attributes,
                VarianceKind variance,
                string name,
                bool referenceTypeConstraint,
                bool valueTypeConstraint,
                bool unmanagedTypeConstraint,
                bool notNullConstraint,
                bool constructorConstraint,
                ImmutableArray<ITypeSymbol> constraintTypes,
                NullableAnnotation nullableAnnotation)
            {
                Name = name;
                Variance = variance;
                HasReferenceTypeConstraint = referenceTypeConstraint;
                HasValueTypeConstraint = valueTypeConstraint;
                HasUnmanagedTypeConstraint = unmanagedTypeConstraint;
                HasNotNullConstraint = notNullConstraint;
                HasConstructorConstraint = constructorConstraint;
                ConstraintTypes = constraintTypes.NullToEmpty();
                _attributes = attributes.NullToEmpty();
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

            ITypeParameterSymbol ITypeParameterSymbol.OriginalDefinition => throw new NotImplementedException();
            public IMethodSymbol DeclaringMethod => throw new NotImplementedException();
            public ImmutableArray<NullableAnnotation> ConstraintNullableAnnotations => throw new NotImplementedException();
            public INamedTypeSymbol DeclaringType => throw new NotImplementedException();
            public int Ordinal => throw new NotImplementedException();
            public ITypeParameterSymbol ReducedFrom => throw new NotImplementedException();
            public NullableAnnotation ReferenceTypeConstraintNullableAnnotation => throw new NotImplementedException();
            public TypeParameterKind TypeParameterKind => throw new NotImplementedException();

            #endregion
        }
    }
}
