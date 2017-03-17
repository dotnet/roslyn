// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.InitializeParameter;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpInitializeParameterCodeRefactoringProvider)), Shared]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.ChangeSignature)]
    internal class CSharpInitializeParameterCodeRefactoringProvider :
        AbstractInitializeParameterCodeRefactoringProvider<
            ParameterSyntax,
            BaseMethodDeclarationSyntax,
            StatementSyntax,
            ExpressionSyntax,
            BinaryExpressionSyntax>
    {
        protected override SyntaxNode GetBody(BaseMethodDeclarationSyntax containingMember)
            => containingMember.Body ?? (SyntaxNode)containingMember.ExpressionBody;

        protected override void InsertStatement(
            SyntaxEditor editor,
            SyntaxNode body,
            IOperation statementToAddAfterOpt,
            StatementSyntax nullCheckStatement)
        {
            var generator = editor.Generator;

            if (body is ArrowExpressionClauseSyntax arrowExpression)
            {
                var methodBase = (BaseMethodDeclarationSyntax)body.Parent;

                editor.SetStatements(methodBase, 
                    ImmutableArray.Create(
                        nullCheckStatement,
                        generator.ExpressionStatement(arrowExpression.Expression)));
            }
            else if (body is BlockSyntax block)
            {
                var indexToAddAfter = block.Statements.IndexOf(s => s == statementToAddAfterOpt?.Syntax);
                if (indexToAddAfter >= 0)
                {
                    editor.InsertAfter(block.Statements[indexToAddAfter], nullCheckStatement);
                }
                else if (block.Statements.Count > 0)
                {
                    editor.InsertBefore(block.Statements[0], nullCheckStatement);
                }
                else
                {
                    editor.ReplaceNode(block, block.AddStatements((StatementSyntax)nullCheckStatement));
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}