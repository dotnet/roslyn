// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal static class FormattingHelpers
{
    // TODO:  Need to determine correct way to handle newlines
    public const string NewLine = "\r\n";

    public static string GetIndent(this SyntaxToken token)
    {
        var precedingTrivia = token.GetAllPrecedingTriviaToPreviousToken();

        // indent is the spaces/tabs between last new line (if there is one) and end of trivia
        var indent = precedingTrivia.AsString();
        var lastNewLinePos = indent.LastIndexOf(NewLine, StringComparison.Ordinal);
        if (lastNewLinePos != -1)
        {
            var start = lastNewLinePos + NewLine.Length;
            indent = indent[start..];
        }

        return indent;
    }

    public static string ContentBeforeLastNewLine(this IEnumerable<SyntaxTrivia> trivia)
    {
        var leading = trivia.AsString();
        var lastNewLinePos = leading.LastIndexOf(NewLine, StringComparison.Ordinal);
        if (lastNewLinePos == -1)
        {
            return string.Empty;
        }

        return leading[..lastNewLinePos];
    }

    public static (SyntaxToken openBrace, SyntaxToken closeBrace) GetBracePair(this SyntaxNode? node)
        => node.GetBraces();

    public static (SyntaxToken openBracket, SyntaxToken closeBracket) GetBracketPair(this SyntaxNode? node)
        => node.GetBrackets();

    public static bool IsValidBracketOrBracePair(this (SyntaxToken openBracketOrBrace, SyntaxToken closeBracketOrBrace) bracketOrBracePair)
    {
        if (bracketOrBracePair.openBracketOrBrace.IsKind(SyntaxKind.None) ||
            bracketOrBracePair.openBracketOrBrace.IsMissing ||
            bracketOrBracePair.closeBracketOrBrace.IsKind(SyntaxKind.None))
        {
            return false;
        }

        if (bracketOrBracePair.openBracketOrBrace.IsKind(SyntaxKind.OpenBraceToken))
        {
            return bracketOrBracePair.closeBracketOrBrace.IsKind(SyntaxKind.CloseBraceToken);
        }

        if (bracketOrBracePair.openBracketOrBrace.IsKind(SyntaxKind.OpenBracketToken))
        {
            return bracketOrBracePair.closeBracketOrBrace.IsKind(SyntaxKind.CloseBracketToken);
        }

        return false;
    }

    public static bool IsOpenParenInParameterListOfAConversionOperatorDeclaration(this SyntaxToken token)
        => token.IsOpenParenInParameterList() && token.Parent.IsParentKind(SyntaxKind.ConversionOperatorDeclaration);

    public static bool IsOpenParenInParameterListOfAOperationDeclaration(this SyntaxToken token)
        => token.IsOpenParenInParameterList() && token.Parent.IsParentKind(SyntaxKind.OperatorDeclaration);

    public static bool IsOpenParenInParameterList(this SyntaxToken token)
        => token.Kind() == SyntaxKind.OpenParenToken && token.Parent.IsKind(SyntaxKind.ParameterList);

    public static bool IsCloseParenInParameterList(this SyntaxToken token)
        => token.Kind() == SyntaxKind.CloseParenToken && token.Parent.IsKind(SyntaxKind.ParameterList);

    public static bool IsOpenParenInArgumentListOrPositionalPattern(this SyntaxToken token)
    {
        return token.Kind() == SyntaxKind.OpenParenToken &&
            IsTokenInArgumentListOrPositionalPattern(token);
    }

    public static bool IsCloseParenInArgumentListOrPositionalPattern(this SyntaxToken token)
    {
        return token.Kind() == SyntaxKind.CloseParenToken &&
            IsTokenInArgumentListOrPositionalPattern(token);
    }

    private static bool IsTokenInArgumentListOrPositionalPattern(SyntaxToken token)
    {
        // Argument lists
        if (token.Parent is (kind: SyntaxKind.ArgumentList or SyntaxKind.AttributeArgumentList))
        {
            return true;
        }

        // Positional patterns
        if (token.Parent.IsKind(SyntaxKind.PositionalPatternClause) && token.Parent.Parent.IsKind(SyntaxKind.RecursivePattern))
        {
            // Avoid treating tuple expressions as positional patterns for formatting
            return token.Parent.Parent.GetFirstToken() != token;
        }

        return false;
    }

    public static bool IsColonInTypeBaseList(this SyntaxToken token)
        => token.Kind() == SyntaxKind.ColonToken && token.Parent.IsKind(SyntaxKind.BaseList);

    public static bool IsCommaInArgumentOrParameterList(this SyntaxToken token)
        => token.Kind() == SyntaxKind.CommaToken && (token.Parent.IsAnyArgumentList() || token.Parent?.Kind() is SyntaxKind.ParameterList or SyntaxKind.FunctionPointerParameterList);

    public static bool IsOpenParenInParameterListOfParenthesizedLambdaExpression(this SyntaxToken token)
        => token.Kind() == SyntaxKind.OpenParenToken && token.Parent.IsKind(SyntaxKind.ParameterList) && token.Parent.Parent.IsKind(SyntaxKind.ParenthesizedLambdaExpression);

    public static bool IsLambdaBodyBlock(this SyntaxNode node)
    {
        if (node.Kind() != SyntaxKind.Block)
        {
            return false;
        }

        return node.Parent?.Kind() is SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression;
    }

    public static bool IsAnonymousMethodBlock(this SyntaxNode node)
    {
        if (node.Kind() != SyntaxKind.Block)
        {
            return false;
        }

        return node.IsParentKind(SyntaxKind.AnonymousMethodExpression);
    }

    public static bool IsSemicolonInForStatement(this SyntaxToken token)
    {
        return
            token.Kind() == SyntaxKind.SemicolonToken &&
            token.Parent is ForStatementSyntax forStatement &&
            (forStatement.FirstSemicolonToken == token || forStatement.SecondSemicolonToken == token);
    }

    public static bool IsSemicolonOfEmbeddedStatement(this SyntaxToken token)
    {
        if (token.Kind() != SyntaxKind.SemicolonToken)
        {
            return false;
        }

        if (token.Parent is not StatementSyntax statement ||
            statement.GetLastToken() != token)
        {
            return false;
        }

        return IsEmbeddedStatement(statement);
    }

    public static bool IsCloseBraceOfExpression(this SyntaxToken token)
    {
        if (token.Kind() != SyntaxKind.CloseBraceToken)
        {
            return false;
        }

        return token.Parent is ExpressionSyntax || token.Parent.IsKind(SyntaxKind.PropertyPatternClause);
    }

    public static bool IsCloseBraceOfEmbeddedBlock(this SyntaxToken token)
    {
        if (token.Kind() != SyntaxKind.CloseBraceToken)
        {
            return false;
        }

        if (token.Parent is not BlockSyntax block ||
            block.CloseBraceToken != token)
        {
            return false;
        }

        return IsEmbeddedStatement(block);
    }

    public static bool IsEmbeddedStatement([NotNullWhen(true)] this SyntaxNode? node)
    {
        SyntaxNode? statementOrElse = node as StatementSyntax;
        statementOrElse ??= node as ElseClauseSyntax;

        return statementOrElse != null
            && statementOrElse.Parent != null
            && statementOrElse.Parent.IsEmbeddedStatementOwner();
    }

    public static bool IsCommaInEnumDeclaration(this SyntaxToken token)
    {
        return token.Kind() == SyntaxKind.CommaToken &&
            token.Parent.IsKind(SyntaxKind.EnumDeclaration);
    }

    public static bool IsCommaInAnyArgumentsList(this SyntaxToken token)
    {
        return token.Kind() == SyntaxKind.CommaToken &&
            token.Parent.IsAnyArgumentList();
    }

    public static bool IsOpenParenOfParenthesizedExpression(this SyntaxToken token)
        => token.Parent is ParenthesizedExpressionSyntax parenthesizedExpression && parenthesizedExpression.OpenParenToken.Equals(token);

    public static bool IsCloseParenOfParenthesizedExpression(this SyntaxToken token)
        => token.Parent is ParenthesizedExpressionSyntax parenthesizedExpression && parenthesizedExpression.CloseParenToken.Equals(token);

    public static bool IsParenInArgumentList(this SyntaxToken token)
    {
        var parent = token.Parent ?? throw new ArgumentNullException(nameof(token));
        switch (parent.Kind())
        {
            case SyntaxKind.SizeOfExpression:
                var sizeOfExpression = (SizeOfExpressionSyntax)parent;
                return sizeOfExpression.OpenParenToken == token || sizeOfExpression.CloseParenToken == token;

            case SyntaxKind.TypeOfExpression:
                var typeOfExpression = (TypeOfExpressionSyntax)parent;
                return typeOfExpression.OpenParenToken == token || typeOfExpression.CloseParenToken == token;

            case SyntaxKind.CheckedExpression:
            case SyntaxKind.UncheckedExpression:
                var checkedOfExpression = (CheckedExpressionSyntax)parent;
                return checkedOfExpression.OpenParenToken == token || checkedOfExpression.CloseParenToken == token;

            case SyntaxKind.DefaultExpression:
                var defaultExpression = (DefaultExpressionSyntax)parent;
                return defaultExpression.OpenParenToken == token || defaultExpression.CloseParenToken == token;

            case SyntaxKind.MakeRefExpression:
                var makeRefExpression = (MakeRefExpressionSyntax)parent;
                return makeRefExpression.OpenParenToken == token || makeRefExpression.CloseParenToken == token;

            case SyntaxKind.RefTypeExpression:
                var refTypeOfExpression = (RefTypeExpressionSyntax)parent;
                return refTypeOfExpression.OpenParenToken == token || refTypeOfExpression.CloseParenToken == token;

            case SyntaxKind.RefValueExpression:
                var refValueExpression = (RefValueExpressionSyntax)parent;
                return refValueExpression.OpenParenToken == token || refValueExpression.CloseParenToken == token;

            case SyntaxKind.ArgumentList:
                var argumentList = (ArgumentListSyntax)parent;
                return argumentList.OpenParenToken == token || argumentList.CloseParenToken == token;

            case SyntaxKind.AttributeArgumentList:
                var attributeArgumentList = (AttributeArgumentListSyntax)parent;
                return attributeArgumentList.OpenParenToken == token || attributeArgumentList.CloseParenToken == token;
        }

        return false;
    }

    public static bool IsEqualsTokenInAutoPropertyInitializers(this SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.EqualsToken) &&
            token.Parent.IsKind(SyntaxKind.EqualsValueClause) &&
            token.Parent.Parent.IsKind(SyntaxKind.PropertyDeclaration);
    }

    public static bool IsCloseParenInStatement(this SyntaxToken token)
    {
        if (token.Parent is not StatementSyntax statement)
        {
            return false;
        }

        return statement switch
        {
            IfStatementSyntax ifStatement => ifStatement.CloseParenToken.Equals(token),
            SwitchStatementSyntax switchStatement => switchStatement.CloseParenToken.Equals(token),
            WhileStatementSyntax whileStatement => whileStatement.CloseParenToken.Equals(token),
            DoStatementSyntax doStatement => doStatement.CloseParenToken.Equals(token),
            ForStatementSyntax forStatement => forStatement.CloseParenToken.Equals(token),
            CommonForEachStatementSyntax foreachStatement => foreachStatement.CloseParenToken.Equals(token),
            LockStatementSyntax lockStatement => lockStatement.CloseParenToken.Equals(token),
            UsingStatementSyntax usingStatement => usingStatement.CloseParenToken.Equals(token),
            FixedStatementSyntax fixedStatement => fixedStatement.CloseParenToken.Equals(token),
            _ => false,
        };
    }

    public static bool IsDotInMemberAccessOrQualifiedName(this SyntaxToken token)
        => token.IsDotInMemberAccess() || (token.Kind() == SyntaxKind.DotToken && token.Parent.IsKind(SyntaxKind.QualifiedName));

    public static bool IsDotInMemberAccess(this SyntaxToken token)
    {
        if (token.Parent is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        return token.Kind() == SyntaxKind.DotToken
            && memberAccess.OperatorToken.Equals(token);
    }

    public static bool IsGenericGreaterThanToken(this SyntaxToken token)
    {
        if (token.Kind() == SyntaxKind.GreaterThanToken)
            return token.Parent is (kind: SyntaxKind.TypeParameterList or SyntaxKind.TypeArgumentList);

        return false;
    }

    public static bool IsCommaInInitializerExpression(this SyntaxToken token)
    {
        return token.Kind() == SyntaxKind.CommaToken &&
                ((token.Parent is InitializerExpressionSyntax) ||
                 (token.Parent is AnonymousObjectCreationExpressionSyntax));
    }

    public static bool IsColonInCasePatternSwitchLabel(this SyntaxToken token)
        => token.Kind() == SyntaxKind.ColonToken && token.Parent is CasePatternSwitchLabelSyntax;

    public static bool IsColonInSwitchExpressionArm(this SyntaxToken token)
        => token.Kind() == SyntaxKind.ColonToken && token.Parent.IsKind(SyntaxKind.SwitchExpressionArm);

    public static bool IsCommaInSwitchExpression(this SyntaxToken token)
        => token.Kind() == SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKind.SwitchExpression);

    public static bool IsCommaInPropertyPatternClause(this SyntaxToken token)
        => token.Kind() == SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKind.PropertyPatternClause);

    public static bool IsIdentifierInLabeledStatement(this SyntaxToken token)
    {
        return token.Kind() == SyntaxKind.IdentifierToken &&
            token.Parent is LabeledStatementSyntax labeledStatement &&
            labeledStatement.Identifier == token;
    }

    public static bool IsColonInSwitchLabel(this SyntaxToken token)
        => FormattingRangeHelper.IsColonInSwitchLabel(token);

    public static bool IsColonInLabeledStatement(this SyntaxToken token)
    {
        return token.Kind() == SyntaxKind.ColonToken &&
            token.Parent is LabeledStatementSyntax labeledStatement &&
            labeledStatement.ColonToken == token;
    }

    public static bool IsEmbeddedStatementOwnerWithCloseParen([NotNullWhen(true)] this SyntaxNode? node)
    {
        return node is IfStatementSyntax or
               WhileStatementSyntax or
               ForStatementSyntax or
               CommonForEachStatementSyntax or
               UsingStatementSyntax or
               FixedStatementSyntax or
               LockStatementSyntax;
    }

    public static bool IsNestedQueryExpression(this SyntaxToken token)
        => token.Kind() == SyntaxKind.InKeyword && token.Parent is FromClauseSyntax { Expression: QueryExpressionSyntax };

    public static bool IsFirstFromKeywordInExpression(this SyntaxToken token)
    {
        return token.Kind() == SyntaxKind.FromKeyword &&
               token.Parent?.Parent is QueryExpressionSyntax queryExpression &&
               queryExpression.GetFirstToken().Equals(token);
    }

    public static bool IsInitializerForObjectOrAnonymousObjectCreationExpression([NotNullWhen(true)] this SyntaxNode? node)
    {
        if (node is InitializerExpressionSyntax initializer)
        {
            var parent = initializer.Parent;
            if (parent is AnonymousObjectCreationExpressionSyntax)
            {
                return true;
            }

            if (parent is BaseObjectCreationExpressionSyntax)
            {
                if (initializer.Expressions.Count <= 0)
                {
                    return true;
                }

                var expression = initializer.Expressions[0];
                if (expression.Kind() == SyntaxKind.SimpleAssignmentExpression)
                {
                    return true;
                }
            }

            return false;
        }
        else if (node is AnonymousObjectMemberDeclaratorSyntax anonymousObjectInitializer)
        {
            return anonymousObjectInitializer.Parent is AnonymousObjectCreationExpressionSyntax;
        }
        else
        {
            return false;
        }
    }

    public static bool IsInitializerForArrayOrCollectionCreationExpression([NotNullWhen(true)] this SyntaxNode? node)
    {
        if (node is InitializerExpressionSyntax initializer)
        {
            var parent = initializer.Parent;
            if (parent is ArrayCreationExpressionSyntax ||
                parent is ImplicitArrayCreationExpressionSyntax ||
                parent is StackAllocArrayCreationExpressionSyntax ||
                parent is ImplicitStackAllocArrayCreationExpressionSyntax ||
                parent is EqualsValueClauseSyntax ||
                parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                return true;
            }

            if (parent is BaseObjectCreationExpressionSyntax)
            {
                return !IsInitializerForObjectOrAnonymousObjectCreationExpression(initializer);
            }

            return false;
        }
        else if (node is AnonymousObjectMemberDeclaratorSyntax anonymousObjectInitializer)
        {
            var parent = anonymousObjectInitializer.Parent;
            if (parent is ArrayCreationExpressionSyntax ||
                parent is ImplicitArrayCreationExpressionSyntax ||
                parent is EqualsValueClauseSyntax ||
                parent is BaseObjectCreationExpressionSyntax ||
                parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                return true;
            }

            return false;
        }
        else
        {
            return false;
        }
    }

    public static bool ParenOrBracketContainsNothing(this SyntaxToken token1, SyntaxToken token2)
    {
        return (token1.Kind() == SyntaxKind.OpenParenToken && token2.Kind() == SyntaxKind.CloseParenToken) ||
               (token1.Kind() == SyntaxKind.OpenBracketToken && token2.Kind() == SyntaxKind.CloseBracketToken);
    }

    public static bool IsLastTokenInLabelStatement(this SyntaxToken token)
    {
        if (token.Kind() is not SyntaxKind.SemicolonToken and not SyntaxKind.CloseBraceToken)
        {
            return false;
        }

        if (token.Parent == null)
        {
            return false;
        }

        return token.Parent.Parent is LabeledStatementSyntax;
    }

    public static (SyntaxToken firstToken, SyntaxToken lastToken) GetFirstAndLastMemberDeclarationTokensAfterAttributes(this MemberDeclarationSyntax node)
    {
        Contract.ThrowIfNull(node);

        // there are no attributes associated with the node. return back first and last token of the node.
        var attributes = node.GetAttributes();
        if (attributes.Count == 0)
        {
            return (node.GetFirstToken(includeZeroWidth: true), node.GetLastToken(includeZeroWidth: true));
        }

        var lastToken = node.GetLastToken(includeZeroWidth: true);
        var lastAttributeToken = attributes.Last().GetLastToken(includeZeroWidth: true);
        if (lastAttributeToken.Equals(lastToken))
        {
            return default;
        }

        var firstTokenAfterAttribute = lastAttributeToken.GetNextToken(includeZeroWidth: true);

        // there are attributes, get first token after the tokens belong to attributes
        return (firstTokenAfterAttribute, lastToken);
    }

    public static bool IsPlusOrMinusExpression(this SyntaxToken token)
    {
        if (token.Kind() is not SyntaxKind.PlusToken and not SyntaxKind.MinusToken)
        {
            return false;
        }

        return token.Parent is PrefixUnaryExpressionSyntax;
    }

    public static bool IsInterpolation(this SyntaxToken currentToken)
        => currentToken.Parent.IsKind(SyntaxKind.Interpolation);

    /// <summary>
    /// Checks whether currentToken is the opening paren of a deconstruction-declaration in var form, such as <c>var (x, y) = ...</c>
    /// </summary>
    public static bool IsOpenParenInVarDeconstructionDeclaration(this SyntaxToken currentToken)
    {
        return currentToken.Kind() == SyntaxKind.OpenParenToken && currentToken is { Parent: ParenthesizedVariableDesignationSyntax, Parent.Parent: DeclarationExpressionSyntax };
    }

    /// <summary>
    /// Check whether the currentToken is a comma and is a delimiter between arguments inside a tuple expression.
    /// </summary>
    public static bool IsCommaInTupleExpression(this SyntaxToken currentToken)
    {
        return currentToken.IsKind(SyntaxKind.CommaToken) &&
            currentToken.Parent.IsKind(SyntaxKind.TupleExpression);
    }

    public static bool IsCommaInCollectionExpression(this SyntaxToken token)
        => token.Kind() == SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKind.CollectionExpression);
}
