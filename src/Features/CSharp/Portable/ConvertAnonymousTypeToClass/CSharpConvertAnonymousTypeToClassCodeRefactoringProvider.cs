// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAnonymousTypeToClass
{
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.ConvertAnonymousTypeToClass)), Shared]
    internal class CSharpConvertAnonymousTypeToClassCodeRefactoringProvider :
        AbstractConvertAnonymousTypeToClassCodeRefactoringProvider<
            ExpressionSyntax,
            NameSyntax,
            IdentifierNameSyntax,
            ObjectCreationExpressionSyntax,
            AnonymousObjectCreationExpressionSyntax,
            NamespaceDeclarationSyntax>
    {
        [ImportingConstructor]
        public CSharpConvertAnonymousTypeToClassCodeRefactoringProvider()
        {
        }

        protected override ObjectCreationExpressionSyntax CreateObjectCreationExpression(
            NameSyntax nameNode, AnonymousObjectCreationExpressionSyntax anonymousObject)
        {
            return SyntaxFactory.ObjectCreationExpression(
                nameNode, CreateArgumentList(anonymousObject), initializer: default);
        }

        private ArgumentListSyntax CreateArgumentList(AnonymousObjectCreationExpressionSyntax anonymousObject)
            => SyntaxFactory.ArgumentList(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTriviaFrom(anonymousObject.OpenBraceToken),
                CreateArguments(anonymousObject.Initializers),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTriviaFrom(anonymousObject.CloseBraceToken));

        private SeparatedSyntaxList<ArgumentSyntax> CreateArguments(SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> initializers)
            => SyntaxFactory.SeparatedList<ArgumentSyntax>(CreateArguments(initializers.GetWithSeparators()));

        private SyntaxNodeOrTokenList CreateArguments(SyntaxNodeOrTokenList list)
            => new SyntaxNodeOrTokenList(list.Select(CreateArgumentOrComma));

        private SyntaxNodeOrToken CreateArgumentOrComma(SyntaxNodeOrToken declOrComma)
            => declOrComma.IsToken
                ? declOrComma
                : CreateArgument((AnonymousObjectMemberDeclaratorSyntax)declOrComma);

        private ArgumentSyntax CreateArgument(AnonymousObjectMemberDeclaratorSyntax decl)
            => SyntaxFactory.Argument(decl.Expression);
    }
}
