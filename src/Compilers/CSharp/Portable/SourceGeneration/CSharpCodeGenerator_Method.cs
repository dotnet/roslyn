// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static MemberDeclarationSyntax GenerateMethodDeclaration(IMethodSymbol method)
        {
            switch (method.MethodKind)
            {
                case MethodKind.Constructor:
                    return GenerateConstructor(method);
                case MethodKind.Conversion:
                    return GenerateConversion(method);
                case MethodKind.Destructor:
                    return GenerateDestructor(method);
                case MethodKind.UserDefinedOperator:
                    return GenerateOperator(method);
                case MethodKind.Ordinary:
                    return GenerateOrdinaryMethod(method);
            }

            throw new NotImplementedException();
        }

        private static ConstructorDeclarationSyntax GenerateConstructor(IMethodSymbol method)
        {
            return ConstructorDeclaration(
                GenerateAttributeLists(method.GetAttributes()),
                GenerateModifiers(method.DeclaredAccessibility, method.GetModifiers()),
                Identifier(method.ContainingType?.Name ?? method.Name),
                GenerateParameterList(method.Parameters),
                initializer: null,
                body: null,
                semicolonToken: Token(SyntaxKind.SemicolonToken));
        }

        private static ConversionOperatorDeclarationSyntax GenerateConversion(IMethodSymbol method)
        {
            throw new NotImplementedException();
        }

        private static DestructorDeclarationSyntax GenerateDestructor(IMethodSymbol method)
        {
            throw new NotImplementedException();
        }

        private static OperatorDeclarationSyntax GenerateOperator(IMethodSymbol method)
        {
            throw new NotImplementedException();
        }

        private static MethodDeclarationSyntax GenerateOrdinaryMethod(IMethodSymbol method)
        {
            return MethodDeclaration(
                GenerateAttributeLists(method.GetAttributes()),
                GenerateModifiers(method.DeclaredAccessibility, method.GetModifiers()),
                method.ReturnType.GenerateTypeSyntax(),
                GenerateExplicitInterfaceSpecification(method.ExplicitInterfaceImplementations),
                Identifier(method.Name),
                GenerateTypeParameterList(method.TypeArguments),
                GenerateParameterList(method.Parameters),
                GenerateTypeParameterConstraintClauses(method.TypeArguments),
                body: null,
                Token(SyntaxKind.SemicolonToken));
        }
    }
}
