// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
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
                indent = indent.Substring(start, indent.Length - start);
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

            return leading.Substring(0, lastNewLinePos);
        }

        public static ValueTuple<SyntaxToken, SyntaxToken> GetBracePair(this SyntaxNode node)
        {
            return node.GetBraces();
        }

        public static bool IsValidBracePair(this ValueTuple<SyntaxToken, SyntaxToken> bracePair)
        {
            if (bracePair.Item1.IsKind(SyntaxKind.None) ||
                bracePair.Item1.IsMissing ||
                bracePair.Item2.IsKind(SyntaxKind.None))
            {
                return false;
            }

            // don't check whether token is actually braces as long as it is not none.
            return true;
        }

        public static bool IsOpenParenInParameterListOfAConversionOperatorDeclaration(this SyntaxToken token)
        {
            return token.IsOpenParenInParameterList() && token.Parent.IsParentKind(SyntaxKind.ConversionOperatorDeclaration);
        }

        public static bool IsOpenParenInParameterListOfAOperationDeclaration(this SyntaxToken token)
        {
            return token.IsOpenParenInParameterList() && token.Parent.IsParentKind(SyntaxKind.OperatorDeclaration);
        }

        public static bool IsOpenParenInParameterList(this SyntaxToken token)
        {
            return token.Kind() == SyntaxKind.OpenParenToken && token.Parent.Kind() == SyntaxKind.ParameterList;
        }

        public static bool IsCloseParenInParameterList(this SyntaxToken token)
        {
            return token.Kind() == SyntaxKind.CloseParenToken && token.Parent.Kind() == SyntaxKind.ParameterList;
        }

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
            if (token.Parent.IsKind(SyntaxKind.ArgumentList, SyntaxKind.AttributeArgumentList))
            {
                return true;
            }

            // Positional patterns
            if (token.Parent.IsKind(SyntaxKindEx.PositionalPatternClause) && token.Parent.Parent.IsKind(SyntaxKindEx.RecursivePattern))
            {
                // Avoid treating tuple expressions as positional patterns for formatting
                return token.Parent.Parent.GetFirstToken() != token;
            }

            return false;
        }

        public static bool IsColonInTypeBaseList(this SyntaxToken token)
        {
            return token.Kind() == SyntaxKind.ColonToken && token.Parent.Kind() == SyntaxKind.BaseList;
        }

        public static bool IsCommaInArgumentOrParameterList(this SyntaxToken token)
        {
            return token.Kind() == SyntaxKind.CommaToken && (token.Parent.IsAnyArgumentList() || token.Parent.Kind() == SyntaxKind.ParameterList);
        }

        public static bool IsLambdaBodyBlock(this SyntaxNode node)
        {
            if (node.Kind() != SyntaxKind.Block)
            {
                return false;
            }

            return node.IsParentKind(SyntaxKind.SimpleLambdaExpression) || node.IsParentKind(SyntaxKind.ParenthesizedLambdaExpression);
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
            var forStatement = token.Parent as ForStatementSyntax;
            return
                token.Kind() == SyntaxKind.SemicolonToken &&
                forStatement != null &&
                (forStatement.FirstSemicolonToken == token || forStatement.SecondSemicolonToken == token);
        }

        public static bool IsSemicolonOfEmbeddedStatement(this SyntaxToken token)
        {
            if (token.Kind() != SyntaxKind.SemicolonToken)
            {
                return false;
            }

            if (!(token.Parent is StatementSyntax statement) ||
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

            return token.Parent is ExpressionSyntax || token.Parent.IsKind(SyntaxKindEx.PropertyPatternClause);
        }

        public static bool IsCloseBraceOfEmbeddedBlock(this SyntaxToken token)
        {
            if (token.Kind() != SyntaxKind.CloseBraceToken)
            {
                return false;
            }

            if (!(token.Parent is BlockSyntax block) ||
                block.CloseBraceToken != token)
            {
                return false;
            }

            return IsEmbeddedStatement(block);
        }

        public static bool IsEmbeddedStatement(this SyntaxNode node)
        {
            SyntaxNode statementOrElse = node as StatementSyntax;
            if (statementOrElse == null)
            {
                statementOrElse = node as ElseClauseSyntax;
            }

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

        public static bool IsParenInParenthesizedExpression(this SyntaxToken token)
        {
            if (!(token.Parent is ParenthesizedExpressionSyntax parenthesizedExpression))
            {
                return false;
            }

            return parenthesizedExpression.OpenParenToken.Equals(token) || parenthesizedExpression.CloseParenToken.Equals(token);
        }

        public static bool IsParenInArgumentList(this SyntaxToken token)
        {
            var parent = token.Parent;
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
            if (!(token.Parent is StatementSyntax statement))
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
        {
            return token.IsDotInMemberAccess() || (token.Kind() == SyntaxKind.DotToken && token.Parent.Kind() == SyntaxKind.QualifiedName);
        }

        public static bool IsDotInMemberAccess(this SyntaxToken token)
        {
            if (!(token.Parent is MemberAccessExpressionSyntax memberAccess))
            {
                return false;
            }

            return token.Kind() == SyntaxKind.DotToken
                && memberAccess.OperatorToken.Equals(token);
        }

        public static bool IsGenericGreaterThanToken(this SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.GreaterThanToken)
            {
                return token.Parent.IsKind(SyntaxKind.TypeParameterList, SyntaxKind.TypeArgumentList);
            }

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
            => token.Kind() == SyntaxKind.ColonToken && token.Parent.IsKind(SyntaxKindEx.SwitchExpressionArm);

        public static bool IsCommaInSwitchExpression(this SyntaxToken token)
            => token.Kind() == SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKindEx.SwitchExpression);

        public static bool IsCommaInPropertyPatternClause(this SyntaxToken token)
            => token.Kind() == SyntaxKind.CommaToken && token.Parent.IsKind(SyntaxKindEx.PropertyPatternClause);

        public static bool IsIdentifierInLabeledStatement(this SyntaxToken token)
        {
            var labeledStatement = token.Parent as LabeledStatementSyntax;
            return token.Kind() == SyntaxKind.IdentifierToken &&
                labeledStatement != null &&
                labeledStatement.Identifier == token;
        }

        public static bool IsColonInSwitchLabel(this SyntaxToken token)
        {
            return FormattingRangeHelper.IsColonInSwitchLabel(token);
        }

        public static bool IsColonInLabeledStatement(this SyntaxToken token)
        {
            var labeledStatement = token.Parent as LabeledStatementSyntax;
            return token.Kind() == SyntaxKind.ColonToken &&
                labeledStatement != null &&
                labeledStatement.ColonToken == token;
        }

        public static bool IsEmbeddedStatementOwnerWithCloseParen(this SyntaxNode node)
        {
            return node is IfStatementSyntax ||
                   node is WhileStatementSyntax ||
                   node is ForStatementSyntax ||
                   node is CommonForEachStatementSyntax ||
                   node is UsingStatementSyntax ||
                   node is FixedStatementSyntax ||
                   node is LockStatementSyntax;
        }

        public static bool IsNestedQueryExpression(this SyntaxToken token)
        {
            var fromClause = token.Parent as FromClauseSyntax;
            return token.Kind() == SyntaxKind.InKeyword &&
                   fromClause != null &&
                   fromClause.Expression is QueryExpressionSyntax;
        }

        public static bool IsFirstFromKeywordInExpression(this SyntaxToken token)
        {
            var queryExpression = token.Parent.Parent as QueryExpressionSyntax;
            return token.Kind() == SyntaxKind.FromKeyword &&
                   queryExpression != null &&
                   queryExpression.GetFirstToken().Equals(token);
        }

        public static bool IsInitializerForObjectOrAnonymousObjectCreationExpression(this SyntaxNode node)
        {
            var initializer = node as InitializerExpressionSyntax;
            AnonymousObjectMemberDeclaratorSyntax anonymousObjectInitializer = null;
            if (initializer == null)
            {
                anonymousObjectInitializer = node as AnonymousObjectMemberDeclaratorSyntax;
                if (anonymousObjectInitializer == null)
                {
                    return false;
                }
            }

            var parent = initializer != null ? initializer.Parent : anonymousObjectInitializer.Parent;
            if (parent is AnonymousObjectCreationExpressionSyntax)
            {
                return true;
            }

            if (parent is ObjectCreationExpressionSyntax)
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

        public static bool IsInitializerForArrayOrCollectionCreationExpression(this SyntaxNode node)
        {
            var initializer = node as InitializerExpressionSyntax;
            AnonymousObjectMemberDeclaratorSyntax anonymousObjectInitializer = null;
            if (initializer == null)
            {
                anonymousObjectInitializer = node as AnonymousObjectMemberDeclaratorSyntax;
                if (anonymousObjectInitializer == null)
                {
                    return false;
                }
            }

            var parent = initializer != null ? initializer.Parent : anonymousObjectInitializer.Parent;
            if (parent is ArrayCreationExpressionSyntax ||
                parent is ImplicitArrayCreationExpressionSyntax ||
                parent is EqualsValueClauseSyntax ||
                parent.Kind() == SyntaxKind.SimpleAssignmentExpression)
            {
                return true;
            }

            if (parent is ObjectCreationExpressionSyntax)
            {
                return !IsInitializerForObjectOrAnonymousObjectCreationExpression(initializer);
            }

            return false;
        }

        public static bool ParenOrBracketContainsNothing(this SyntaxToken token1, SyntaxToken token2)
        {
            return (token1.Kind() == SyntaxKind.OpenParenToken && token2.Kind() == SyntaxKind.CloseParenToken) ||
                   (token1.Kind() == SyntaxKind.OpenBracketToken && token2.Kind() == SyntaxKind.CloseBracketToken);
        }

        public static bool IsLastTokenInLabelStatement(this SyntaxToken token)
        {
            if (token.Kind() != SyntaxKind.SemicolonToken && token.Kind() != SyntaxKind.CloseBraceToken)
            {
                return false;
            }

            if (token.Parent == null)
            {
                return false;
            }

            return token.Parent.Parent is LabeledStatementSyntax;
        }

        public static ValueTuple<SyntaxToken, SyntaxToken> GetFirstAndLastMemberDeclarationTokensAfterAttributes(this MemberDeclarationSyntax node)
        {
            Contract.ThrowIfNull(node);

            // there are no attributes associated with the node. return back first and last token of the node.
            var attributes = node.GetAttributes();
            if (attributes.Count == 0)
            {
                return ValueTuple.Create(node.GetFirstToken(includeZeroWidth: true), node.GetLastToken(includeZeroWidth: true));
            }

            var lastToken = node.GetLastToken(includeZeroWidth: true);
            var lastAttributeToken = attributes.Last().GetLastToken(includeZeroWidth: true);
            if (lastAttributeToken.Equals(lastToken))
            {
                return ValueTuple.Create(default(SyntaxToken), default(SyntaxToken));
            }

            var firstTokenAfterAttribute = lastAttributeToken.GetNextToken(includeZeroWidth: true);

            // there are attributes, get first token after the tokens belong to attributes
            return ValueTuple.Create(firstTokenAfterAttribute, lastToken);
        }

        public static bool IsBlockBody(this SyntaxNode node)
        {
            Contract.ThrowIfNull(node);

            if (!(node is BlockSyntax blockNode) || blockNode.Parent == null)
            {
                return false;
            }

            switch (blockNode.Parent.Kind())
            {
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.CheckedStatement:
                case SyntaxKind.UncheckedStatement:
                case SyntaxKind.UnsafeStatement:
                case SyntaxKind.TryStatement:
                case SyntaxKind.CatchClause:
                case SyntaxKind.FinallyClause:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.UnknownAccessorDeclaration:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPlusOrMinusExpression(this SyntaxToken token)
        {
            if (token.Kind() != SyntaxKind.PlusToken && token.Kind() != SyntaxKind.MinusToken)
            {
                return false;
            }

            return token.Parent is PrefixUnaryExpressionSyntax;
        }

        public static bool IsInterpolation(this SyntaxToken currentToken)
        {
            return currentToken.Parent.IsKind(SyntaxKind.Interpolation);
        }

        /// <summary>
        /// Checks whether currentToken is the opening paren of a deconstruction-declaration in var form, such as <c>var (x, y) = ...</c>
        /// </summary>
        public static bool IsOpenParenInVarDeconstructionDeclaration(this SyntaxToken currentToken)
        {
            return currentToken.Kind() == SyntaxKind.OpenParenToken &&
                currentToken.Parent is ParenthesizedVariableDesignationSyntax &&
                currentToken.Parent.Parent is DeclarationExpressionSyntax;
        }

        /// <summary>
        /// Check whether the currentToken is a comma and is a delimiter between arguments inside a tuple expression.
        /// </summary>
        public static bool IsCommaInTupleExpression(this SyntaxToken currentToken)
        {
            return currentToken.IsKind(SyntaxKind.CommaToken) &&
                currentToken.Parent.IsKind(SyntaxKind.TupleExpression);
        }
    }
}
