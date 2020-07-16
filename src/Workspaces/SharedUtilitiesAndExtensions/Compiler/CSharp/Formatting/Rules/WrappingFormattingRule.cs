// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal sealed class WrappingFormattingRule : BaseFormattingRule
    {
        private readonly CachedOptions _options;

        public WrappingFormattingRule()
            : this(new CachedOptions(null))
        {
        }

        private WrappingFormattingRule(CachedOptions options)
        {
            _options = options;
        }

        public override AbstractFormattingRule WithOptions(AnalyzerConfigOptions options)
        {
            var cachedOptions = new CachedOptions(options);

            if (cachedOptions == _options)
            {
                return this;
            }

            return new WrappingFormattingRule(cachedOptions);
        }

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
        {
            nextOperation.Invoke();

            AddBraceSuppressOperations(list, node);

            AddStatementExceptBlockSuppressOperations(list, node);

            AddSpecificNodesSuppressOperations(list, node);

            if (!_options.WrappingPreserveSingleLine)
            {
                RemoveSuppressOperationForBlock(list, node);
            }

            if (!_options.WrappingKeepStatementsOnSingleLine)
            {
                RemoveSuppressOperationForStatementMethodDeclaration(list, node);
            }
        }

        private static (SyntaxToken firstToken, SyntaxToken lastToken) GetSpecificNodeSuppressionTokenRange(SyntaxNode node)
        {
            var embeddedStatement = node.GetEmbeddedStatement();
            if (embeddedStatement != null)
            {
                var firstTokenOfEmbeddedStatement = embeddedStatement.GetFirstToken(includeZeroWidth: true);
                var firstToken = firstTokenOfEmbeddedStatement.GetPreviousToken(includeZeroWidth: true);
                if (embeddedStatement.IsKind(SyntaxKind.Block))
                {
                    return (firstToken, embeddedStatement.GetLastToken(includeZeroWidth: true));
                }
                else
                {
                    return (firstToken, firstTokenOfEmbeddedStatement);
                }
            }

            return node switch
            {
                SwitchSectionSyntax switchSection => (switchSection.GetFirstToken(includeZeroWidth: true), switchSection.GetLastToken(includeZeroWidth: true)),
                AnonymousMethodExpressionSyntax anonymousMethod => (anonymousMethod.DelegateKeyword, anonymousMethod.GetLastToken(includeZeroWidth: true)),
                _ => default,
            };
        }

        private static void AddSpecificNodesSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            var (firstToken, lastToken) = GetSpecificNodeSuppressionTokenRange(node);
            if (!firstToken.IsKind(SyntaxKind.None) || !lastToken.IsKind(SyntaxKind.None))
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, firstToken, lastToken);
            }
        }

        private static void AddStatementExceptBlockSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            if (!(node is StatementSyntax statementNode) || statementNode.Kind() == SyntaxKind.Block)
            {
                return;
            }

            var firstToken = statementNode.GetFirstToken(includeZeroWidth: true);
            var lastToken = statementNode.GetLastToken(includeZeroWidth: true);

            AddSuppressWrappingIfOnSingleLineOperation(list, firstToken, lastToken);
        }

        private static void RemoveSuppressOperationForStatementMethodDeclaration(List<SuppressOperation> list, SyntaxNode node)
        {
            if (!(!(node is StatementSyntax statementNode) || statementNode.Kind() == SyntaxKind.Block))
            {
                var firstToken = statementNode.GetFirstToken(includeZeroWidth: true);
                var lastToken = statementNode.GetLastToken(includeZeroWidth: true);

                RemoveSuppressOperation(list, firstToken, lastToken);
            }

            var tokens = GetSpecificNodeSuppressionTokenRange(node);
            if (!tokens.firstToken.IsKind(SyntaxKind.None) || !tokens.lastToken.IsKind(SyntaxKind.None))
            {
                RemoveSuppressOperation(list, tokens.firstToken, tokens.lastToken);
            }

            var ifStatementNode = node as IfStatementSyntax;
            if (ifStatementNode?.Else != null)
            {
                RemoveSuppressOperation(list, ifStatementNode.Else.ElseKeyword, ifStatementNode.Else.Statement.GetFirstToken(includeZeroWidth: true));
            }
        }

        private static void RemoveSuppressOperationForBlock(List<SuppressOperation> list, SyntaxNode node)
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
                firstTokenOfNode = node.Parent!.GetFirstToken(includeZeroWidth: true);
            }

            // suppress wrapping on whole construct that owns braces and also brace pair itself if it is on same line
            RemoveSuppressOperation(list, firstTokenOfNode, bracePair.closeBrace);
            RemoveSuppressOperation(list, bracePair.openBrace, bracePair.closeBrace);
        }

        private static (SyntaxToken openBrace, SyntaxToken closeBrace) GetBracePair(SyntaxNode node)
        {
            if (node is BaseMethodDeclarationSyntax methodDeclaration && methodDeclaration.Body != null)
            {
                return (methodDeclaration.Body.OpenBraceToken, methodDeclaration.Body.CloseBraceToken);
            }

            if (node is PropertyDeclarationSyntax propertyDeclaration && propertyDeclaration.AccessorList != null)
            {
                return (propertyDeclaration.AccessorList.OpenBraceToken, propertyDeclaration.AccessorList.CloseBraceToken);
            }

            if (node is AccessorDeclarationSyntax accessorDeclaration && accessorDeclaration.Body != null)
            {
                return (accessorDeclaration.Body.OpenBraceToken, accessorDeclaration.Body.CloseBraceToken);
            }

            return node.GetBracePair();
        }

        private static void RemoveSuppressOperation(
            List<SuppressOperation> list,
            SyntaxToken startToken,
            SyntaxToken endToken)
        {
            if (startToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
            {
                return;
            }

            var span = TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);
            list.RemoveOrTransformAll(
                (operation, span) =>
                {
                    if (operation.TextSpan.Start >= span.Start && operation.TextSpan.End <= span.End && operation.Option.HasFlag(SuppressOption.NoWrappingIfOnSingleLine))
                        return null;

                    return operation;
                },
                span);
        }

        private readonly struct CachedOptions : IEquatable<CachedOptions>
        {
            public readonly bool WrappingPreserveSingleLine;
            public readonly bool WrappingKeepStatementsOnSingleLine;

            public CachedOptions(AnalyzerConfigOptions? options)
            {
                WrappingPreserveSingleLine = GetOptionOrDefault(options, CSharpFormattingOptions2.WrappingPreserveSingleLine);
                WrappingKeepStatementsOnSingleLine = GetOptionOrDefault(options, CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine);
            }

            public static bool operator ==(CachedOptions left, CachedOptions right)
                => left.Equals(right);

            public static bool operator !=(CachedOptions left, CachedOptions right)
                => !(left == right);

            private static T GetOptionOrDefault<T>(AnalyzerConfigOptions? options, Option2<T> option)
            {
                if (options is null)
                    return option.DefaultValue;

                return options.GetOption(option);
            }

            public override bool Equals(object? obj)
                => obj is CachedOptions options && Equals(options);

            public bool Equals(CachedOptions other)
            {
                return WrappingPreserveSingleLine == other.WrappingPreserveSingleLine
                    && WrappingKeepStatementsOnSingleLine == other.WrappingKeepStatementsOnSingleLine;
            }

            public override int GetHashCode()
            {
                var hashCode = 0;
                hashCode = (hashCode << 1) + (WrappingPreserveSingleLine ? 1 : 0);
                hashCode = (hashCode << 1) + (WrappingKeepStatementsOnSingleLine ? 1 : 0);
                return hashCode;
            }
        }
    }
}
