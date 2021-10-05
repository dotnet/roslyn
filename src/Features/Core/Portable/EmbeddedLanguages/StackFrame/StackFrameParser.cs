// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

    internal struct StackFrameParser
    {
        private StackFrameLexer _lexer;
        private StackFrameToken CurrentToken => _lexer.PreviousCharAsToken();

        private StackFrameParser(VirtualCharSequence text)
        {
            _lexer = new(text);
        }

        /// <summary>
        /// Given an input text, and set of options, parses out a fully representative syntax tree 
        /// and list of diagnostics.  Parsing should always succeed, except in the case of the stack 
        /// overflowing.
        /// </summary>
        public static StackFrameTree? TryParse(VirtualCharSequence text)
        {
            if (text.IsDefault)
            {
                return null;
            }

            try
            {
                return new StackFrameParser(text).ParseTree();
            }
            catch (InsufficientExecutionStackException)
            {
                return null;
            }
        }

        /// <summary>
        /// Constructs a <see cref="VirtualCharSequence"/> and calls <see cref="TryParse(VirtualCharSequence)"/>
        /// </summary>
        public static StackFrameTree? TryParse(string text)
            => TryParse(VirtualCharSequence.Create(0, text));

        private StackFrameTree? ParseTree()
        {
            var atTrivia = _lexer.ScanAtTrivia();

            var methodDeclaration = ParseMethodDeclaration(addLeadingWhitespaceAsTrivia: !atTrivia.HasValue);
            if (methodDeclaration is null)
            {
                return null;
            }

            var inTrivia = _lexer.ScanInTrivia();
            var fileInformationExpression = inTrivia.HasValue
                ? ParseFileInformation()
                : null;
            var trailingTrivia = _lexer.ScanTrailingTrivia();

            Debug.Assert(_lexer.Position == _lexer.Text.Length);
            Debug.Assert(_lexer.CurrentCharAsToken().Kind == StackFrameKind.EndOfLine);

            var root = new StackFrameCompilationUnit(atTrivia, methodDeclaration, inTrivia, fileInformationExpression, trailingTrivia);

            return new StackFrameTree(
                _lexer.Text, root, ImmutableArray<EmbeddedDiagnostic>.Empty);
        }

        private StackFrameMethodDeclarationNode? ParseMethodDeclaration(bool addLeadingWhitespaceAsTrivia)
        {
            var identifierExpression = ParseIdentifierExpression(addLeadingWhitespaceAsTrivia: addLeadingWhitespaceAsTrivia);
            if (identifierExpression is not StackFrameMemberAccessExpressionNode memberAccessExpression)
            {
                return null;
            }

            var typeArguments = ParseTypeArguments();
            var arguments = ParseMethodArguments();

            if (arguments is null)
            {
                return null;
            }

            return new StackFrameMethodDeclarationNode(memberAccessExpression, typeArguments, arguments);
        }

        /// <summary>
        /// Parses an identifier expression which could either be a <see cref="StackFrameBaseIdentifierNode"/> or <see cref="StackFrameMemberAccessExpressionNode"/>
        /// </summary>
        private StackFrameExpressionNode? ParseIdentifierExpression(bool addLeadingWhitespaceAsTrivia)
        {
            Queue<(StackFrameBaseIdentifierNode identifier, StackFrameToken separator)> typeIdentifierNodes = new();

            // If allowed, add the leading whitespace as trivia to the first
            // identifier token in the expression
            var leadingTrivia = AllowWhitespace(CurrentToken)
                    ? _lexer.ScanWhiteSpace(includePrevious: addLeadingWhitespaceAsTrivia && CurrentToken.Kind == StackFrameKind.WhitespaceToken)
                    : null;

            var currentIdentifier = _lexer.ScanIdentifier();
            while (currentIdentifier.HasValue && currentIdentifier.Value.Kind == StackFrameKind.IdentifierToken)
            {
                StackFrameToken? arity = null;
                if (CurrentToken.Kind == StackFrameKind.GraveAccentToken)
                {
                    arity = _lexer.ScanTypeArity();
                }

                if (leadingTrivia.HasValue)
                {
                    currentIdentifier = currentIdentifier.Value.With(leadingTrivia: ImmutableArray.Create(leadingTrivia.Value));
                    leadingTrivia = null;
                }

                StackFrameBaseIdentifierNode identifierNode = arity.HasValue
                    ? new StackFrameGenericTypeIdentifier(currentIdentifier.Value, CurrentToken, arity.Value)
                    : new StackFrameIdentifierNode(currentIdentifier.Value);

                typeIdentifierNodes.Enqueue((identifierNode, CurrentToken));

                if (CurrentToken.Kind != StackFrameKind.DotToken)
                {
                    break;
                }

                currentIdentifier = _lexer.ScanIdentifier();
            }

            if (typeIdentifierNodes.Count == 0)
            {
                return null;
            }

            var trailingTrivia = CurrentToken.Kind == StackFrameKind.WhitespaceToken
                ? _lexer.ScanWhiteSpace(includePrevious: true)
                : null;

            if (typeIdentifierNodes.Count == 1)
            {
                var identifierNode = typeIdentifierNodes.Dequeue().identifier;
                return trailingTrivia.HasValue
                    ? identifierNode.WithTrailingTrivia(trailingTrivia.Value)
                    : identifierNode;
            }

            // Construct the member access expression from the identifiers
            var (firstIdentifierNode, firstSeparator) = typeIdentifierNodes.Dequeue();
            var currentSeparator = firstSeparator;

            StackFrameMemberAccessExpressionNode? memberAccessExpression = null;

            while (typeIdentifierNodes.Count != 0)
            {
                var previousSeparator = currentSeparator;
                (var currentIdentifierNode, currentSeparator) = typeIdentifierNodes.Dequeue();

                StackFrameExpressionNode leftHandNode = memberAccessExpression is null
                    ? firstIdentifierNode
                    : memberAccessExpression;

                memberAccessExpression = new StackFrameMemberAccessExpressionNode(leftHandNode, previousSeparator, currentIdentifierNode);
            }

            RoslynDebug.AssertNotNull(memberAccessExpression);

            return trailingTrivia.HasValue
                    ? memberAccessExpression.WithTrailingTrivia(trailingTrivia.Value)
                    : memberAccessExpression;

            static bool AllowWhitespace(StackFrameToken token)
                => token.Kind
                    is StackFrameKind.WhitespaceTrivia
                    or StackFrameKind.CommaToken
                    or StackFrameKind.OpenBracketToken
                    or StackFrameKind.CloseBracketToken
                    or StackFrameKind.OpenParenToken
                    or StackFrameKind.CloseParenToken
                    or StackFrameKind.WhitespaceToken;
        }

        private StackFrameTypeArgumentList? ParseTypeArguments()
        {
            if (CurrentToken.Kind is not StackFrameKind.OpenBracketToken or StackFrameKind.LessThanToken)
            {
                return null;
            }

            var useCloseBracket = CurrentToken.Kind is StackFrameKind.OpenBracketToken;

            Func<StackFrameToken, bool> stopParsing = (token) => useCloseBracket ? token.Kind == StackFrameKind.CloseBracketToken : token.Kind == StackFrameKind.GreaterThanToken;

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            builder.Add(CurrentToken);

            var currentIdentifier = _lexer.ScanIdentifier();

            while (currentIdentifier.HasValue && currentIdentifier.Value.Kind == StackFrameKind.IdentifierToken)
            {
                builder.Add(currentIdentifier.Value);
                builder.Add(CurrentToken);

                if (stopParsing(CurrentToken))
                {
                    break;
                }

                currentIdentifier = _lexer.ScanIdentifier();
            }

            return new StackFrameTypeArgumentList(builder.ToImmutable());
        }

        private StackFrameArgumentList? ParseMethodArguments()
        {
            if (CurrentToken.Kind != StackFrameKind.OpenParenToken)
            {
                return null;
            }

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            builder.Add(CurrentToken);

            var identifier = ParseIdentifierExpression(addLeadingWhitespaceAsTrivia: true);
            while (identifier is not null)
            {
                // Check if there's an array type for the identifier
                if (CurrentToken.Kind == StackFrameKind.OpenBracketToken)
                {
                    builder.Add(ParseArrayIdentifier(identifier));
                }
                else
                {
                    builder.Add(identifier);
                }

                // Let whitespace get added as trivia to the identifiers
                // for the parameters
                if (CurrentToken.Kind != StackFrameKind.WhitespaceToken)
                {
                    builder.Add(CurrentToken);
                }

                if (CurrentToken.Kind == StackFrameKind.CloseParenToken)
                {
                    return new StackFrameArgumentList(builder.ToImmutable());
                }

                var addLeadingWhitespaceAsTrivia = identifier.ChildAt(identifier.ChildCount - 1).Token.TrailingTrivia.IsDefaultOrEmpty;
                identifier = ParseIdentifierExpression(addLeadingWhitespaceAsTrivia: addLeadingWhitespaceAsTrivia);
            }

            if (CurrentToken.Kind != StackFrameKind.CloseParenToken)
            {
                // If we parsed identifiers but never got to a closing portion for the argument list
                // we need to bail and return null so we know that this isn't valid
                return null;
            }

            return new StackFrameArgumentList(builder.ToImmutable());
        }

        private StackFrameExpressionNode ParseArrayIdentifier(StackFrameExpressionNode identifier)
        {
            if (CurrentToken.Kind != StackFrameKind.OpenBracketToken)
            {
                throw new InvalidOperationException();
            }

            var arrayBrackets = _lexer.ScanArrayBrackets();
            return new StackFrameArrayExpressionNode(identifier, arrayBrackets);
        }

        public StackFrameFileInformationNode ParseFileInformation()
        {
            throw new NotImplementedException();
        }
    }
}
