// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private static IdentifierNameSyntax GenerateTypeParameterTypeSyntaxWithoutNullable(ITypeParameterSymbol symbol)
            => SyntaxFactory.IdentifierName(symbol.Name);

        private static TypeParameterListSyntax? GenerateTypeParameterList(ImmutableArray<ITypeSymbol> typeArguments)
        {
            using var _ = GetArrayBuilder<TypeParameterSyntax>(out var typeParameters);

            foreach (var typeArg in typeArguments)
                typeParameters.Add(GenerateTypeParameter(EnsureIsTypeParameter(typeArg)));

            return typeParameters.Count == 0
                ? null
                : TypeParameterList(SeparatedList(typeParameters));
        }

        private static TypeParameterSyntax GenerateTypeParameter(ITypeParameterSymbol symbol)
        {
            return TypeParameter(
                GenerateAttributeLists(symbol.GetAttributes()),
                symbol.Variance == VarianceKind.In ? Token(SyntaxKind.InKeyword) :
                symbol.Variance == VarianceKind.Out ? Token(SyntaxKind.OutKeyword) : default,
                Identifier(symbol.Name));
        }

        private static SyntaxList<TypeParameterConstraintClauseSyntax> GenerateTypeParameterConstraintClauses(
            ImmutableArray<ITypeSymbol> typeArguments)
        {
            using var _ = GetArrayBuilder<TypeParameterConstraintClauseSyntax>(out var constraintClauses);

            foreach (var typeArg in typeArguments)
                constraintClauses.AddIfNotNull(GenerateTypeParameterConstraintClause(EnsureIsTypeParameter(typeArg)));

            return List(constraintClauses);
        }

        private static TypeParameterConstraintClauseSyntax? GenerateTypeParameterConstraintClause(
            ITypeParameterSymbol symbol)
        {
            using var _ = GetArrayBuilder<TypeParameterConstraintSyntax>(out var builder);

            if (symbol.HasNotNullConstraint)
                builder.Add(TypeConstraint(ParseTypeName("notnull")));

            if (symbol.HasReferenceTypeConstraint)
                builder.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));

            if (symbol.HasUnmanagedTypeConstraint)
                builder.Add(TypeConstraint(ParseTypeName("unmanaged")));

            if (symbol.HasValueTypeConstraint)
                builder.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));

            foreach (var type in symbol.ConstraintTypes)
                builder.Add(TypeConstraint(type.GenerateTypeSyntax()));

            if (symbol.HasConstructorConstraint)
                builder.Add(ConstructorConstraint());

            return builder.Count == 0
                ? null
                : TypeParameterConstraintClause(IdentifierName(symbol.Name), SeparatedList(builder));
        }
    }
}
