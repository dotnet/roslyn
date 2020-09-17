// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal static class ISyntaxFactsExtensions
    {
        public static bool IsLegalIdentifier(this ISyntaxFacts syntaxFacts, string name)
        {
            if (name.Length == 0)
            {
                return false;
            }

            if (!syntaxFacts.IsIdentifierStartCharacter(name[0]))
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                if (!syntaxFacts.IsIdentifierPartCharacter(name[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsReservedOrContextualKeyword(this ISyntaxFacts syntaxFacts, SyntaxToken token)
            => syntaxFacts.IsReservedKeyword(token) || syntaxFacts.IsContextualKeyword(token);

        public static bool IsWord(this ISyntaxFacts syntaxFacts, SyntaxToken token)
        {
            return syntaxFacts.IsIdentifier(token)
                || syntaxFacts.IsReservedOrContextualKeyword(token)
                || syntaxFacts.IsPreprocessorKeyword(token);
        }

        public static bool IsAnyMemberAccessExpression(
            this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            return syntaxFacts.IsSimpleMemberAccessExpression(node) || syntaxFacts.IsPointerMemberAccessExpression(node);
        }

        public static bool IsRegularOrDocumentationComment(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
            => syntaxFacts.IsRegularComment(trivia) || syntaxFacts.IsDocumentationComment(trivia);

        public static ImmutableArray<SyntaxTrivia> GetTriviaAfterLeadingBlankLines(
            this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            var leadingBlankLines = syntaxFacts.GetLeadingBlankLines(node);
            return node.GetLeadingTrivia().Skip(leadingBlankLines.Length).ToImmutableArray();
        }

        public static void GetPartsOfAssignmentStatement(
            this ISyntaxFacts syntaxFacts, SyntaxNode statement,
            out SyntaxNode left, out SyntaxNode right)
        {
            syntaxFacts.GetPartsOfAssignmentStatement(statement, out left, out _, out right);
        }

        public static SyntaxNode GetExpressionOfInvocationExpression(
            this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfInvocationExpression(node, out var expression, out _);
            return expression;
        }

        public static SyntaxNode Unparenthesize(
            this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            SyntaxToken openParenToken;
            SyntaxNode operand;
            SyntaxToken closeParenToken;

            if (syntaxFacts.IsParenthesizedPattern(node))
            {
                syntaxFacts.GetPartsOfParenthesizedPattern(node,
                    out openParenToken, out operand, out closeParenToken);
            }
            else
            {
                syntaxFacts.GetPartsOfParenthesizedExpression(node,
                    out openParenToken, out operand, out closeParenToken);
            }

            var leadingTrivia = openParenToken.LeadingTrivia
                .Concat(openParenToken.TrailingTrivia)
                .Where(t => !syntaxFacts.IsElastic(t))
                .Concat(operand.GetLeadingTrivia());

            var trailingTrivia = operand.GetTrailingTrivia()
                .Concat(closeParenToken.LeadingTrivia)
                .Where(t => !syntaxFacts.IsElastic(t))
                .Concat(closeParenToken.TrailingTrivia);

            var resultNode = operand
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia);

            // If there's no trivia between the original node and the tokens around it, then add
            // elastic markers so the formatting engine will spaces if necessary to keep things
            // parseable.
            if (resultNode.GetLeadingTrivia().Count == 0)
            {
                var previousToken = node.GetFirstToken().GetPreviousToken();
                if (previousToken.TrailingTrivia.Count == 0 &&
                    syntaxFacts.IsWordOrNumber(previousToken) &&
                    syntaxFacts.IsWordOrNumber(resultNode.GetFirstToken()))
                {
                    resultNode = resultNode.WithPrependedLeadingTrivia(syntaxFacts.ElasticMarker);
                }
            }

            if (resultNode.GetTrailingTrivia().Count == 0)
            {
                var nextToken = node.GetLastToken().GetNextToken();
                if (nextToken.LeadingTrivia.Count == 0 &&
                    syntaxFacts.IsWordOrNumber(nextToken) &&
                    syntaxFacts.IsWordOrNumber(resultNode.GetLastToken()))
                {
                    resultNode = resultNode.WithAppendedTrailingTrivia(syntaxFacts.ElasticMarker);
                }
            }

            return resultNode;
        }

        private static bool IsWordOrNumber(this ISyntaxFacts syntaxFacts, SyntaxToken token)
            => syntaxFacts.IsWord(token) || syntaxFacts.IsNumericLiteral(token);

        public static bool SpansPreprocessorDirective(this ISyntaxFacts service, SyntaxNode node)
            => service.SpansPreprocessorDirective(SpecializedCollections.SingletonEnumerable(node));

        public static bool SpansPreprocessorDirective(this ISyntaxFacts service, params SyntaxNode[] nodes)
            => service.SpansPreprocessorDirective(nodes);

        public static bool IsWhitespaceOrEndOfLineTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
            => syntaxFacts.IsWhitespaceTrivia(trivia) || syntaxFacts.IsEndOfLineTrivia(trivia);

        public static void GetPartsOfBinaryExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node, out SyntaxNode left, out SyntaxNode right)
            => syntaxFacts.GetPartsOfBinaryExpression(node, out left, out _, out right);

        public static SyntaxNode GetPatternOfParenthesizedPattern(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfParenthesizedPattern(node, out _, out var pattern, out _);
            return pattern;
        }

        public static SyntaxNode GetExpressionOfParenthesizedExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfParenthesizedExpression(node, out _, out var expression, out _);
            return expression;
        }

        public static SyntaxToken GetOperatorTokenOfBinaryExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfBinaryExpression(node, out _, out var token, out _);
            return token;
        }

        public static bool IsAnonymousOrLocalFunction(this ISyntaxFacts syntaxFacts, SyntaxNode node)
            => syntaxFacts.IsAnonymousFunction(node) ||
               syntaxFacts.IsLocalFunctionStatement(node);

        public static SyntaxNode GetExpressionOfElementAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfElementAccessExpression(node, out var expression, out _);
            return expression;
        }

        public static SyntaxNode GetArgumentListOfElementAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfElementAccessExpression(node, out _, out var argumentList);
            return argumentList;
        }

        public static SyntaxNode GetExpressionOfConditionalAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfConditionalAccessExpression(node, out var expression, out _);
            return expression;
        }

        public static SyntaxToken GetOperatorTokenOfMemberAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            syntaxFacts.GetPartsOfMemberAccessExpression(node, out _, out var operatorToken, out _);
            return operatorToken;
        }

        public static void GetPartsOfMemberAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node, out SyntaxNode expression, out SyntaxNode name)
            => syntaxFacts.GetPartsOfMemberAccessExpression(node, out expression, out _, out name);

        public static void GetPartsOfConditionalAccessExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node, out SyntaxNode expression, out SyntaxNode whenNotNull)
            => syntaxFacts.GetPartsOfConditionalAccessExpression(node, out expression, out _, out whenNotNull);

        public static TextSpan GetSpanWithoutAttributes(this ISyntaxFacts syntaxFacts, SyntaxNode root, SyntaxNode node)
        {
            // Span without AttributeLists
            // - No AttributeLists -> original .Span
            // - Some AttributeLists -> (first non-trivia/comment Token.Span.Begin, original.Span.End)
            //   - We need to be mindful about comments due to:
            //      // [Test1]
            //      //Comment1
            //      [||]object Property1 { get; set; }
            //     the comment node being part of the next token's (`object`) leading trivia and not the AttributeList's node.
            // - In case only attribute is written we need to be careful to not to use next (unrelated) token as beginning current the node.
            var attributeList = syntaxFacts.GetAttributeLists(node);
            if (attributeList.Any())
            {
                var endOfAttributeLists = attributeList.Last().Span.End;
                var afterAttributesToken = root.FindTokenOnRightOfPosition(endOfAttributeLists);

                var endOfNode = node.Span.End;
                var startOfNodeWithoutAttributes = Math.Min(afterAttributesToken.Span.Start, endOfNode);

                return TextSpan.FromBounds(startOfNodeWithoutAttributes, endOfNode);
            }

            return node.Span;
        }

        /// <summary>
        /// Checks if the position is on the header of a type (from the start of the type up through it's name).
        /// </summary>
        public static bool IsOnTypeHeader(this ISyntaxFacts syntaxFacts, SyntaxNode root, int position, out SyntaxNode typeDeclaration)
            => syntaxFacts.IsOnTypeHeader(root, position, fullHeader: false, out typeDeclaration);

        /// <summary>
        /// Gets the statement container node for the statement <paramref name="node"/>.
        /// </summary>
        /// <param name="syntaxFacts">The <see cref="ISyntaxFacts"/> implementation.</param>
        /// <param name="node">The statement.</param>
        /// <returns>The statement container for <paramref name="node"/>.</returns>
        public static SyntaxNode? GetStatementContainer(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            for (var current = node; current is object; current = current.Parent)
            {
                if (syntaxFacts.IsStatementContainer(current.Parent))
                {
                    return current.Parent;
                }
            }

            return null;
        }

        /// <summary>
        /// Similar to <see cref="ISyntaxFacts.GetStandaloneExpression(SyntaxNode)"/>, this gets the containing
        /// expression that is actually a language expression and not just typed as an ExpressionSyntax for convenience.
        /// However, this goes beyond that that method in that if this expression is the RHS of a conditional access
        /// (i.e. <c>a?.b()</c>) it will also return the root of the conditional access expression tree.
        /// <para/> The intuition here is that this will give the topmost expression node that could realistically be
        /// replaced with any other expression.  For example, with <c>a?.b()</c> technically <c>.b()</c> is an
        /// expression.  But that cannot be replaced with something like <c>(1 + 1)</c> (as <c>a?.(1 + 1)</c> is not
        /// legal).  However, in <c>a?.b()</c>, then <c>a</c> itself could be replaced with <c>(1 + 1)?.b()</c> to form
        /// a legal expression.
        /// </summary>
        public static SyntaxNode GetRootStandaloneExpression(this ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            // First, make sure we're on a construct the language things is a standalone expression.
            var standalone = syntaxFacts.GetStandaloneExpression(node);

            // Then, if this is the RHS of a `?`, walk up to the top of that tree to get the final standalone expression.
            return syntaxFacts.GetRootConditionalAccessExpression(standalone) ?? standalone;
        }

        #region ISyntaxKinds forwarding methods

        #region trivia

        public static bool IsEndOfLineTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
            => trivia.RawKind == syntaxFacts.SyntaxKinds.EndOfLineTrivia;

        public static bool IsWhitespaceTrivia(this ISyntaxFacts syntaxFacts, SyntaxTrivia trivia)
            => trivia.RawKind == syntaxFacts.SyntaxKinds.WhitespaceTrivia;

        public static bool IsSkippedTokensTrivia(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.SkippedTokensTrivia;

        #endregion

        #region keywords

        public static bool IsAwaitKeyword(this ISyntaxFacts syntaxFacts, SyntaxToken token)
            => token.RawKind == syntaxFacts.SyntaxKinds.AwaitKeyword;

        public static bool IsGlobalNamespaceKeyword(this ISyntaxFacts syntaxFacts, SyntaxToken token)
            => token.RawKind == syntaxFacts.SyntaxKinds.GlobalKeyword;

        #endregion

        #region literal tokens

        public static bool IsCharacterLiteral(this ISyntaxFacts syntaxFacts, SyntaxToken token)
            => token.RawKind == syntaxFacts.SyntaxKinds.CharacterLiteralToken;

        public static bool IsStringLiteral(this ISyntaxFacts syntaxFacts, SyntaxToken token)
            => token.RawKind == syntaxFacts.SyntaxKinds.StringLiteralToken;

        #endregion

        #region tokens

        public static bool IsIdentifier(this ISyntaxFacts syntaxFacts, SyntaxToken token)
            => token.RawKind == syntaxFacts.SyntaxKinds.IdentifierToken;

        public static bool IsHashToken(this ISyntaxFacts syntaxFacts, SyntaxToken token)
            => token.RawKind == syntaxFacts.SyntaxKinds.HashToken;

        public static bool IsInterpolatedStringTextToken(this ISyntaxFacts syntaxFacts, SyntaxToken token)
            => token.RawKind == syntaxFacts.SyntaxKinds.InterpolatedStringTextToken;

        #endregion

        #region names

        public static bool IsGenericName(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.GenericName;

        public static bool IsIdentifierName(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.IdentifierName;

        public static bool IsQualifiedName(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.QualifiedName;

        #endregion

        #region types

        public static bool IsTupleType(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.TupleType;

        #endregion

        #region literal expressions

        public static bool IsCharacterLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.CharacterLiteralExpression;

        public static bool IsDefaultLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.DefaultLiteralExpression;

        public static bool IsFalseLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.FalseLiteralExpression;

        public static bool IsNullLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.NullLiteralExpression;

        public static bool IsStringLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.StringLiteralExpression;

        public static bool IsTrueLiteralExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.TrueLiteralExpression;

        #endregion

        #region

        public static bool IsAwaitExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.AwaitExpression;

        public static bool IsImplicitObjectCreationExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => syntaxFacts.IsImplicitObjectCreation(node);

        public static bool IsBaseExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.BaseExpression;

        public static bool IsConditionalAccessExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.ConditionalAccessExpression;

        public static bool IsInvocationExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.InvocationExpression;

        public static bool IsLogicalAndExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.LogicalAndExpression;

        public static bool IsLogicalOrExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.LogicalOrExpression;

        public static bool IsLogicalNotExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.LogicalNotExpression;

        public static bool IsObjectCreationExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.ObjectCreationExpression;

        public static bool IsParenthesizedExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.ParenthesizedExpression;

        public static bool IsQueryExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.QueryExpression;

        public static bool IsSimpleMemberAccessExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.SimpleMemberAccessExpression;

        public static bool IsThisExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.ThisExpression;

        public static bool IsTupleExpression(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.TupleExpression;

        public static bool ContainsGlobalStatement(this ISyntaxFacts syntaxFacts, SyntaxNode node)
            => node.ChildNodes().Any(c => c.RawKind == syntaxFacts.SyntaxKinds.GlobalStatement);

        #endregion

        #region statements

        public static bool IsExpressionStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.ExpressionStatement;

        public static bool IsForEachStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.ForEachStatement;

        public static bool IsLocalDeclarationStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.LocalDeclarationStatement;

        public static bool IsLockStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.LockStatement;

        public static bool IsReturnStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.ReturnStatement;

        public static bool IsUsingStatement(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.UsingStatement;

        #endregion

        #region members/declarations

        public static bool IsAttribute(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.Attribute;

        public static bool IsGlobalAttribute(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => syntaxFacts.IsGlobalAssemblyAttribute(node) || syntaxFacts.IsGlobalModuleAttribute(node);

        public static bool IsParameter(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.Parameter;

        public static bool IsTypeConstraint(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.TypeConstraint;

        public static bool IsVariableDeclarator(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.VariableDeclarator;

        public static bool IsTypeArgumentList(this ISyntaxFacts syntaxFacts, [NotNullWhen(true)] SyntaxNode? node)
            => node?.RawKind == syntaxFacts.SyntaxKinds.TypeArgumentList;

        #endregion

        #endregion
    }
}
