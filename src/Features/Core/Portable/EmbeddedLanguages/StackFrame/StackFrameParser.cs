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
        private StackFrameToken CurrentCharAsToken() => _lexer.CurrentCharAsToken();

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

            var fileInformation = TryParseFileInformation();
            var remainingTrivia = _lexer.TryScanRemainingTrivia();

            var eolToken = CurrentCharAsToken().With(leadingTrivia: remainingTrivia.HasValue ? ImmutableArray.Create(remainingTrivia.Value) : ImmutableArray<StackFrameTrivia>.Empty);

            Debug.Assert(_lexer.Position == _lexer.Text.Length);
            Debug.Assert(eolToken.Kind == StackFrameKind.EndOfLine);

            var root = new StackFrameCompilationUnit(methodDeclaration, fileInformation, eolToken);

            return new(_lexer.Text, root);
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
        ///   * [|$$MyClass`1.MyMethod|](string s)
        ///   * [|$$MyClass.MyMethod|][T](T t)
        /// </summary>
        private StackFrameNodeOrToken? TryParseIdentifierExpression(bool scanAtTrivia = false)
        {
            var currentIdentifer = _lexer.TryScanIdentifier(scanAtTrivia: scanAtTrivia, scanWhitespace: true);
            if (!currentIdentifer.HasValue)
            {
                return null;
            }

            var lhs = TryScanGenericTypeIdentifier(currentIdentifer.Value)
                ?? (StackFrameNodeOrToken)currentIdentifer.Value;

            var memberAccess = TryScanMemberAccessExpression(lhs);
            if (memberAccess is null)
            {
                return lhs;
            }

            while (true)
            {
                var newMemberAccess = TryScanMemberAccessExpression(memberAccess);
                if (newMemberAccess is null)
                {
                    return memberAccess;
                }

                memberAccess = newMemberAccess;
            }
        }

        /// <summary>
        /// Given an existing left hand side node or token, which can either be 
        /// an <see cref="StackFrameKind.IdentifierToken"/> or <see cref="StackFrameMemberAccessExpressionNode"/>
        /// </summary>
        private StackFrameMemberAccessExpressionNode? TryScanMemberAccessExpression(StackFrameNodeOrToken lhs)
        {
            Debug.Assert((lhs.IsNode && lhs.Node is StackFrameMemberAccessExpressionNode) ||
                         lhs.Token.Kind == StackFrameKind.IdentifierToken);

            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.DotToken, out var dotToken))
            {
                return null;
            }

            var identifier = _lexer.TryScanIdentifier();
            if (!identifier.HasValue)
            {
                throw new StackFrameParseException(StackFrameKind.IdentifierToken, CurrentCharAsToken());
            }

            var rhs = TryScanGenericTypeIdentifier(identifier.Value)
                ?? (StackFrameNodeOrToken)identifier.Value;

            return new StackFrameMemberAccessExpressionNode(lhs, dotToken, rhs);
        }

        /// <summary>
        /// Given an identifier, attempts to parse the type identifier arity for it.
        /// ex: MyNamespace.MyClass`1.MyMethod()
        ///                 ^--------------------- MyClass would be the identifier passed in
        ///                        ^-------------- Grave token
        ///                         ^------------- Arity token of "1" 
        /// </summary>
        private StackFrameGenericTypeIdentifier? TryScanGenericTypeIdentifier(StackFrameToken identifierToken)
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.GraveAccentToken, out var graveAccentToken))
            {
                return null;
            }

            var arity = _lexer.TryScanNumbers();
            if (!arity.HasValue)
            {
                throw new StackFrameParseException(StackFrameKind.NumberToken, CurrentCharAsToken());
            }

            return new StackFrameGenericTypeIdentifier(identifierToken, graveAccentToken, arity.Value);
        }

        /// <summary>
        /// Type arguments for stacks are only valid on method declarations, and can have either '[' or '&lt;' as the 
        /// starting character depending on output source.
        /// 
        /// ex: MyNamespace.MyClass.MyMethod[T](T t)
        /// ex: MyNamespace.MyClass.MyMethod&lt;T&lt;(T t)
        /// 
        /// Assumes the identifier "MyMethod" has already been parsed, and the type arguments will need to be parsed. 
        /// Returns null if no type arguments are found, and throw a <see cref="StackFrameParseException"/> if they are malformed
        /// </summary>
        private StackFrameTypeArgumentList? TryParseTypeArguments()
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(
                    kind => kind is StackFrameKind.OpenBracketToken or StackFrameKind.LessThanToken,
                    out var openToken))
            {
                return null;
            }

            var useCloseBracket = openToken.Kind is StackFrameKind.OpenBracketToken;

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            var currentIdentifier = _lexer.TryScanIdentifier();
            StackFrameToken closeToken = default;

            while (currentIdentifier.HasValue && currentIdentifier.Value.Kind == StackFrameKind.IdentifierToken)
            {
                builder.Add(new StackFrameTypeArgumentNode(currentIdentifier.Value));

                if (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseBracketToken, out var closeBracket))
                {
                    if (useCloseBracket)
                    {
                        closeToken = closeBracket;
                        break;
                    }

                    throw new StackFrameParseException(StackFrameKind.GreaterThanToken, closeBracket);
                }

                if (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.GreaterThanToken, out var greaterThanToken))
                {
                    if (useCloseBracket)
                    {
                        throw new StackFrameParseException(StackFrameKind.CloseBracketToken, greaterThanToken);
                    }

                    closeToken = greaterThanToken;
                    break;
                }

                builder.Add(CurrentCharAsToken());
                currentIdentifier = _lexer.TryScanIdentifier();
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
        /// Returns null in cases where the opening paren is not found. Throws <see cref="StackFrameParseException"/> if the 
        /// opening paren exists but the remaining parameter definitions are malformed.
        /// </summary>
        private StackFrameParameterList? TryParseMethodParameters()
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.OpenParenToken, scanTrailingWhitespace: true, out var openParen))
            {
                return null;
            }

            if (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseParenToken, out var closeParen))
            {
                return new StackFrameParameterList(openParen, closeParen, SeparatedStackFrameNodeList<StackFrameParameterNode>.Empty);
            }

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            builder.Add(ParseParameterNode());
            while (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CommaToken, out var commaToken))
            {
                builder.Add(commaToken);
                builder.Add(ParseParameterNode());
            }

            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseParenToken, out closeParen))
            {
                throw new StackFrameParseException(StackFrameKind.CloseParenToken, CurrentCharAsToken());
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

            if (CurrentCharAsToken().Kind == StackFrameKind.OpenBracketToken)
            {
                var arrayIdentifiers = ParseArrayIdentifiers();
                typeIdentifier = new StackFrameArrayTypeExpression(typeIdentifier.Value, arrayIdentifiers);
            }

            var identifier = _lexer.TryScanIdentifier(scanWhitespace: true);
            if (!identifier.HasValue)
            {
                throw new StackFrameParseException("Expected a parameter identifier");
            }

            return new StackFrameParameterNode(typeIdentifier.Value, identifier.Value);
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
                if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.OpenBracketToken, scanTrailingWhitespace: true, out var openBracket))
                {
                    return builder.ToImmutable();
                }

                commaBuilder.Clear();
                while (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CommaToken, scanTrailingWhitespace: true, out var commaToken))
                {
                    commaBuilder.Add(commaToken);
                }

                if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseBracketToken, scanTrailingWhitespace: true, out var closeBracket))
                {
                    throw new StackFrameParseException(StackFrameKind.CloseBracketToken, CurrentCharAsToken());
                }

                builder.Add(new StackFrameArrayRankSpecifier(openBracket, closeBracket, commaBuilder.ToImmutable()));
            }

            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Parses text for a valid file path using valid file characters. It's very possible this includes a path that doesn't exist but
        /// forms a valid path identifier. 
        /// 
        /// Can return if only a path is available but not line numbers, but throws if the value after the path is a colon as the expectation
        /// is that line number should follow.
        /// </summary>
        private StackFrameFileInformationNode? TryParseFileInformation()
        {
            var path = _lexer.TryScanPath();
            if (!path.HasValue)
            {
                return null;
            }

            if (path.Value.Kind != StackFrameKind.PathToken)
            {
                throw new StackFrameParseException("'In ' trivia was present ");
            }

            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.ColonToken, out var colonToken))
            {
                return new(path.Value, null, null);
            }

            var lineNumber = _lexer.TryScanLineNumber();

            // TryScanLineNumber can return a token that isn't a number, in which case we want 
            // to bail in error and consider this malformed.
            if (!lineNumber.HasValue || lineNumber.Value.Kind != StackFrameKind.NumberToken)
            {
                throw new StackFrameParseException("Expected line number to exist after colon token");
            }

            return new(path.Value, colonToken, lineNumber.Value);
        }
    }
}
