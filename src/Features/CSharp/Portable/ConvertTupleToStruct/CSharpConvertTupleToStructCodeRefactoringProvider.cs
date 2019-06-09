// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
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
            LiteralExpressionSyntax,
            ObjectCreationExpressionSyntax,
            TupleExpressionSyntax,
            ArgumentSyntax,
            TupleTypeSyntax,
            TypeDeclarationSyntax,
            NamespaceDeclarationSyntax>
    {
        [ImportingConstructor]
        public CSharpConvertTupleToStructCodeRefactoringProvider()
        {
        }

        protected override ObjectCreationExpressionSyntax CreateObjectCreationExpression(
            NameSyntax nameNode, SyntaxToken openParen, SeparatedSyntaxList<ArgumentSyntax> arguments, SyntaxToken closeParen)
        {
            return SyntaxFactory.ObjectCreationExpression(
                nameNode, SyntaxFactory.ArgumentList(openParen, arguments, closeParen), initializer: default);
        }
    }
}
