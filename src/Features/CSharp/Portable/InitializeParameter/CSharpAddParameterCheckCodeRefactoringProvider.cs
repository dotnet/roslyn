﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.Semantics;

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

        protected override void InsertStatement(SyntaxEditor editor, SyntaxNode body, SyntaxNode statementToAddAfterOpt, StatementSyntax statement)
            => InitializeParameterHelpers.InsertStatement(editor, body, statementToAddAfterOpt, statement);

        protected override bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination);
    }
}