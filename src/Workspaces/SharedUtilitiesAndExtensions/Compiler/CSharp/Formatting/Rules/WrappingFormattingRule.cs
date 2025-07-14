﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed class WrappingFormattingRule : BaseFormattingRule
{
    private readonly CSharpSyntaxFormattingOptions _options;

    public WrappingFormattingRule()
        : this(CSharpSyntaxFormattingOptions.Default)
    {
    }

    private WrappingFormattingRule(CSharpSyntaxFormattingOptions options)
    {
        _options = options;
    }

    public override AbstractFormattingRule WithOptions(SyntaxFormattingOptions options)
    {
        var newOptions = options as CSharpSyntaxFormattingOptions ?? CSharpSyntaxFormattingOptions.Default;

        if (_options.WrappingPreserveSingleLine == newOptions.WrappingPreserveSingleLine &&
            _options.WrappingKeepStatementsOnSingleLine == newOptions.WrappingKeepStatementsOnSingleLine &&
            _options.WrapMethodCallChains == newOptions.WrapMethodCallChains &&
            _options.IndentWrappedMethodCallChains == newOptions.IndentWrappedMethodCallChains &&
            _options.WrapParameters == newOptions.WrapParameters &&
            _options.AlignWrappedParameters == newOptions.AlignWrappedParameters &&
            _options.WrapParametersOnNewLine == newOptions.WrapParametersOnNewLine)
        {
            return this;
        }

        return new WrappingFormattingRule(newOptions);
    }

    public override void AddSuppressOperations(ArrayBuilder<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
    {
        nextOperation.Invoke();

        AddBraceSuppressOperations(list, node);

        AddStatementExceptBlockSuppressOperations(list, node);

        AddSpecificNodesSuppressOperations(list, node);

        AddMethodCallChainWrappingOperations(list, node);

        AddParameterWrappingOperations(list, node);

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

    private static void AddSpecificNodesSuppressOperations(ArrayBuilder<SuppressOperation> list, SyntaxNode node)
    {
        var (firstToken, lastToken) = GetSpecificNodeSuppressionTokenRange(node);
        if (!firstToken.IsKind(SyntaxKind.None) || !lastToken.IsKind(SyntaxKind.None))
        {
            AddSuppressWrappingIfOnSingleLineOperation(list, firstToken, lastToken);
        }
    }

    private static void AddStatementExceptBlockSuppressOperations(ArrayBuilder<SuppressOperation> list, SyntaxNode node)
    {
        if (node is not StatementSyntax statementNode || statementNode.Kind() == SyntaxKind.Block)
        {
            return;
        }

        var firstToken = statementNode.GetFirstToken(includeZeroWidth: true);
        var lastToken = statementNode.GetLastToken(includeZeroWidth: true);

        AddSuppressWrappingIfOnSingleLineOperation(list, firstToken, lastToken);
    }

    private static void RemoveSuppressOperationForStatementMethodDeclaration(ArrayBuilder<SuppressOperation> list, SyntaxNode node)
    {
        if (!(node is not StatementSyntax statementNode || statementNode.Kind() == SyntaxKind.Block))
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

    private static void RemoveSuppressOperationForBlock(ArrayBuilder<SuppressOperation> list, SyntaxNode node)
    {
        var bracePair = GetBracePair(node);
        if (!bracePair.IsValidBracketOrBracePair())
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
        ArrayBuilder<SuppressOperation> list,
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

    private void AddMethodCallChainWrappingOperations(ArrayBuilder<SuppressOperation> list, SyntaxNode node)
    {
        // If wrapping is not enabled for method call chains, do nothing
        if (!_options.WrapMethodCallChains)
        {
            return;
        }

        // Process member access expressions (e.g., obj.Method1().Method2())
        if (node is MemberAccessExpressionSyntax memberAccess)
        {
            // Check if this is part of a method call chain
            if (IsPartOfMethodCallChain(memberAccess))
            {
                RemoveSuppressOperationForMethodCallChain(list, memberAccess);
            }
            return;
        }

        // Process invocation expressions (e.g., Method1().Method2())
        if (node is InvocationExpressionSyntax invocation)
        {
            // Check if this invocation is part of a method call chain
            if (IsPartOfMethodCallChain(invocation))
            {
                RemoveSuppressOperationForMethodCallChain(list, invocation);
            }
            return;
        }
    }

    private static bool IsPartOfMethodCallChain(SyntaxNode node)
    {
        // Check if this node is part of a method call chain by looking for:
        // 1. Member access expressions that are part of chained calls
        // 2. Invocation expressions that are part of chained calls

        // For member access, check if the expression is an invocation or another member access
        if (node is MemberAccessExpressionSyntax memberAccess)
        {
            // Check if the left side is an invocation or another member access
            return memberAccess.Expression is InvocationExpressionSyntax or MemberAccessExpressionSyntax;
        }

        // For invocation, check if it's part of a larger chain
        if (node is InvocationExpressionSyntax invocation)
        {
            // Check if this invocation is followed by another member access
            return invocation.Parent is MemberAccessExpressionSyntax;
        }

        return false;
    }

    private static void RemoveSuppressOperationForMethodCallChain(ArrayBuilder<SuppressOperation> list, SyntaxNode node)
    {
        if (node is MemberAccessExpressionSyntax memberAccess)
        {
            // Remove suppress operations around the dot token to allow wrapping
            var leftEnd = memberAccess.Expression.GetLastToken(includeZeroWidth: true);
            var rightStart = memberAccess.Name.GetFirstToken(includeZeroWidth: true);
            
            RemoveSuppressOperation(list, leftEnd, rightStart);
        }
        else if (node is InvocationExpressionSyntax invocation && invocation.Parent is MemberAccessExpressionSyntax parentMemberAccess)
        {
            // Remove suppress operations around the dot token after the invocation
            var leftEnd = invocation.GetLastToken(includeZeroWidth: true);
            var rightStart = parentMemberAccess.Name.GetFirstToken(includeZeroWidth: true);
            
            RemoveSuppressOperation(list, leftEnd, rightStart);
        }
    }

    private void AddParameterWrappingOperations(ArrayBuilder<SuppressOperation> list, SyntaxNode node)
    {
        // If parameter wrapping is not enabled, do nothing
        if (!_options.WrapParameters)
        {
            return;
        }

        // Process parameter lists in method declarations
        if (node is ParameterListSyntax parameterList)
        {
            RemoveSuppressOperationForParameterList(list, parameterList);
            return;
        }

        // Process argument lists in method calls
        if (node is ArgumentListSyntax argumentList)
        {
            RemoveSuppressOperationForArgumentList(list, argumentList);
            return;
        }

        // Process bracket parameter lists (indexers, attributes)
        if (node is BracketedParameterListSyntax bracketedParameterList)
        {
            RemoveSuppressOperationForBracketedParameterList(list, bracketedParameterList);
            return;
        }

        // Process bracketed argument lists (indexers, attributes)
        if (node is BracketedArgumentListSyntax bracketedArgumentList)
        {
            RemoveSuppressOperationForBracketedArgumentList(list, bracketedArgumentList);
            return;
        }
    }

    private static void RemoveSuppressOperationForParameterList(ArrayBuilder<SuppressOperation> list, ParameterListSyntax parameterList)
    {
        // Only process if there are multiple parameters
        if (parameterList.Parameters.Count <= 1)
        {
            return;
        }

        // Remove suppress operations around commas to allow wrapping
        for (int i = 0; i < parameterList.Parameters.SeparatorCount; i++)
        {
            var separator = parameterList.Parameters.GetSeparator(i);
            var leftEnd = parameterList.Parameters[i].GetLastToken(includeZeroWidth: true);
            var rightStart = parameterList.Parameters[i + 1].GetFirstToken(includeZeroWidth: true);
            
            RemoveSuppressOperation(list, leftEnd, rightStart);
        }
    }

    private static void RemoveSuppressOperationForArgumentList(ArrayBuilder<SuppressOperation> list, ArgumentListSyntax argumentList)
    {
        // Only process if there are multiple arguments
        if (argumentList.Arguments.Count <= 1)
        {
            return;
        }

        // Remove suppress operations around commas to allow wrapping
        for (int i = 0; i < argumentList.Arguments.SeparatorCount; i++)
        {
            var separator = argumentList.Arguments.GetSeparator(i);
            var leftEnd = argumentList.Arguments[i].GetLastToken(includeZeroWidth: true);
            var rightStart = argumentList.Arguments[i + 1].GetFirstToken(includeZeroWidth: true);
            
            RemoveSuppressOperation(list, leftEnd, rightStart);
        }
    }

    private static void RemoveSuppressOperationForBracketedParameterList(ArrayBuilder<SuppressOperation> list, BracketedParameterListSyntax bracketedParameterList)
    {
        // Only process if there are multiple parameters
        if (bracketedParameterList.Parameters.Count <= 1)
        {
            return;
        }

        // Remove suppress operations around commas to allow wrapping
        for (int i = 0; i < bracketedParameterList.Parameters.SeparatorCount; i++)
        {
            var separator = bracketedParameterList.Parameters.GetSeparator(i);
            var leftEnd = bracketedParameterList.Parameters[i].GetLastToken(includeZeroWidth: true);
            var rightStart = bracketedParameterList.Parameters[i + 1].GetFirstToken(includeZeroWidth: true);
            
            RemoveSuppressOperation(list, leftEnd, rightStart);
        }
    }

    private static void RemoveSuppressOperationForBracketedArgumentList(ArrayBuilder<SuppressOperation> list, BracketedArgumentListSyntax bracketedArgumentList)
    {
        // Only process if there are multiple arguments
        if (bracketedArgumentList.Arguments.Count <= 1)
        {
            return;
        }

        // Remove suppress operations around commas to allow wrapping
        for (int i = 0; i < bracketedArgumentList.Arguments.SeparatorCount; i++)
        {
            var separator = bracketedArgumentList.Arguments.GetSeparator(i);
            var leftEnd = bracketedArgumentList.Arguments[i].GetLastToken(includeZeroWidth: true);
            var rightStart = bracketedArgumentList.Arguments[i + 1].GetFirstToken(includeZeroWidth: true);
            
            RemoveSuppressOperation(list, leftEnd, rightStart);
        }
    }
}
