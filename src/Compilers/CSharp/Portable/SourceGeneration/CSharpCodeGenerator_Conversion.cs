// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static ConversionOperatorDeclarationSyntax GenerateConversion(IMethodSymbol method)
        {
            return ConversionOperatorDeclaration(
                GenerateAttributeLists(method.GetAttributes()),
                GenerateModifiers(method.DeclaredAccessibility, method.GetModifiers()),
                Token(method.Name == WellKnownMemberNames.ImplicitConversionName
                    ? SyntaxKind.ImplicitKeyword
                    : SyntaxKind.ExplicitKeyword),
                method.ReturnType.GenerateTypeSyntax(),
                GenerateParameterList(method.Parameters),
                Block(),
                expressionBody: null);
        }
    }
}
