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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{

    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

    /// <summary>
    /// Attempts to parse a stack frame line from given input. StackFrame is generally
    /// defined as a string line in a StackTrace. See https://docs.microsoft.com/en-us/dotnet/api/system.environment.stacktrace for 
    /// more documentation on dotnet stack traces. 
    /// </summary>
    internal struct StackFrameParser
    {
        private class StackFrameParseException : Exception
        {
            public StackFrameParseException(StackFrameKind expectedKind, StackFrameNodeOrToken actual)
                : this($"Expected {expectedKind} instead of {GetDetails(actual)}")
            {
            }

            private static string GetDetails(StackFrameNodeOrToken actual)
            {
                if (actual.IsNode)
                {
                    var node = actual.Node;
                    return $"'{node.Kind}' at {node.GetSpan().Start}";
                }
                else
                {
                    var token = actual.Token;
                    return $"'{token.VirtualChars.CreateString()}' at {token.GetSpan().Start}";
                }
            }

            public StackFrameParseException(string message)
                : base(message)
            {
            }
        }

        private StackFrameLexer _lexer;
        private StackFrameToken CurrentToken => _lexer.CurrentCharAsToken();

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
                return new StackFrameParser(text).TryParseTree();
            }
            catch (StackFrameParseException)
            {
                // Should we report why parsing failed here?
                return null;
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

        /// <summary>
        /// Attempts to parse the full tree. Returns null on malformed data
        /// </summary>
        private StackFrameTree? TryParseTree()
        {
            var methodDeclaration = TryParseMethodDeclaration();
            if (methodDeclaration is null)
            {
                return null;
            }

            var inTrivia = _lexer.ScanInTrivia();
            var fileInformationNodeOrToken = inTrivia.HasValue
                ? TryParseFileInformation()
                : null;

            using var _ = ArrayBuilder<StackFrameTrivia>.GetInstance(out var trailingTriviaBuilder);

            var fileInformation = fileInformationNodeOrToken.Node as StackFrameFileInformationNode;

            if (fileInformation is not null)
            {
                Debug.Assert(inTrivia.HasValue);
                fileInformation = fileInformation.WithLeadingTrivia(inTrivia.Value);
            }
            else if (inTrivia.HasValue)
            {
                // If the file path wasn't valid make sure to add the consumed tokens to the trailing trivia
                trailingTriviaBuilder.Add(StackFrameLexer.CreateTrivia(StackFrameKind.TextTrivia, inTrivia.Value.VirtualChars));

                var fileToken = fileInformationNodeOrToken.Token;
                if (!fileToken.LeadingTrivia.IsDefaultOrEmpty)
                {
                    trailingTriviaBuilder.AddRange(fileToken.LeadingTrivia);
                }

                trailingTriviaBuilder.Add(StackFrameLexer.CreateTrivia(StackFrameKind.TextTrivia, fileToken.VirtualChars));

                if (!fileToken.TrailingTrivia.IsDefaultOrEmpty)
                {
                    trailingTriviaBuilder.AddRange(fileToken.TrailingTrivia);
                }
            }

            var remainingTrivia = _lexer.ScanRemainingTrivia();
            if (remainingTrivia.HasValue)
            {
                trailingTriviaBuilder.Add(remainingTrivia.Value);
            }

            var eolToken = CurrentToken.With(leadingTrivia: trailingTriviaBuilder.ToImmutable());

            Debug.Assert(_lexer.Position == _lexer.Text.Length);
            Debug.Assert(eolToken.Kind == StackFrameKind.EndOfLine);

            var root = new StackFrameCompilationUnit(methodDeclaration, fileInformation, eolToken);

            return new StackFrameTree(
                _lexer.Text, root, ImmutableArray<EmbeddedDiagnostic>.Empty);
        }

        /// <summary>
        /// Attempts to parse the full method declaration, optionally adding leading whitespace as trivia. Includes
        /// all of the generic indicators for types, 
        /// 
        /// Ex: [|MyClass.MyMethod(string s)|]
        /// </summary>
        private StackFrameMethodDeclarationNode? TryParseMethodDeclaration()
        {
            var identifierExpression = TryParseIdentifierExpression(scanAtTrivia: true);
            if (!(identifierExpression.HasValue && identifierExpression.Value.IsNode))
            {
                return null;
            }

            if (identifierExpression.Value.Node is not StackFrameMemberAccessExpressionNode memberAccessExpression)
            {
                return null;
            }

            var typeArguments = TryParseTypeArguments();
            var arguments = TryParseMethodParameters();

            if (arguments is null)
            {
                return null;
            }

            return new StackFrameMethodDeclarationNode(memberAccessExpression, typeArguments, arguments);
        }

        /// <summary>
        /// Parses an identifier expression which could either be a <see cref="StackFrameBaseIdentifierNode"/> or <see cref="StackFrameMemberAccessExpressionNode" />. Combines
        /// identifiers that are separated by <see cref="StackFrameKind.DotToken"/> into <see cref="StackFrameMemberAccessExpressionNode" />.
        /// 
        /// Identifiers will be parsed for arity but not generic type arguments.
        ///
        /// All of the following are valid identifiers, where "$$" marks the parsing starting point, and "[|" + "|]" mark the endpoints of the parsed identifier including trivia
        ///   * [|$$MyNamespace.MyClass.MyMethod|](string s)
        ///   * MyClass.MyMethod([|$$string |]s)
        ///   * MyClass.MyMethod(string[| $$s|])
        ///   * [|$$MyClass`1.MyMethod|](string s)
        ///   * [|$$MyClass.MyMethod|][T](T t)
        /// </summary>
        private StackFrameNodeOrToken? TryParseIdentifierExpression(bool scanAtTrivia = false)
        {
            Queue<(StackFrameNodeOrToken identifier, StackFrameToken separator)> typeIdentifierNodes = new();

            var currentIdentifier = _lexer.ScanIdentifier(scanAtTrivia: scanAtTrivia, scanWhitespace: true);

            while (currentIdentifier.HasValue && currentIdentifier.Value.Kind == StackFrameKind.IdentifierToken)
            {
                StackFrameToken? arity = null;
                if (_lexer.ScanIfMatch(StackFrameKind.GraveAccentToken, out var graveAccentToken))
                {
                    arity = _lexer.ScanNumbers();
                    if (!arity.HasValue)
                    {
                        throw new StackFrameParseException(StackFrameKind.NumberToken, CurrentToken);
                    }
                }

                StackFrameNodeOrToken identifierNode = arity.HasValue
                    ? new StackFrameGenericTypeIdentifier(currentIdentifier.Value, graveAccentToken, arity.Value)
                    : currentIdentifier.Value;

                typeIdentifierNodes.Enqueue((identifierNode, CurrentToken));

                // Progress the lexer if the current token is a dot token, which 
                // was already added to the list. 
                if (!_lexer.ScanIfMatch(StackFrameKind.DotToken, out var _))
                {
                    break;
                }

                currentIdentifier = _lexer.ScanIdentifier();
            }

            if (typeIdentifierNodes.Count == 0)
            {
                return null;
            }

            var (firstIdentifierNode, firstSeparator) = typeIdentifierNodes.Dequeue();
            if (typeIdentifierNodes.Count == 0)
            {
                return firstIdentifierNode;
            }

            // Construct the member access expression from the identifiers in the list
            var currentSeparator = firstSeparator;

            StackFrameMemberAccessExpressionNode? memberAccessExpression = null;

            while (typeIdentifierNodes.Count != 0)
            {
                var previousSeparator = currentSeparator;
                (var currentIdentifierNode, currentSeparator) = typeIdentifierNodes.Dequeue();

                var leftHandNode = memberAccessExpression is null
                    ? firstIdentifierNode
                    : memberAccessExpression;

                memberAccessExpression = new StackFrameMemberAccessExpressionNode(leftHandNode, previousSeparator, currentIdentifierNode);
            }

            RoslynDebug.AssertNotNull(memberAccessExpression);

            return memberAccessExpression;
        }

        /// <summary>
        /// Type arguments for stacks are only valid on method declarations, and can have either '[' or '&lt;' as the 
        /// starting character depending on output source.
        /// 
        /// ex: MyNamespace.MyClass.MyMethod[T](T t)
        /// 
        /// Assumes the identifier "MyMethod" has already been parsed, and the type arguments will need to be parsed. 
        /// Returns null if no type arguments are found or if they are malformed.
        /// </summary>
        private StackFrameTypeArgumentList? TryParseTypeArguments()
        {
            if (!_lexer.ScanIfMatch(
                kind => kind is StackFrameKind.OpenBracketToken or StackFrameKind.LessThanToken,
                out var openToken))
            {
                return null;
            }

            var useCloseBracket = openToken.Kind is StackFrameKind.OpenBracketToken;

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            var currentIdentifier = _lexer.ScanIdentifier();
            StackFrameToken closeToken = default;

            while (currentIdentifier.HasValue && currentIdentifier.Value.Kind == StackFrameKind.IdentifierToken)
            {
                builder.Add(new StackFrameTypeArgumentNode(currentIdentifier.Value));

                if (_lexer.ScanIfMatch(StackFrameKind.CloseBracketToken, out var closeBracket))
                {
                    if (useCloseBracket)
                    {
                        closeToken = closeBracket;
                        break;
                    }

                    throw new StackFrameParseException(StackFrameKind.GreaterThanToken, closeBracket);
                }

                if (_lexer.ScanIfMatch(StackFrameKind.GreaterThanToken, out var greaterThanToken))
                {
                    if (useCloseBracket)
                    {
                        throw new StackFrameParseException(StackFrameKind.CloseBracketToken, greaterThanToken);
                    }

                    closeToken = greaterThanToken;
                    break;
                }

                builder.Add(CurrentToken);
                currentIdentifier = _lexer.ScanIdentifier();
            }

            if (closeToken.IsMissing)
            {
                return null;
            }

            return new StackFrameTypeArgumentList(openToken, new SeparatedStackFrameNodeList<StackFrameTypeArgumentNode>(builder.ToImmutable()), closeToken);
        }

        /// <summary>
        /// MyNamespace.MyClass.MyMethod[|(string s1, string s2, int i1)|]
        /// Takes parameter declarations from method text and parses them into a <see cref="StackFrameParameterList"/>. 
        /// 
        /// Returns null in cases where the input is malformed.
        /// </summary>
        private StackFrameParameterList? TryParseMethodParameters()
        {
            if (!_lexer.ScanIfMatch(StackFrameKind.OpenParenToken, scanTrailingWhitespace: true, out var openParen))
            {
                return null;
            }

            if (_lexer.ScanIfMatch(StackFrameKind.CloseParenToken, out var closeParen))
            {
                return new StackFrameParameterList(openParen, closeParen, SeparatedStackFrameNodeList<StackFrameParameterNode>.Empty);
            }

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            builder.Add(ParseParameterNode());
            while (_lexer.ScanIfMatch(StackFrameKind.CommaToken, out var commaToken))
            {
                builder.Add(commaToken);
                builder.Add(ParseParameterNode());
            }

            if (!_lexer.ScanIfMatch(StackFrameKind.CloseParenToken, out closeParen))
            {
                throw new StackFrameParseException(StackFrameKind.CloseParenToken, CurrentToken);
            }

            var parameters = new SeparatedStackFrameNodeList<StackFrameParameterNode>(builder.ToImmutable());
            return new StackFrameParameterList(openParen, closeParen, parameters);
        }

        /// <summary>
        /// Parses a <see cref="StackFrameParameterNode"/> by parsing identifiers first representing the type and then the parameter identifier.
        /// Ex: System.String[] s
        ///     ^--------------^ -- Type = "System.String[]"
        ///                     ^-- Identifier = "s"    
        /// </summary>
        private StackFrameParameterNode ParseParameterNode()
        {
            var typeIdentifier = TryParseIdentifierExpression();
            if (!typeIdentifier.HasValue)
            {
                throw new StackFrameParseException("Expected type identifier when parsing parameters");
            }

            if (CurrentToken.Kind == StackFrameKind.OpenBracketToken)
            {
                var arrayIdentifiers = ParseArrayIdentifiers();
                typeIdentifier = new StackFrameArrayTypeExpression(typeIdentifier.Value, arrayIdentifiers);
            }

            var identifier = TryParseIdentifierExpression();
            if (!identifier.HasValue)
            {
                throw new StackFrameParseException("Expected a parameter identifier");
            }

            // Parameter identifiers should only be tokens
            if (identifier.Value.IsNode)
            {
                throw new StackFrameParseException(StackFrameKind.IdentifierToken, identifier.Value);
            }

            return new StackFrameParameterNode(typeIdentifier.Value, identifier.Value.Token);
        }

        /// <summary>
        /// Parses the array rank specifiers for an identifier. 
        /// Ex: string[,][]
        ///           ^----^ both are array rank specifiers
        ///                  0: "[,]
        ///                  1: "[]"
        /// </summary>
        private ImmutableArray<StackFrameArrayRankSpecifier> ParseArrayIdentifiers()
        {
            using var _ = ArrayBuilder<StackFrameArrayRankSpecifier>.GetInstance(out var builder);
            using var _1 = ArrayBuilder<StackFrameToken>.GetInstance(out var commaBuilder);

            while (true)
            {
                if (!_lexer.ScanIfMatch(StackFrameKind.OpenBracketToken, scanTrailingWhitespace: true, out var openBracket))
                {
                    return builder.ToImmutable();
                }

                commaBuilder.Clear();
                while (_lexer.ScanIfMatch(StackFrameKind.CommaToken, scanTrailingWhitespace: true, out var commaToken))
                {
                    commaBuilder.Add(commaToken);
                }

                if (!_lexer.ScanIfMatch(StackFrameKind.CloseBracketToken, scanTrailingWhitespace: true, out var closeBracket))
                {
                    throw new StackFrameParseException(StackFrameKind.CloseBracketToken, CurrentToken);
                }

                builder.Add(new StackFrameArrayRankSpecifier(openBracket, closeBracket, commaBuilder.ToImmutable()));
            }

            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Parses text for a valid file path using valid file characters. It's very possible this includes a path that doesn't exist but
        /// forms a valid path identifier. 
        /// </summary>
        private StackFrameNodeOrToken TryParseFileInformation()
        {
            var path = _lexer.ScanPath();
            if (!path.HasValue)
            {
                return null;
            }

            if (!_lexer.ScanIfMatch(StackFrameKind.ColonToken, out var colonToken))
            {
                return path.Value;
            }

            var lineIdentifier = _lexer.ScanLineTrivia();
            if (!lineIdentifier.HasValue)
            {
                // malformed, we have a "<path>: " with no "line " trivia
                // add the colonToken as trivia to the valid path and return it
                var colonTrivia = StackFrameLexer.CreateTrivia(StackFrameKind.TextTrivia, colonToken.VirtualChars);
                return path.Value.With(trailingTrivia: ImmutableArray.Create(colonTrivia));
            }

            var numbers = _lexer.ScanNumbers();
            if (!numbers.HasValue)
            {
                // malformed, we have a "<path>:line " but no following number. 
                // Add the colon and line trivia as trailing trivia
                var jointTriviaSpan = new TextSpan(colonToken.GetSpan().Start, colonToken.VirtualChars.Length + lineIdentifier.Value.VirtualChars.Length);
                var trailingTrivia = StackFrameLexer.CreateTrivia(StackFrameKind.TextTrivia, _lexer.Text.GetSubSequence(jointTriviaSpan));
                return path.Value.With(trailingTrivia: ImmutableArray.Create(trailingTrivia));
            }

            return new StackFrameFileInformationNode(
                    path.Value,
                    colonToken,
                    numbers.Value.With(leadingTrivia: ImmutableArray.Create(lineIdentifier.Value)));
        }
    }
}
