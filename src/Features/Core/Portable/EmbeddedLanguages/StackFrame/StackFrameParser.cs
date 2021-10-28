// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

    /// <summary>
    /// Attempts to parse a stack frame line from given input. StackFrame is generally
    /// defined as a string line in a StackTrace. See https://docs.microsoft.com/en-us/dotnet/api/system.environment.stacktrace for 
    /// more documentation on dotnet stack traces. 
    /// </summary>
    internal partial struct StackFrameParser
    {
        private record struct ParseResult<T>(bool Success, T Value);

        private StackFrameParser(VirtualCharSequence text)
        {
            _lexer = new(text);
        }

        private StackFrameLexer _lexer;
        private StackFrameToken CurrentCharAsToken() => _lexer.CurrentCharAsToken();

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
            var (_, methodDeclaration) = TryParseMethodDeclaration();
            if (methodDeclaration is null)
            {
                return null;
            }

            var fileInformationResult = TryParseFileInformation();
            if (!fileInformationResult.Success)
            {
                return null;
            }

            var remainingTrivia = _lexer.TryScanRemainingTrivia();

            var eolToken = CurrentCharAsToken().With(leadingTrivia: remainingTrivia.HasValue ? ImmutableArray.Create(remainingTrivia.Value) : ImmutableArray<StackFrameTrivia>.Empty);

            Debug.Assert(_lexer.Position == _lexer.Text.Length);
            Debug.Assert(eolToken.Kind == StackFrameKind.EndOfLine);

            var root = new StackFrameCompilationUnit(methodDeclaration, fileInformationResult.Value, eolToken);

            return new(_lexer.Text, root);
        }

        /// <summary>
        /// Attempts to parse the full method declaration, optionally adding leading whitespace as trivia. Includes
        /// all of the generic indicators for types, 
        /// 
        /// Ex: [|MyClass.MyMethod(string s)|]
        /// </summary>
        private ParseResult<StackFrameMethodDeclarationNode?> TryParseMethodDeclaration()
        {
            var (success, identifierNode) = TryParseNameNode(scanAtTrivia: true);
            if (!success)
            {
                return new(false, null);
            }

            if (identifierNode is not StackFrameQualifiedNameNode memberAccessExpression)
            {
                return new(true, null);
            }

            var typeArgumentsResult = TryParseTypeArguments();
            if (!typeArgumentsResult.Success)
            {
                return new(false, null);
            }

            var argumentsResult = TryParseMethodParameters();

            if (!argumentsResult.Success || argumentsResult.Value is null)
            {
                return new(false, null);
            }

            return new(true, new(memberAccessExpression, typeArgumentsResult.Value, argumentsResult.Value));
        }

        /// <summary>
        /// Parses a <see cref="StackFrameNameNode"/> which could either be a <see cref="StackFrameSimpleNameNode"/> or <see cref="StackFrameQualifiedNameNode" />.
        /// 
        /// Nodes will be parsed for arity but not generic type arguments.
        ///
        /// All of the following are valid nodes, where "$$" marks the parsing starting point, and "[|" + "|]" mark the endpoints of the parsed node excluding trivia
        ///   * [|$$MyNamespace.MyClass.MyMethod|](string s)
        ///   * MyClass.MyMethod([|$$string|] s)
        ///   * MyClass.MyMethod([|$$string[]|] s)
        ///   * [|$$MyClass`1.MyMethod|](string s)
        ///   * [|$$MyClass.MyMethod|][T](T t)
        /// </summary>
        private ParseResult<StackFrameNameNode?> TryParseNameNode(bool scanAtTrivia)
        {
            var currentIdentifer = _lexer.TryScanIdentifier(scanAtTrivia: scanAtTrivia, scanLeadingWhitespace: true, scanTrailingWhitespace: false);
            if (!currentIdentifer.HasValue)
            {
                return new(true, null);
            }

            var identifierParseResult = TryScanGenericTypeIdentifier(currentIdentifer.Value);
            if (!identifierParseResult.Success)
            {
                return new(false, null);
            }

            StackFrameSimpleNameNode? lhs = identifierParseResult.Value;
            lhs ??= new StackFrameIdentifierNameNode(currentIdentifer.Value);

            var parseResult = TryScanQualifiedNameNode(lhs);
            if (!parseResult.Success)
            {
                return new(false, null);
            }

            var memberAccess = parseResult.Value;
            if (memberAccess is null)
            {
                return new(true, lhs);
            }

            while (true)
            {
                parseResult = TryScanQualifiedNameNode(memberAccess);
                if (!parseResult.Success)
                {
                    return new(false, null);
                }

                var newMemberAccess = parseResult.Value;
                if (newMemberAccess is null)
                {
                    return new(true, memberAccess);
                }

                memberAccess = newMemberAccess;
            }
        }

        /// <summary>
        /// Given an existing left hand side node or token, which can either be 
        /// an <see cref="StackFrameKind.IdentifierToken"/> or <see cref="StackFrameQualifiedNameNode"/>
        /// </summary>
        private ParseResult<StackFrameQualifiedNameNode?> TryScanQualifiedNameNode(StackFrameNameNode lhs)
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.DotToken, out var dotToken))
            {
                return new(true, null);
            }

            var identifier = _lexer.TryScanIdentifier();
            if (!identifier.HasValue)
            {
                return new(false, null);
            }

            (var success, StackFrameSimpleNameNode? rhs) = TryScanGenericTypeIdentifier(identifier.Value);
            if (!success)
            {
                return new(false, null);
            }

            rhs ??= new StackFrameIdentifierNameNode(identifier.Value);

            return new(true, new(lhs, dotToken, rhs));
        }

        /// <summary>
        /// Given an identifier, attempts to parse the type identifier arity for it.
        /// 
        /// <code>
        /// ex: MyNamespace.MyClass`1.MyMethod()
        ///                 ^--------------------- MyClass would be the identifier passed in
        ///                        ^-------------- Grave token
        ///                         ^------------- Arity token of "1" 
        /// </code>
        /// 
        /// </summary>
        private ParseResult<StackFrameGenericNameNode?> TryScanGenericTypeIdentifier(StackFrameToken identifierToken)
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.GraveAccentToken, out var graveAccentToken))
            {
                return new(true, null);
            }

            var arity = _lexer.TryScanNumbers();
            if (!arity.HasValue)
            {
                return new(false, null);
            }

            return new(true, new(identifierToken, graveAccentToken, arity.Value));
        }

        /// <summary>
        /// Type arguments for stacks are only valid on method declarations, and can have either '[' or '&lt;' as the 
        /// starting character depending on output source.
        /// 
        /// ex: MyNamespace.MyClass.MyMethod[T](T t)
        /// ex: MyNamespace.MyClass.MyMethod&lt;T&lt;(T t)
        /// 
        /// Assumes the identifier "MyMethod" has already been parsed, and the type arguments will need to be parsed.
        /// </summary>
        private ParseResult<StackFrameTypeArgumentList?> TryParseTypeArguments()
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(
                    kind => kind is StackFrameKind.OpenBracketToken or StackFrameKind.LessThanToken,
                    out var openToken))
            {
                return new(true, null);
            }

            var useCloseBracket = openToken.Kind is StackFrameKind.OpenBracketToken;

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            var currentIdentifier = _lexer.TryScanIdentifier(scanAtTrivia: false, scanLeadingWhitespace: true, scanTrailingWhitespace: true);
            StackFrameToken closeToken = default;

            while (currentIdentifier.HasValue && currentIdentifier.Value.Kind == StackFrameKind.IdentifierToken)
            {
                builder.Add(new StackFrameIdentifierNameNode(currentIdentifier.Value));

                if (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseBracketToken, out var closeBracket))
                {
                    if (useCloseBracket)
                    {
                        closeToken = closeBracket;
                        break;
                    }

                    return new(false, null);
                }

                if (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.GreaterThanToken, out var greaterThanToken))
                {
                    if (useCloseBracket)
                    {
                        return new(false, null);
                    }

                    closeToken = greaterThanToken;
                    break;
                }

                builder.Add(CurrentCharAsToken());
                currentIdentifier = _lexer.TryScanIdentifier();
            }

            if (builder.Count == 0)
            {
                return new(false, null);
            }

            if (closeToken.IsMissing)
            {
                return new(false, null);
            }

            var separatedList = new SeparatedStackFrameNodeList<StackFrameIdentifierNameNode>(builder.ToImmutable());
            return new(true, new(openToken, separatedList, closeToken));
        }

        /// <summary>
        /// MyNamespace.MyClass.MyMethod[|(string s1, string s2, int i1)|]
        /// Takes parameter declarations from method text and parses them into a <see cref="StackFrameParameterList"/>.
        /// </summary>
        private ParseResult<StackFrameParameterList?> TryParseMethodParameters()
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.OpenParenToken, scanTrailingWhitespace: true, out var openParen))
            {
                return new(true, null);
            }

            if (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseParenToken, out var closeParen))
            {
                return new(true, new(openParen, SeparatedStackFrameNodeList<StackFrameParameterNode>.Empty, closeParen));
            }

            var (success, parameterNode) = ParseParameterNode();
            if (!success || parameterNode is null)
            {
                return new(false, null);
            }

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            builder.Add(parameterNode);

            while (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CommaToken, out var commaToken))
            {
                builder.Add(commaToken);
                (success, parameterNode) = ParseParameterNode();
                if (!success || parameterNode is null)
                {
                    return new(false, null);
                }

                builder.Add(parameterNode);
            }

            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseParenToken, out closeParen))
            {
                return new(true, null);
            }

            var parameters = new SeparatedStackFrameNodeList<StackFrameParameterNode>(builder.ToImmutable());
            return new(true, new(openParen, parameters, closeParen));
        }

        /// <summary>
        /// Parses a <see cref="StackFrameParameterNode"/> by parsing identifiers first representing the type and then the parameter identifier.
        /// Ex: System.String[] s
        ///     ^--------------^ -- Type = "System.String[]"
        ///                     ^-- Identifier = "s"    
        /// </summary>
        private ParseResult<StackFrameParameterNode?> ParseParameterNode()
        {
            var (success, typeIdentifier) = TryParseNameNode(scanAtTrivia: false);
            if (!success || typeIdentifier is null)
            {
                return new(false, null);
            }

            if (CurrentCharAsToken().Kind == StackFrameKind.OpenBracketToken)
            {
                (success, var arrayIdentifiers) = ParseArrayRankSpecifiers();
                if (!success || arrayIdentifiers.IsDefault)
                {
                    return new(false, null);
                }

                typeIdentifier = new StackFrameArrayTypeExpression(typeIdentifier, arrayIdentifiers);
            }

            var identifier = _lexer.TryScanIdentifier(scanAtTrivia: false, scanLeadingWhitespace: true, scanTrailingWhitespace: true);
            if (!identifier.HasValue)
            {
                return new(false, null);
            }

            return new(true, new(typeIdentifier, identifier.Value));
        }

        /// <summary>
        /// Parses the array rank specifiers for an identifier. 
        /// Ex: string[,][]
        ///           ^----^ both are array rank specifiers
        ///                  0: "[,]
        ///                  1: "[]"
        /// </summary>
        private ParseResult<ImmutableArray<StackFrameArrayRankSpecifier>> ParseArrayRankSpecifiers()
        {
            using var _ = ArrayBuilder<StackFrameArrayRankSpecifier>.GetInstance(out var builder);
            using var _1 = ArrayBuilder<StackFrameToken>.GetInstance(out var commaBuilder);

            while (true)
            {
                if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.OpenBracketToken, scanTrailingWhitespace: true, out var openBracket))
                {
                    return new(true, builder.ToImmutable());
                }

                commaBuilder.Clear();
                while (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CommaToken, scanTrailingWhitespace: true, out var commaToken))
                {
                    commaBuilder.Add(commaToken);
                }

                if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseBracketToken, scanTrailingWhitespace: true, out var closeBracket))
                {
                    return new(false, default);
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
        private ParseResult<StackFrameFileInformationNode?> TryParseFileInformation()
        {
            var path = _lexer.TryScanPath();
            if (!path.HasValue)
            {
                return new(true, null);
            }

            if (path.Value.Kind != StackFrameKind.PathToken)
            {
                return new(false, null);
            }

            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.ColonToken, out var colonToken))
            {
                return new(true, new(path.Value, null, null));
            }

            var lineNumber = _lexer.TryScanLineNumber();

            // TryScanLineNumber can return a token that isn't a number, in which case we want 
            // to bail in error and consider this malformed.
            if (!lineNumber.HasValue || lineNumber.Value.Kind != StackFrameKind.NumberToken)
            {
                return new(false, null);
            }

            return new(true, new(path.Value, colonToken, lineNumber.Value));
        }
    }
}
