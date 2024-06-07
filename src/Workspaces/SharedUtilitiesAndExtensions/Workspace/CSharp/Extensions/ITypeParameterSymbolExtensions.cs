// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

using static SyntaxFactory;

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

        return List(clauses);
    }

    private static void AddConstraintClauses(
        List<TypeParameterConstraintClauseSyntax> clauses,
        ITypeParameterSymbol typeParameter)
    {
        var constraints = new List<TypeParameterConstraintSyntax>();

        if (typeParameter.HasReferenceTypeConstraint)
        {
            constraints.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));
        }
        else if (typeParameter.HasUnmanagedTypeConstraint)
        {
            constraints.Add(TypeConstraint(IdentifierName("unmanaged")));
        }
        else if (typeParameter.HasValueTypeConstraint)
        {
            constraints.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));
        }
        else if (typeParameter.HasNotNullConstraint)
        {
            constraints.Add(TypeConstraint(IdentifierName("notnull")));
        }

        var constraintTypes =
            typeParameter.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Class).Concat(
            typeParameter.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Interface).Concat(
            typeParameter.ConstraintTypes.Where(t => t.TypeKind is not TypeKind.Class and not TypeKind.Interface)));

        foreach (var type in constraintTypes)
        {
            if (type.SpecialType != SpecialType.System_Object)
            {
                constraints.Add(TypeConstraint(type.GenerateTypeSyntax()));
            }
        }

        if (typeParameter.HasConstructorConstraint)
        {
            constraints.Add(ConstructorConstraint());
        }

        if (typeParameter.AllowsRefLikeType())
        {
            // "allows ref struct" anti-constraint must be last
            constraints.Add(SyntaxFactoryExtensions.AllowsConstraintClause([SyntaxFactoryExtensions.RefStructConstraint()]));
        }

        if (constraints.Count == 0)
        {
            return;
        }

        clauses.Add(TypeParameterConstraintClause(
            typeParameter.Name.ToIdentifierName(),
            SeparatedList(constraints)));
    }
}
