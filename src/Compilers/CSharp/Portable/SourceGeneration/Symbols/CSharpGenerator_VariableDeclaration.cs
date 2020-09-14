// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private static VariableDeclarationSyntax GenerateVariableDeclaration(
            ITypeSymbol type, string name, ExpressionSyntax? initializer)
        {
            var equalsValue = initializer == null
                ? null
                : EqualsValueClause(initializer);

            return VariableDeclaration(
                type.GenerateTypeSyntax(),
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier(name), argumentList: null, equalsValue)));
        }
    }
}
