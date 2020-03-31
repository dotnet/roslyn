// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    [ExportLanguageService(typeof(SyntaxGeneratorInternal), LanguageNames.CSharp), Shared]
    internal sealed class CSharpSyntaxGeneratorInternal : SyntaxGeneratorInternal
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")]
        public CSharpSyntaxGeneratorInternal()
        {
        }

        public static readonly SyntaxGeneratorInternal Instance = new CSharpSyntaxGeneratorInternal();

        internal override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

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

        internal override SyntaxNode ConditionalAccessExpression(SyntaxNode expression, SyntaxNode whenNotNull)
            => SyntaxFactory.ConditionalAccessExpression((ExpressionSyntax)expression, (ExpressionSyntax)whenNotNull);

        internal override SyntaxNode MemberBindingExpression(SyntaxNode name)
            => SyntaxFactory.MemberBindingExpression((SimpleNameSyntax)name);

        internal override SyntaxNode RefExpression(SyntaxNode expression)
            => SyntaxFactory.RefExpression((ExpressionSyntax)expression);

        internal override SyntaxNode AddParentheses(SyntaxNode expression, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true)
        {
            return Parenthesize(expression, includeElasticTrivia, addSimplifierAnnotation);
        }

        internal static ExpressionSyntax Parenthesize(SyntaxNode expression, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true)
        {
            return ((ExpressionSyntax)expression).Parenthesize(includeElasticTrivia, addSimplifierAnnotation);
        }

        internal override SyntaxNode YieldReturnStatement(SyntaxNode expressionOpt = null)
            => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, (ExpressionSyntax)expressionOpt);
    }
}
