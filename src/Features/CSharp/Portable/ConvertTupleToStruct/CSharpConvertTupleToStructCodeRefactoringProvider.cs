// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
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
                SyntaxFactory.SeparatedList<ArgumentSyntax>(ConvertArguments(tupleExpression.Arguments.GetWithSeparators())),
                tupleExpression.CloseParenToken);

        private SyntaxNodeOrTokenList ConvertArguments(SyntaxNodeOrTokenList list)
            => SyntaxFactory.NodeOrTokenList(list.Select(ConvertArgumentOrComma));

        private SyntaxNodeOrToken ConvertArgumentOrComma(SyntaxNodeOrToken arg)
            => arg.IsToken
                ? arg
                : ConvertArgument((ArgumentSyntax)arg.AsNode());

        // Keep named arguments for literal args.  It helps keep the code self-documenting.
        // Remove for complex args as it's most likely just clutter a person doesn't need
        // when instantiating their new type.
        private SyntaxNode ConvertArgument(ArgumentSyntax argument)
            => argument.Expression is LiteralExpressionSyntax
                ? argument
                : SyntaxFactory.Argument(argument.Expression).WithTriviaFrom(argument);
    }
}
