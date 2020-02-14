﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.Wrapping)]
    internal class CSharpInitializeMemberFromParameterCodeRefactoringProvider :
        AbstractInitializeMemberFromParameterCodeRefactoringProvider<
            ParameterSyntax,
            StatementSyntax,
            ExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpInitializeMemberFromParameterCodeRefactoringProvider()
        {
        }

        protected override bool IsFunctionDeclaration(SyntaxNode node)
            => InitializeParameterHelpers.IsFunctionDeclaration(node);

        protected override SyntaxNode GetTypeBlock(SyntaxNode node)
            => node;

        protected override SyntaxNode TryGetLastStatement(IBlockOperation blockStatementOpt)
            => InitializeParameterHelpers.TryGetLastStatement(blockStatementOpt);

        protected override void InsertStatement(SyntaxEditor editor, SyntaxNode functionDeclaration, IMethodSymbol method, SyntaxNode statementToAddAfterOpt, StatementSyntax statement)
            => InitializeParameterHelpers.InsertStatement(editor, functionDeclaration, method, statementToAddAfterOpt, statement);

        protected override bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination);

        // Fields are always private by default in C#.
        protected override Accessibility DetermineDefaultFieldAccessibility(INamedTypeSymbol containingType)
            => Accessibility.Private;

        // Properties are always private by default in C#.
        protected override Accessibility DetermineDefaultPropertyAccessibility()
            => Accessibility.Private;

        protected override SyntaxNode GetBody(SyntaxNode functionDeclaration)
            => InitializeParameterHelpers.GetBody(functionDeclaration);
    }
}
