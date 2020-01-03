// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ITypeParameterSymbolExtensions
    {
        public static SyntaxList<TypeParameterConstraintClauseSyntax> GenerateConstraintClauses(
            this ImmutableArray<ITypeParameterSymbol> typeParameters)
        {
            return typeParameters.AsEnumerable().GenerateConstraintClauses();
        }

        public static SyntaxList<TypeParameterConstraintClauseSyntax> GenerateConstraintClauses(
            this IEnumerable<ITypeParameterSymbol> typeParameters)
        {
            var clauses = new List<TypeParameterConstraintClauseSyntax>();

            foreach (var typeParameter in typeParameters)
            {
                AddConstraintClauses(clauses, typeParameter);
            }

            return clauses.Count == 0 ? default : clauses.ToSyntaxList();
        }

        private static void AddConstraintClauses(
            List<TypeParameterConstraintClauseSyntax> clauses,
            ITypeParameterSymbol typeParameter)
        {
            var constraints = new List<TypeParameterConstraintSyntax>();

            if (typeParameter.HasReferenceTypeConstraint)
            {
                var questionMarkToken = typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
                                        ? SyntaxFactory.Token(SyntaxKind.QuestionToken)
                                        : default;

                constraints.Add(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint, SyntaxFactory.Token(SyntaxKind.ClassKeyword), questionMarkToken));
            }
            else if (typeParameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add(SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName("unmanaged")));
            }
            else if (typeParameter.HasValueTypeConstraint)
            {
                constraints.Add(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint));
            }
            else if (typeParameter.HasNotNullConstraint)
            {
                constraints.Add(SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName("notnull")));
            }

            var constraintTypes =
                typeParameter.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Class).Concat(
                typeParameter.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Interface).Concat(
                typeParameter.ConstraintTypes.Where(t => t.TypeKind != TypeKind.Class && t.TypeKind != TypeKind.Interface)));

            foreach (var type in constraintTypes)
            {
                if (type.SpecialType != SpecialType.System_Object)
                {
                    constraints.Add(SyntaxFactory.TypeConstraint(type.GenerateTypeSyntax()));
                }
            }

            if (typeParameter.HasConstructorConstraint)
            {
                constraints.Add(SyntaxFactory.ConstructorConstraint());
            }

            if (constraints.Count == 0)
            {
                return;
            }

            clauses.Add(SyntaxFactory.TypeParameterConstraintClause(
                typeParameter.Name.ToIdentifierName(),
                SyntaxFactory.SeparatedList(constraints)));
        }
    }
}
