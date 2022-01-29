// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages
{
    internal abstract class AbstractLanguageDetector<TOptions, TTree>
        where TOptions : struct, Enum
        where TTree : class
    {
        protected readonly EmbeddedLanguageInfo Info;
        private readonly LanguageCommentDetector<TOptions> _commentDetector;

        protected AbstractLanguageDetector(
            EmbeddedLanguageInfo info,
            LanguageCommentDetector<TOptions> commentDetector)
        {
            Info = info;
            _commentDetector = commentDetector;
        }

        protected abstract bool IsEmbeddedLanguageString(SyntaxToken token, SyntaxNode argumentNode, SemanticModel semanticModel, CancellationToken cancellationToken, out TOptions options);
        protected abstract TTree? TryParse(VirtualCharSequence chars, TOptions options);
        protected abstract bool TryGetOptions(SemanticModel semanticModel, ITypeSymbol exprType, SyntaxNode expr, CancellationToken cancellationToken, out TOptions options);

        public bool IsPossiblyPatternToken(SyntaxToken token, ISyntaxFacts syntaxFacts)
        {
            if (!syntaxFacts.IsStringLiteral(token))
                return false;

            if (syntaxFacts.IsLiteralExpression(token.Parent) && syntaxFacts.IsArgument(token.Parent.Parent))
                return true;

            return HasLanguageComment(token, syntaxFacts, out _);
        }

        private bool HasLanguageComment(
            SyntaxToken token, ISyntaxFacts syntaxFacts, out TOptions options)
        {
            if (HasLanguageComment(token.GetPreviousToken().TrailingTrivia, syntaxFacts, out options))
                return true;

            for (var node = token.Parent; node != null; node = node.Parent)
            {
                if (HasLanguageComment(node.GetLeadingTrivia(), syntaxFacts, out options))
                    return true;

                // Stop walking up once we hit a statement.  We don't need/want statements higher up the parent chain to
                // have any impact on this token.
                if (syntaxFacts.IsStatement(node))
                    break;
            }

            options = default;
            return false;
        }

        private bool HasLanguageComment(
            SyntaxTriviaList list, ISyntaxFacts syntaxFacts, out TOptions options)
        {
            foreach (var trivia in list)
            {
                if (HasLanguageComment(trivia, syntaxFacts, out options))
                    return true;
            }

            options = default;
            return false;
        }

        private bool HasLanguageComment(
            SyntaxTrivia trivia, ISyntaxFacts syntaxFacts, out TOptions options)
        {
            if (syntaxFacts.IsRegularComment(trivia))
            {
                // Note: ToString on SyntaxTrivia is non-allocating.  It will just return the
                // underlying text that the trivia is already pointing to.
                var text = trivia.ToString();
                if (_commentDetector.TryMatch(text, out options))
                    return true;
            }

            options = default;
            return false;
        }

        public bool IsEmbeddedLanguageString(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken, out TOptions options)
        {
            options = default;
            if (!IsPossiblyPatternToken(token, Info.SyntaxFacts))
                return false;

            var syntaxFacts = Info.SyntaxFacts;
            if (HasLanguageComment(token, syntaxFacts, out options))
                return true;

            var stringLiteral = token;
            var literalNode = stringLiteral.GetRequiredParent();
            var argumentNode = literalNode.Parent;
            Debug.Assert(syntaxFacts.IsArgument(argumentNode));

            return IsEmbeddedLanguageString(token, argumentNode, semanticModel, cancellationToken, out options);
        }

        public TTree? TryParseString(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (!this.IsEmbeddedLanguageString(token, semanticModel, cancellationToken, out var options))
                return null;

            var chars = Info.VirtualCharService.TryConvertToVirtualChars(token);
            return TryParse(chars, options);
        }

        protected TOptions GetOptionsFromSiblingArgument(SyntaxNode argumentNode, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var syntaxFacts = Info.SyntaxFacts;
            var argumentList = argumentNode.GetRequiredParent();
            var arguments = syntaxFacts.GetArgumentsOfArgumentList(argumentList);
            foreach (var siblingArg in arguments)
            {
                if (siblingArg != argumentNode)
                {
                    var expr = syntaxFacts.GetExpressionOfArgument(siblingArg);
                    if (expr != null)
                    {
                        var exprType = semanticModel.GetTypeInfo(expr, cancellationToken);
                        if (exprType.Type != null &&
                            TryGetOptions(semanticModel, exprType.Type, expr, cancellationToken, out var options))
                        {
                            return options;
                        }
                    }
                }
            }

            return default;
        }

        protected string? GetNameOfType(SyntaxNode? typeNode, ISyntaxFacts syntaxFacts)
        {
            if (syntaxFacts.IsQualifiedName(typeNode))
            {
                return GetNameOfType(syntaxFacts.GetRightSideOfDot(typeNode), syntaxFacts);
            }
            else if (syntaxFacts.IsIdentifierName(typeNode))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(typeNode).ValueText;
            }

            return null;
        }

        protected string? GetNameOfInvokedExpression(SyntaxNode invokedExpression)
        {
            var syntaxFacts = Info.SyntaxFacts;
            if (syntaxFacts.IsSimpleMemberAccessExpression(invokedExpression))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(syntaxFacts.GetNameOfMemberAccessExpression(invokedExpression)).ValueText;
            }
            else if (syntaxFacts.IsIdentifierName(invokedExpression))
            {
                return syntaxFacts.GetIdentifierOfSimpleName(invokedExpression).ValueText;
            }

            return null;
        }
    }
}
