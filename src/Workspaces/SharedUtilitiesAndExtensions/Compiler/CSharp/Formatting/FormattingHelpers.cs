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

    extension(SyntaxToken token)
    {
        public string GetIndent()
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

        public bool IsOpenParenInParameterListOfAConversionOperatorDeclaration()
            => token.IsOpenParenInParameterList() && token.Parent.IsParentKind(SyntaxKind.ConversionOperatorDeclaration);

        public bool IsOpenParenInParameterListOfAOperationDeclaration()
            => token.IsOpenParenInParameterList() && token.Parent.IsParentKind(SyntaxKind.OperatorDeclaration);

        public bool IsOpenParenInParameterList()
            => token.Kind() == SyntaxKind.OpenParenToken && token.Parent.IsKind(SyntaxKind.ParameterList);

        public bool IsCloseParenInParameterList()
            => token.Kind() == SyntaxKind.CloseParenToken && token.Parent.IsKind(SyntaxKind.ParameterList);

        public bool IsOpenParenInArgumentListOrPositionalPattern()
        {
            return token.Kind() == SyntaxKind.OpenParenToken &&
                IsTokenInArgumentListOrPositionalPattern(token);
        }

        public bool IsCloseParenInArgumentListOrPositionalPattern()
        {
            return token.Kind() == SyntaxKind.CloseParenToken &&
                IsTokenInArgumentListOrPositionalPattern(token);
        }

        public bool IsColonInTypeBaseList()
            => token.Kind() == SyntaxKind.ColonToken && token.Parent.IsKind(SyntaxKind.BaseList);

        public bool IsCommaInArgumentOrParameterList()
            => token.Kind() == SyntaxKind.CommaToken && (token.Parent.IsAnyArgumentList() || token.Parent?.Kind() is SyntaxKind.ParameterList or SyntaxKind.FunctionPointerParameterList);

        public bool IsOpenParenInParameterListOfParenthesizedLambdaExpression()
            => token.Kind() == SyntaxKind.OpenParenToken && token.Parent.IsKind(SyntaxKind.ParameterList) && token.Parent.Parent.IsKind(SyntaxKind.ParenthesizedLambdaExpression);

        public bool IsSemicolonInForStatement()
        {
            return
                token.Kind() == SyntaxKind.SemicolonToken &&
                token.Parent is ForStatementSyntax forStatement &&
                (forStatement.FirstSemicolonToken == token || forStatement.SecondSemicolonToken == token);
        }

        public bool IsSemicolonOfEmbeddedStatement()
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

        public bool IsCloseBraceOfExpression()
        {
            if (token.Kind() != SyntaxKind.CloseBraceToken)
            {
                return false;
            }

            return token.Parent is ExpressionSyntax || token.Parent.IsKind(SyntaxKind.PropertyPatternClause);
        }

        public bool IsCloseBraceOfEmbeddedBlock()
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

        public bool IsCommaInEnumDeclaration()
        {
            return token.Kind() == SyntaxKind.CommaToken &&
                token.Parent.IsKind(SyntaxKind.EnumDeclaration);
        }

        public bool IsCommaInAnyArgumentsList()
        {
            return token.Kind() == SyntaxKind.CommaToken &&
                token.Parent.IsAnyArgumentList();
        }

        public bool IsParenInParenthesizedExpression()
        {
            if (token.Parent is not ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                return false;
            }

            return parenthesizedExpression.OpenParenToken.Equals(token) || parenthesizedExpression.CloseParenToken.Equals(token);
        }

        public bool IsParenInArgumentList()
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

        public bool IsEqualsTokenInAutoPropertyInitializers()
        {
            return token.IsKind(SyntaxKind.EqualsToken) &&
                token.Parent.IsKind(SyntaxKind.EqualsValueClause) &&
                token.Parent.Parent.IsKind(SyntaxKind.PropertyDeclaration);
        }

        public bool IsCloseParenInStatement()
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

        public bool IsDotInMemberAccessOrQualifiedName()
            => token.IsDotInMemberAccess() || (token.Kind() == SyntaxKind.DotToken && token.Parent.IsKind(SyntaxKind.QualifiedName));

        public bool IsDotInMemberAccess()
        {
            if (token.Parent is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            return token.Kind() == SyntaxKind.DotToken
                && memberAccess.OperatorToken.Equals(token);
        }

        public bool IsGenericGreaterThanToken()
        {
            if (token.Kind() == SyntaxKind.GreaterThanToken)
                return token.Parent is (kind: SyntaxKind.TypeParameterList or SyntaxKind.TypeArgumentList);

            return false;
        }

        public bool IsCommaInInitializerExpression()
        {
            return token.Kind() == SyntaxKind.CommaToken &&
                    ((token.Parent is InitializerExpressionSyntax) ||
                     (token.Parent is AnonymousObjectCreationExpressionSyntax));
        }

        public bool IsColonInCasePatternSwitchLabel()
            => token.Kind() == SyntaxKind.ColonToken && token.Parent is CasePatternSwitchLabelSyntax;

        public bool IsColonInSwitchExpressionArm()
            => token.Kind() == SyntaxKind.ColonToken && token.Parent.IsKind(SyntaxKind.SwitchExpressionArm);

        public bool IsCommaInSwitchExpression()
            => token.Kind() == SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKind.SwitchExpression);

        public bool IsCommaInPropertyPatternClause()
            => token.Kind() == SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKind.PropertyPatternClause);

        public bool IsIdentifierInLabeledStatement()
        {
            return token.Kind() == SyntaxKind.IdentifierToken &&
                token.Parent is LabeledStatementSyntax labeledStatement &&
                labeledStatement.Identifier == token;
        }

        public bool IsColonInSwitchLabel()
            => FormattingRangeHelper.IsColonInSwitchLabel(token);

        public bool IsColonInLabeledStatement()
        {
            return token.Kind() == SyntaxKind.ColonToken &&
                token.Parent is LabeledStatementSyntax labeledStatement &&
                labeledStatement.ColonToken == token;
        }

        public bool IsNestedQueryExpression()
        {
            return token.Kind() == SyntaxKind.InKeyword &&
                   token.Parent is FromClauseSyntax fromClause &&
                   fromClause.Expression is QueryExpressionSyntax;
        }

        public bool IsFirstFromKeywordInExpression()
        {
            return token.Kind() == SyntaxKind.FromKeyword &&
                   token.Parent?.Parent is QueryExpressionSyntax queryExpression &&
                   queryExpression.GetFirstToken().Equals(token);
        }

        public bool IsLastTokenInLabelStatement()
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

        public bool IsPlusOrMinusExpression()
        {
            if (token.Kind() is not SyntaxKind.PlusToken and not SyntaxKind.MinusToken)
            {
                return false;
            }

            return token.Parent is PrefixUnaryExpressionSyntax;
        }

        public bool IsCommaInCollectionExpression()
            => token.Kind() == SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKind.CollectionExpression);
    }

    extension(IEnumerable<SyntaxTrivia> trivia)
    {
        public string ContentBeforeLastNewLine()
        {
            var leading = trivia.AsString();
            var lastNewLinePos = leading.LastIndexOf(NewLine, StringComparison.Ordinal);
            if (lastNewLinePos == -1)
            {
                return string.Empty;
            }

            return leading[..lastNewLinePos];
        }
    }

    extension(SyntaxNode? node)
    {
        public (SyntaxToken openBrace, SyntaxToken closeBrace) GetBracePair()
        => node.GetBraces();

        public (SyntaxToken openBracket, SyntaxToken closeBracket) GetBracketPair()
            => node.GetBrackets();
    }

    extension((SyntaxToken openBracketOrBrace, SyntaxToken closeBracketOrBrace) bracketOrBracePair)
    {
        public bool IsValidBracketOrBracePair()
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

    extension(SyntaxNode node)
    {
        public bool IsLambdaBodyBlock()
        {
            if (node.Kind() != SyntaxKind.Block)
            {
                return false;
            }

            return node.Parent?.Kind() is SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression;
        }

        public bool IsAnonymousMethodBlock()
        {
            if (node.Kind() != SyntaxKind.Block)
            {
                return false;
            }

            return node.IsParentKind(SyntaxKind.AnonymousMethodExpression);
        }
    }

    extension([NotNullWhen(true)] SyntaxNode? node)
    {
        public bool IsEmbeddedStatement()
        {
            SyntaxNode? statementOrElse = node as StatementSyntax;
            statementOrElse ??= node as ElseClauseSyntax;

            return statementOrElse != null
                && statementOrElse.Parent != null
                && statementOrElse.Parent.IsEmbeddedStatementOwner();
        }

        public bool IsEmbeddedStatementOwnerWithCloseParen()
        {
            return node is IfStatementSyntax or
                   WhileStatementSyntax or
                   ForStatementSyntax or
                   CommonForEachStatementSyntax or
                   UsingStatementSyntax or
                   FixedStatementSyntax or
                   LockStatementSyntax;
        }

        public bool IsInitializerForObjectOrAnonymousObjectCreationExpression()
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

        public bool IsInitializerForArrayOrCollectionCreationExpression()
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
    }

    extension(SyntaxToken token1)
    {
        public bool ParenOrBracketContainsNothing(SyntaxToken token2)
        {
            return (token1.Kind() == SyntaxKind.OpenParenToken && token2.Kind() == SyntaxKind.CloseParenToken) ||
                   (token1.Kind() == SyntaxKind.OpenBracketToken && token2.Kind() == SyntaxKind.CloseBracketToken);
        }
    }

    extension(MemberDeclarationSyntax node)
    {
        public (SyntaxToken firstToken, SyntaxToken lastToken) GetFirstAndLastMemberDeclarationTokensAfterAttributes()
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
    }

    extension(SyntaxToken currentToken)
    {
        public bool IsInterpolation()
        => currentToken.Parent.IsKind(SyntaxKind.Interpolation);

        /// <summary>
        /// Checks whether currentToken is the opening paren of a deconstruction-declaration in var form, such as <c>var (x, y) = ...</c>
        /// </summary>
        public bool IsOpenParenInVarDeconstructionDeclaration()
        {
            return currentToken.Kind() == SyntaxKind.OpenParenToken &&
                currentToken.Parent is ParenthesizedVariableDesignationSyntax &&
                currentToken.Parent.Parent is DeclarationExpressionSyntax;
        }

        /// <summary>
        /// Check whether the currentToken is a comma and is a delimiter between arguments inside a tuple expression.
        /// </summary>
        public bool IsCommaInTupleExpression()
        {
            return currentToken.IsKind(SyntaxKind.CommaToken) &&
                currentToken.Parent.IsKind(SyntaxKind.TupleExpression);
        }
    }
}
