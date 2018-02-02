// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpInitializeMemberFromParameterCodeRefactoringProvider)), Shared]
    [ExtensionOrder(Before = nameof(CSharpAddParameterCheckCodeRefactoringProvider))]
    internal class CSharpInitializeMemberFromParameterCodeRefactoringProvider :
        AbstractInitializeMemberFromParameterCodeRefactoringProvider<
            ParameterSyntax,
            ParameterListSyntax,
            StatementSyntax,
            ExpressionSyntax,
            BinaryExpressionSyntax>
    {
        protected override SyntaxNode GetFunctionDeclaration(ParameterListSyntax parameterList)
            => InitializeParameterHelpers.GetFunctionDeclaration(parameterList);

        protected override SyntaxNode GetTypeBlock(SyntaxNode node)
            => node;

        protected override SyntaxNode GetBody(SyntaxNode functionDeclaration)
            => InitializeParameterHelpers.GetBody(functionDeclaration);

        protected override SyntaxNode TryGetLastStatement(IBlockOperation blockStatementOpt)
            => InitializeParameterHelpers.TryGetLastStatement(blockStatementOpt);

        protected override void InsertStatement(SyntaxEditor editor, SyntaxNode functionDeclaration, SyntaxNode statementToAddAfterOpt, StatementSyntax statement)
            => InitializeParameterHelpers.InsertStatement(editor, functionDeclaration, statementToAddAfterOpt, statement);

        protected override bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination);
    }
}
