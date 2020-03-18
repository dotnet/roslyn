// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    [ExportLanguageService(typeof(SyntaxGeneratorInternal), LanguageNames.CSharp), Shared]
    internal sealed class CSharpSyntaxGeneratorInternal : SyntaxGeneratorInternal
    {
        [ImportingConstructor]
        public CSharpSyntaxGeneratorInternal()
        {
        }

        public static readonly SyntaxGeneratorInternal Instance = new CSharpSyntaxGeneratorInternal();

        internal override SyntaxNode LocalDeclarationStatement(SyntaxNode type, SyntaxToken name, SyntaxNode initializer, bool isConst)
        {
            return SyntaxFactory.LocalDeclarationStatement(
                isConst ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ConstKeyword)) : default,
                 VariableDeclaration(type, name, initializer));
        }

        internal override SyntaxNode WithInitializer(SyntaxNode variableDeclarator, SyntaxNode initializer)
            => ((VariableDeclaratorSyntax)variableDeclarator).WithInitializer((EqualsValueClauseSyntax)initializer);

        internal override SyntaxNode EqualsValueClause(SyntaxToken operatorToken, SyntaxNode value)
            => SyntaxFactory.EqualsValueClause(operatorToken, (ExpressionSyntax)value);

        internal static VariableDeclarationSyntax VariableDeclaration(SyntaxNode type, SyntaxToken name, SyntaxNode expression)
        {
            return SyntaxFactory.VariableDeclaration(
                type == null ? SyntaxFactory.IdentifierName("var") : (TypeSyntax)type,
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            name, argumentList: null,
                            expression == null ? null : SyntaxFactory.EqualsValueClause((ExpressionSyntax)expression))));
        }

        internal override SyntaxToken Identifier(string identifier)
            => SyntaxFactory.Identifier(identifier);
    }
}
