// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private DestructorDeclarationSyntax GenerateDestructor(IMethodSymbol method)
        {
            if (_currentNamedType == null)
                throw new NotSupportedException("Constructors must be contained within a named type");

            var (body, arrow, semicolon) = method.GetBody().GenerateBodyParts();
            return DestructorDeclaration(
                GenerateAttributeLists(method.GetAttributes()),
                default,
                Token(SyntaxKind.TildeToken),
                Identifier(_currentNamedType.Name),
                GenerateParameterList(method.Parameters),
                body,
                arrow,
                semicolon);
        }
    }
}
