// Licensed to the .NET Foundation under one or more agreements.
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
            _options.WrapConditionalExpressions == newOptions.WrapConditionalExpressions &&
            _options.IndentWrappedConditionalExpressions == newOptions.IndentWrappedConditionalExpressions)
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

        AddConditionalWrappingOperations(list, node);

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

    private void AddConditionalWrappingOperations(ArrayBuilder<SuppressOperation> list, SyntaxNode node)
    {
        // Only process binary expressions that are within conditional statements
        if (node is not BinaryExpressionSyntax binaryExpression)
        {
            return;
        }

        // Check if this binary expression is within a conditional statement
        if (!IsInConditionalStatement(binaryExpression))
        {
            return;
        }

        // If wrapping is enabled for conditional expressions
        if (_options.WrapConditionalExpressions)
        {
            // Remove suppress operations for logical operators to allow wrapping
            RemoveSuppressOperationForConditionalExpression(list, binaryExpression);
        }
    }

    private static bool IsInConditionalStatement(SyntaxNode node)
    {
        // Check if this node is within an if/while/for statement condition
        var parent = node.Parent;
        while (parent != null)
        {
            switch (parent)
            {
                case IfStatementSyntax ifStatement:
                    return node.IsDescendantOfOrSelfWith(ifStatement.Condition);
                case WhileStatementSyntax whileStatement:
                    return node.IsDescendantOfOrSelfWith(whileStatement.Condition);
                case ForStatementSyntax forStatement:
                    return forStatement.Condition != null && node.IsDescendantOfOrSelfWith(forStatement.Condition);
                case DoStatementSyntax doStatement:
                    return node.IsDescendantOfOrSelfWith(doStatement.Condition);
                case ConditionalExpressionSyntax conditionalExpression:
                    return node.IsDescendantOfOrSelfWith(conditionalExpression.Condition);
            }
            parent = parent.Parent;
        }
        return false;
    }

    private static void RemoveSuppressOperationForConditionalExpression(ArrayBuilder<SuppressOperation> list, BinaryExpressionSyntax binaryExpression)
    {
        // Only handle logical operators (&&, ||)
        if (binaryExpression.OperatorToken.Kind() is not (SyntaxKind.AmpersandAmpersandToken or SyntaxKind.BarBarToken))
        {
            return;
        }

        // Remove suppress operations around the binary operator to allow wrapping
        var leftEnd = binaryExpression.Left.GetLastToken(includeZeroWidth: true);
        var rightStart = binaryExpression.Right.GetFirstToken(includeZeroWidth: true);
        
        // Remove suppression around the operator to allow line breaks
        RemoveSuppressOperation(list, leftEnd, rightStart);
    }
}
