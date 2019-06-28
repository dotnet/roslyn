// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class WrappingFormattingRule : BaseFormattingRule
    {
        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, in NextSuppressOperationAction nextOperation)
        {
            nextOperation.Invoke();

            AddBraceSuppressOperations(list, node);

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

        private ValueTuple<SyntaxToken, SyntaxToken> GetSpecificNodeSuppressionTokenRange(SyntaxNode node)
        {
            var embeddedStatement = node.GetEmbeddedStatement();
            if (embeddedStatement != null)
            {
                var firstTokenOfEmbeddedStatement = embeddedStatement.GetFirstToken(includeZeroWidth: true);
                if (embeddedStatement.IsKind(SyntaxKind.Block))
                {
                    return ValueTuple.Create(
                        firstTokenOfEmbeddedStatement.GetPreviousToken(includeZeroWidth: true),
                        embeddedStatement.GetLastToken(includeZeroWidth: true));
                }
                else
                {
                    return ValueTuple.Create(
                        firstTokenOfEmbeddedStatement.GetPreviousToken(includeZeroWidth: true),
                        firstTokenOfEmbeddedStatement);
                }
            }

            return node switch
            {
                SwitchSectionSyntax switchSection => ValueTuple.Create(switchSection.GetFirstToken(includeZeroWidth: true), switchSection.GetLastToken(includeZeroWidth: true)),
                AnonymousMethodExpressionSyntax anonymousMethod => ValueTuple.Create(anonymousMethod.DelegateKeyword, anonymousMethod.GetLastToken(includeZeroWidth: true)),
                _ => default,
            };
        }

        private void AddSpecificNodesSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            var tokens = GetSpecificNodeSuppressionTokenRange(node);
            if (!tokens.Equals(default))
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, tokens.Item1, tokens.Item2);
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

            var tokens = GetSpecificNodeSuppressionTokenRange(node);
            if (!tokens.Equals(default))
            {
                RemoveSuppressOperation(list, tokens.Item1, tokens.Item2);
            }

            var ifStatementNode = node as IfStatementSyntax;
            if (ifStatementNode?.Else != null)
            {
                RemoveSuppressOperation(list, ifStatementNode.Else.ElseKeyword, ifStatementNode.Else.Statement.GetFirstToken(includeZeroWidth: true));
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
            if (node is BaseMethodDeclarationSyntax methodDeclaration && methodDeclaration.Body != null)
            {
                return ValueTuple.Create(methodDeclaration.Body.OpenBraceToken, methodDeclaration.Body.CloseBraceToken);
            }

            if (node is PropertyDeclarationSyntax propertyDeclaration && propertyDeclaration.AccessorList != null)
            {
                return ValueTuple.Create(propertyDeclaration.AccessorList.OpenBraceToken, propertyDeclaration.AccessorList.CloseBraceToken);
            }

            if (node is AccessorDeclarationSyntax accessorDeclaration && accessorDeclaration.Body != null)
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

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].TextSpan.Start >= span.Start && list[i].TextSpan.End <= span.End && list[i].Option.HasFlag(SuppressOption.NoWrappingIfOnSingleLine))
                {
                    list[i] = null;
                }
            }
        }
    }
}
