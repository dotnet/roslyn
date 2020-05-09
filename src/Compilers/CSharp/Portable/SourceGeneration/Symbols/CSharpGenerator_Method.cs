// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.CSharp.CodeGen.CodeGenerator;
using Microsoft.CodeAnalysis.CSharp.SourceGeneration;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private MemberDeclarationSyntax? TryGenerateMethodDeclaration(IMethodSymbol method)
        {
            // skip accessors, they are directly created by properties/events.
            if (IsAnyAccessor(method))
                return null;

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
                case MethodKind.ExplicitInterfaceImplementation:
                    return null;
            }

            throw new NotImplementedException();
        }

        private MethodDeclarationSyntax GenerateOrdinaryMethod(IMethodSymbol method)
        {
            var (body, arrow, semicolon) = method.GetBody().GenerateBodyParts();
            return MethodDeclaration(
                GenerateAttributeLists(method.GetAttributes()),
                GenerateModifiers(method),
                method.ReturnType.GenerateTypeSyntax(),
                GenerateExplicitInterfaceSpecification(method.ExplicitInterfaceImplementations),
                Identifier(method.Name),
                GenerateTypeParameterList(method.TypeArguments),
                GenerateParameterList(method.Parameters),
                GenerateTypeParameterConstraintClauses(method.TypeArguments),
                body,
                arrow,
                semicolon);
        }
    }
}
