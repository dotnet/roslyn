// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertTupleToStruct
{
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.ConvertTupleToStruct)), Shared]
    internal class CSharpConvertTupleToStructCodeRefactoringProvider :
        AbstractConvertTupleToStructCodeRefactoringProvider<
            ExpressionSyntax,
            NameSyntax,
            IdentifierNameSyntax,
            ObjectCreationExpressionSyntax,
            TupleExpressionSyntax,
            TupleTypeSyntax,
            TypeDeclarationSyntax,
            NamespaceDeclarationSyntax>
    {
        protected override ObjectCreationExpressionSyntax CreateObjectCreationExpression(
            NameSyntax nameNode, TupleExpressionSyntax tupleExpression)
        {
            return SyntaxFactory.ObjectCreationExpression(
                nameNode, CreateArgumentList(tupleExpression), initializer: default);
        }

        private ArgumentListSyntax CreateArgumentList(TupleExpressionSyntax tupleExpression)
            => SyntaxFactory.ArgumentList(
                tupleExpression.OpenParenToken,
                tupleExpression.Arguments,
                tupleExpression.CloseParenToken);

        //private SeparatedSyntaxList<ArgumentSyntax> CreateArguments(SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> initializers)
        //    => SyntaxFactory.SeparatedList<ArgumentSyntax>(CreateArguments(initializers.GetWithSeparators()));

        //private SyntaxNodeOrTokenList CreateArguments(SyntaxNodeOrTokenList list)
        //    => new SyntaxNodeOrTokenList(list.Select(CreateArgumentOrComma));

        //private SyntaxNodeOrToken CreateArgumentOrComma(SyntaxNodeOrToken declOrComma)
        //    => declOrComma.IsToken
        //        ? declOrComma
        //        : CreateArgument((AnonymousObjectMemberDeclaratorSyntax)declOrComma);

        //private ArgumentSyntax CreateArgument(AnonymousObjectMemberDeclaratorSyntax decl)
        //    => SyntaxFactory.Argument(decl.Expression);
    }
}
