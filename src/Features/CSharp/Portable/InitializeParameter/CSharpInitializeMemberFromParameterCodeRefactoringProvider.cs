// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpInitializeMemberFromParameterCodeRefactoringProvider)), Shared]
    [ExtensionOrder(Before = nameof(CSharpAddParameterCheckCodeRefactoringProvider))]
    internal class CSharpInitializeMemberFromParameterCodeRefactoringProvider :
        AbstractInitializeMemberFromParameterCodeRefactoringProvider<
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

        protected override SyntaxNode GetLastStatement(IBlockStatement blockStatement)
            => InitializeParameterHelpers.GetLastStatement(blockStatement);

        protected override void InsertStatement(SyntaxEditor editor, SyntaxNode body, SyntaxNode statementToAddAfterOpt, StatementSyntax statement)
            => InitializeParameterHelpers.InsertStatement(editor, body, statementToAddAfterOpt, statement);

        protected override bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination);
    }
}