// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private static IdentifierNameSyntax GenerateLocalIdentifierName(ILocalSymbol symbol)
            => IdentifierName(symbol.Name);

        private static LocalDeclarationStatementSyntax GenerateLocalDeclarationStatement(ILocalSymbol symbol)
        {
            return LocalDeclarationStatement(
                GenerateModifiers(symbol.DeclaredAccessibility, symbol.GetModifiers()),
                GenerateVariableDeclaration(symbol.Type, symbol.Name,
                    GenerateConstantExpression(symbol.Type, symbol.HasConstantValue, symbol.ConstantValue)));
        }
    }
}
