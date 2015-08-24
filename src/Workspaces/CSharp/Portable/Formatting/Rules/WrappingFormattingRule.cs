// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class WrappingFormattingRule : BaseFormattingRule
    {
        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, SyntaxToken lastToken, OptionSet optionSet, NextAction<SuppressOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            AddBraceSuppressOperations(list, node, lastToken);

            AddStatementExceptBlockSuppressOperations(list, node);

            AddSpecificNodesSuppressOperations(list, node);

            if (!optionSet.GetOption(CSharpFormattingOptions.WrappingPreserveSingleLine))
            {
                RemoveSuppressOperationForBlock(list, node);
            }

            if (!optionSet.GetOption(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine))
            {
                RemoveSuppressOperationForStatementMethodDeclaration(list, node);
            }
        }

        private void AddSpecificNodesSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            var ifStatementNode = node as IfStatementSyntax;
            if (ifStatementNode != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, ifStatementNode.IfKeyword, ifStatementNode.Statement.GetLastToken(includeZeroWidth: true));

                if (ifStatementNode.Else != null)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, ifStatementNode.Else.ElseKeyword, ifStatementNode.Else.Statement.GetLastToken(includeZeroWidth: true));
                }

                return;
            }

            var whileStatementNode = node as DoStatementSyntax;
            if (whileStatementNode != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, whileStatementNode.GetFirstToken(includeZeroWidth: true), whileStatementNode.Statement.GetLastToken(includeZeroWidth: true));
                return;
            }

            var memberDeclNode = node as MemberDeclarationSyntax;
            if (memberDeclNode != null)
            {
                var tokens = memberDeclNode.GetFirstAndLastMemberDeclarationTokensAfterAttributes();
                AddSuppressWrappingIfOnSingleLineOperation(list, tokens.Item1, tokens.Item2);
                return;
            }

            var accessorDeclNode = node as AccessorDeclarationSyntax;
            if (accessorDeclNode != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, accessorDeclNode.GetFirstToken(includeZeroWidth: true), accessorDeclNode.GetLastToken(includeZeroWidth: true));
                return;
            }

            var switchSection = node as SwitchSectionSyntax;
            if (switchSection != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, switchSection.GetFirstToken(includeZeroWidth: true), switchSection.GetLastToken(includeZeroWidth: true));
                return;
            }

            var anonymousMethod = node as AnonymousMethodExpressionSyntax;
            if (anonymousMethod != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, anonymousMethod.DelegateKeyword, anonymousMethod.GetLastToken(includeZeroWidth: true));
                return;
            }
        }

        private void AddStatementExceptBlockSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            var statementNode = node as StatementSyntax;
            if (statementNode == null || statementNode.Kind() == SyntaxKind.Block)
            {
                return;
            }

            var firstToken = statementNode.GetFirstToken(includeZeroWidth: true);
            var lastToken = statementNode.GetLastToken(includeZeroWidth: true);

            AddSuppressWrappingIfOnSingleLineOperation(list, firstToken, lastToken);
        }

        private void RemoveSuppressOperationForStatementMethodDeclaration(List<SuppressOperation> list, SyntaxNode node)
        {
            var statementNode = node as StatementSyntax;
            if (!(statementNode == null || statementNode.Kind() == SyntaxKind.Block))
            {
                var firstToken = statementNode.GetFirstToken(includeZeroWidth: true);
                var lastToken = statementNode.GetLastToken(includeZeroWidth: true);

                RemoveSuppressOperation(list, firstToken, lastToken);
            }

            var ifStatementNode = node as IfStatementSyntax;
            if (ifStatementNode != null)
            {
                RemoveSuppressOperation(list, ifStatementNode.IfKeyword, ifStatementNode.Statement.GetLastToken(includeZeroWidth: true));

                if (ifStatementNode.Else != null)
                {
                    RemoveSuppressOperation(list, ifStatementNode.Else.ElseKeyword, ifStatementNode.Else.Statement.GetLastToken(includeZeroWidth: true));
                }

                return;
            }

            var whileStatementNode = node as DoStatementSyntax;
            if (whileStatementNode != null)
            {
                RemoveSuppressOperation(list, whileStatementNode.GetFirstToken(includeZeroWidth: true), whileStatementNode.Statement.GetLastToken(includeZeroWidth: true));
                return;
            }

            var memberDeclNode = node as MemberDeclarationSyntax;
            if (memberDeclNode != null)
            {
                var tokens = memberDeclNode.GetFirstAndLastMemberDeclarationTokensAfterAttributes();
                RemoveSuppressOperation(list, tokens.Item1, tokens.Item2);
                return;
            }

            var switchSection = node as SwitchSectionSyntax;
            if (switchSection != null)
            {
                RemoveSuppressOperation(list, switchSection.GetFirstToken(includeZeroWidth: true), switchSection.GetLastToken(includeZeroWidth: true));
                return;
            }

            var anonymousMethod = node as AnonymousMethodExpressionSyntax;
            if (anonymousMethod != null)
            {
                RemoveSuppressOperation(list, anonymousMethod.DelegateKeyword, anonymousMethod.GetLastToken(includeZeroWidth: true));
                return;
            }
        }

        private void RemoveSuppressOperationForBlock(List<SuppressOperation> list, SyntaxNode node)
        {
            var bracePair = GetBracePair(node);
            if (!bracePair.IsValidBracePair())
            {
                return;
            }

            var firstTokenOfNode = node.GetFirstToken(includeZeroWidth: true);

            if (node.IsLambdaBodyBlock())
            {
                // include lambda itself.
                firstTokenOfNode = node.Parent.GetFirstToken(includeZeroWidth: true);
            }

            // suppress wrapping on whole construct that owns braces and also brace pair itself if it is on same line
            RemoveSuppressOperation(list, firstTokenOfNode, bracePair.Item2);
            RemoveSuppressOperation(list, bracePair.Item1, bracePair.Item2);
        }

        private ValueTuple<SyntaxToken, SyntaxToken> GetBracePair(SyntaxNode node)
        {
            var methodDeclaration = node as BaseMethodDeclarationSyntax;
            if (methodDeclaration != null && methodDeclaration.Body != null)
            {
                return ValueTuple.Create(methodDeclaration.Body.OpenBraceToken, methodDeclaration.Body.CloseBraceToken);
            }

            var propertyDeclaration = node as PropertyDeclarationSyntax;
            if (propertyDeclaration != null && propertyDeclaration.AccessorList != null)
            {
                return ValueTuple.Create(propertyDeclaration.AccessorList.OpenBraceToken, propertyDeclaration.AccessorList.CloseBraceToken);
            }

            var accessorDeclaration = node as AccessorDeclarationSyntax;
            if (accessorDeclaration != null && accessorDeclaration.Body != null)
            {
                return ValueTuple.Create(accessorDeclaration.Body.OpenBraceToken, accessorDeclaration.Body.CloseBraceToken);
            }

            return node.GetBracePair();
        }

        protected void RemoveSuppressOperation(
            List<SuppressOperation> list,
            SyntaxToken startToken,
            SyntaxToken endToken)
        {
            if (startToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            var span = TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].TextSpan.Start >= span.Start && list[i].TextSpan.End <= span.End)
                {
                    list[i] = null;
                }
            }
        }
    }
}
