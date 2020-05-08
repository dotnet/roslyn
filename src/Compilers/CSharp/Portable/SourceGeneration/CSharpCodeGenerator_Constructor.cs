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
        private static ConstructorDeclarationSyntax GenerateConstructor(IMethodSymbol method)
        {
            return ConstructorDeclaration(
                GenerateAttributeLists(method.GetAttributes()),
                GenerateModifiers(method.DeclaredAccessibility, method.GetModifiers()),
                Identifier(method.ContainingType?.Name ?? method.Name),
                GenerateParameterList(method.Parameters),
                initializer: null,
                body: Block(),
                semicolonToken: default);
        }
    }
}
