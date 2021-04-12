﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.Options;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.AddParameterCheck), Shared]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.ChangeSignature)]
    internal class CSharpAddParameterCheckCodeRefactoringProvider :
        AbstractAddParameterCheckCodeRefactoringProvider<
            BaseTypeDeclarationSyntax,
            ParameterSyntax,
            StatementSyntax,
            ExpressionSyntax,
            BinaryExpressionSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpAddParameterCheckCodeRefactoringProvider()
        {
        }

        protected override bool IsFunctionDeclaration(SyntaxNode node)
            => InitializeParameterHelpers.IsFunctionDeclaration(node);

        protected override SyntaxNode GetBody(SyntaxNode functionDeclaration)
            => InitializeParameterHelpers.GetBody(functionDeclaration);

        protected override void InsertStatement(SyntaxEditor editor, SyntaxNode functionDeclaration, bool returnsVoid, SyntaxNode statementToAddAfterOpt, StatementSyntax statement)
            => InitializeParameterHelpers.InsertStatement(editor, functionDeclaration, returnsVoid, statementToAddAfterOpt, statement);

        protected override bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => InitializeParameterHelpers.IsImplicitConversion(compilation, source, destination);

        protected override bool CanOffer(SyntaxNode body)
        {
            if (InitializeParameterHelpers.IsExpressionBody(body))
            {
                return InitializeParameterHelpers.TryConvertExpressionBodyToStatement(body,
                    semicolonToken: Token(SyntaxKind.SemicolonToken),
                    createReturnStatementForExpression: false,
                    statement: out var _);
            }

            return true;
        }

        protected override bool PrefersThrowExpression(DocumentOptionSet options)
            => options.GetOption(CSharpCodeStyleOptions.PreferThrowExpression).Value;

        protected override string EscapeResourceString(string input)
            => input.Replace("\\", "\\\\").Replace("\"", "\\\"");

        protected override StatementSyntax CreateParameterCheckIfStatement(DocumentOptionSet options, ExpressionSyntax condition, StatementSyntax ifTrueStatement)
        {
            var withBlock = options.GetOption(CSharpCodeStyleOptions.PreferBraces).Value == CodeAnalysis.CodeStyle.PreferBracesPreference.Always;
            var singleLine = options.GetOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine).Value;
            var closeParenToken = Token(SyntaxKind.CloseParenToken);
            if (withBlock)
            {
                ifTrueStatement = Block(ifTrueStatement);
            }
            else if (singleLine)
            {
                // Any elastic trivia between the closing parenthesis of if and the statement must be removed
                // to convince the formatter to keep everything on a single line.
                // Note: ifTrueStatement and closeParenToken are generated, so there is no need to deal with any existing trivia.
                closeParenToken = closeParenToken.WithTrailingTrivia(Space);
                ifTrueStatement = ifTrueStatement.WithoutLeadingTrivia();
            }

            return IfStatement(
                attributeLists: default,
                ifKeyword: Token(SyntaxKind.IfKeyword),
                openParenToken: Token(SyntaxKind.OpenParenToken),
                condition: condition,
                closeParenToken: closeParenToken,
                statement: ifTrueStatement,
                @else: null);
        }
    }
}
