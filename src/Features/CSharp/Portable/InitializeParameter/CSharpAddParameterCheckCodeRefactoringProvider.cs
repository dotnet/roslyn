﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpAddParameterCheckCodeRefactoringProvider)), Shared]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.ChangeSignature)]
    internal class CSharpAddParameterCheckCodeRefactoringProvider :
        AbstractAddParameterCheckCodeRefactoringProvider<
            ParameterSyntax,
            BaseMethodDeclarationSyntax,
            StatementSyntax,
            ExpressionSyntax,
            BinaryExpressionSyntax>
    {
        protected override SyntaxNode GetTypeBlock(SyntaxNode node)
            => node;

        protected override SyntaxNode GetBody(BaseMethodDeclarationSyntax containingMember)
            => InitializeParameterHelpers.GetBody(containingMember);

        protected override void InsertStatement(SyntaxEditor editor, BaseMethodDeclarationSyntax methodDeclarationSyntax, SyntaxNode statementToAddAfterOpt, StatementSyntax statement)
            => InitializeParameterHelpers.InsertStatement(editor, methodDeclarationSyntax, statementToAddAfterOpt, statement);

        protected override bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination);

        protected override bool CanOffer(SyntaxNode body)
        {
            if (body is ArrowExpressionClauseSyntax arrowExpressionClauseSyntax)
            {
                return arrowExpressionClauseSyntax.TryConvertToStatement(
                    semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken), 
                    createReturnStatementForExpression: false, 
                    statement: out var _);              
            }

            return true;
        }
    }
}
