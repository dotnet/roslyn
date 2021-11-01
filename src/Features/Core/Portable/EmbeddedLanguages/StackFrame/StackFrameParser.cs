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
        private ParseResult<StackFrameMethodDeclarationNode> TryParseMethodDeclaration()
        {
            var (success, identifierNode) = TryParseNameNode(scanAtTrivia: true);
            if (!success)
            {
                return ParseResult<StackFrameMethodDeclarationNode>.Abort;
            }

            if (identifierNode is not StackFrameQualifiedNameNode memberAccessExpression)
            {
                return ParseResult<StackFrameMethodDeclarationNode>.Abort;
            }

            var typeArgumentsResult = TryParseTypeArguments();
            if (!typeArgumentsResult.Success)
            {
                return ParseResult<StackFrameMethodDeclarationNode>.Abort;
            }

            var argumentsResult = ParseMethodParameters();
            if (!argumentsResult.Success || argumentsResult.Value is null)
            {
                return ParseResult<StackFrameMethodDeclarationNode>.Abort;
            }

            return new(new(memberAccessExpression, typeArgumentsResult.Value, argumentsResult.Value));
        }

        /// <summary>
        /// Parses a <see cref="StackFrameNameNode"/> which could either be a <see cref="StackFrameSimpleNameNode"/> or <see cref="StackFrameQualifiedNameNode" />.
        /// 
        /// Nodes will be parsed for arity but not generic type arguments.
        ///
        /// <code>
        /// All of the following are valid nodes, where "$$" marks the parsing starting point, and "[|" + "|]" mark the endpoints of the parsed node excluding trivia
        ///   * [|$$MyNamespace.MyClass.MyMethod|](string s)
        ///   * MyClass.MyMethod([|$$string|] s)
        ///   * MyClass.MyMethod([|$$string[]|] s)
        ///   * [|$$MyClass`1.MyMethod|](string s)
        ///   * [|$$MyClass.MyMethod|][T](T t)
        /// </code>
        /// 
        /// </summary>
        private ParseResult<StackFrameNameNode> TryParseNameNode(bool scanAtTrivia)
        {
            var currentIdentifer = _lexer.TryScanIdentifier(scanAtTrivia: scanAtTrivia, scanLeadingWhitespace: true, scanTrailingWhitespace: false);
            if (!currentIdentifer.HasValue)
            {
                // If we can't parse an identifier, no need to abort the parser entirely here. 
                // The caller should determine if an identifier is required or not.
                return ParseResult<StackFrameNameNode>.Empty;
            }

            var identifierParseResult = TryScanGenericTypeIdentifier(currentIdentifer.Value);
            if (!identifierParseResult.Success)
            {
                return ParseResult<StackFrameNameNode>.Abort;
            }

            RoslynDebug.AssertNotNull(identifierParseResult.Value);
            var lhs = identifierParseResult.Value;

            var parseResult = TryParseQualifiedName(lhs);
            if (!parseResult.Success)
            {
                return ParseResult<StackFrameNameNode>.Abort;
            }

            var memberAccess = parseResult.Value;
            if (memberAccess is null)
            {
                Debug.Assert(lhs is StackFrameSimpleNameNode);
                return new(lhs);
            }

            while (true)
            {
                parseResult = TryParseQualifiedName(memberAccess);
                if (!parseResult.Success)
                {
                    return ParseResult<StackFrameNameNode>.Abort;
                }

                var newMemberAccess = parseResult.Value;
                if (newMemberAccess is null)
                {
                    Debug.Assert(memberAccess is StackFrameQualifiedNameNode);
                    return new(memberAccess);
                }

                memberAccess = newMemberAccess;
            }
        }

        /// <summary>
        /// Given an existing left hand side node or token, which can either be 
        /// an <see cref="StackFrameKind.IdentifierToken"/> or <see cref="StackFrameQualifiedNameNode"/>
        /// </summary>
        private ParseResult<StackFrameQualifiedNameNode> TryParseQualifiedName(StackFrameNameNode lhs)
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.DotToken, out var dotToken))
            {
                return ParseResult<StackFrameQualifiedNameNode>.Empty;
            }

            var identifier = _lexer.TryScanIdentifier();
            if (!identifier.HasValue)
            {
                return ParseResult<StackFrameQualifiedNameNode>.Abort;
            }

            var (success, rhs) = TryScanGenericTypeIdentifier(identifier.Value);
            if (!success)
            {
                return ParseResult<StackFrameQualifiedNameNode>.Abort;
            }

            RoslynDebug.AssertNotNull(rhs);
            return new(new(lhs, dotToken, rhs));
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
        private ParseResult<StackFrameSimpleNameNode> TryScanGenericTypeIdentifier(StackFrameToken identifierToken)
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.GraveAccentToken, out var graveAccentToken))
            {
                return new(new StackFrameIdentifierNameNode(identifierToken));
            }

            var arity = _lexer.TryScanNumbers();
            if (!arity.HasValue)
            {
                return ParseResult<StackFrameSimpleNameNode>.Abort;
            }

            return new(new StackFrameGenericNameNode(identifierToken, graveAccentToken, arity.Value));
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
        private ParseResult<StackFrameTypeArgumentList> TryParseTypeArguments()
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(
                    kind => kind is StackFrameKind.OpenBracketToken or StackFrameKind.LessThanToken,
                    out var openToken))
            {
                return ParseResult<StackFrameTypeArgumentList>.Empty;
            }

            var closeBrackeKind = openToken.Kind is StackFrameKind.OpenBracketToken
                ? StackFrameKind.CloseBracketToken
                : StackFrameKind.GreaterThanToken;

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            var currentIdentifier = _lexer.TryScanIdentifier(scanAtTrivia: false, scanLeadingWhitespace: true, scanTrailingWhitespace: true);
            StackFrameToken closeToken = default;

            while (currentIdentifier.HasValue && currentIdentifier.Value.Kind == StackFrameKind.IdentifierToken)
            {
                builder.Add(new StackFrameIdentifierNameNode(currentIdentifier.Value));

                if (_lexer.ScanCurrentCharAsTokenIfMatch(closeBrackeKind, out closeToken))
                {
                    break;
                }

                if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CommaToken, out var commaToken))
                {
                    return ParseResult<StackFrameTypeArgumentList>.Abort;
                }

                builder.Add(commaToken);
                currentIdentifier = _lexer.TryScanIdentifier();
            }

            if (builder.Count == 0)
            {
                return ParseResult<StackFrameTypeArgumentList>.Abort;
            }

            if (closeToken.IsMissing)
            {
                return ParseResult<StackFrameTypeArgumentList>.Abort;
            }

            var separatedList = new SeparatedStackFrameNodeList<StackFrameIdentifierNameNode>(builder.ToImmutable());
            return new(new(openToken, separatedList, closeToken));
        }

        /// <summary>
        /// MyNamespace.MyClass.MyMethod[|(string s1, string s2, int i1)|]
        /// Takes parameter declarations from method text and parses them into a <see cref="StackFrameParameterList"/>.
        /// </summary>
        private ParseResult<StackFrameParameterList> ParseMethodParameters()
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.OpenParenToken, scanTrailingWhitespace: true, out var openParen))
            {
                return ParseResult<StackFrameParameterList>.Abort;
            }

            if (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseParenToken, out var closeParen))
            {
                return new(new(openParen, SeparatedStackFrameNodeList<StackFrameParameterDeclarationNode>.Empty, closeParen));
            }

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);

            while (true)
            {
                var (success, parameterNode) = ParseParameterNode();
                if (!success)
                {
                    return ParseResult<StackFrameParameterList>.Abort;
                }

                RoslynDebug.AssertNotNull(parameterNode);
                builder.Add(parameterNode);

                if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CommaToken, out var commaToken))
                {
                    break;
                }

                builder.Add(commaToken);
            }

            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseParenToken, out closeParen))
            {
                return ParseResult<StackFrameParameterList>.Empty;
            }

            var parameters = new SeparatedStackFrameNodeList<StackFrameParameterDeclarationNode>(builder.ToImmutable());
            return new(new(openParen, parameters, closeParen));
        }

        /// <summary>
        /// Parses a <see cref="StackFrameParameterDeclarationNode"/> by parsing identifiers first representing the type and then the parameter identifier.
        /// Ex: System.String[] s
        ///     ^--------------^ -- Type = "System.String[]"
        ///                     ^-- Identifier = "s"    
        /// </summary>
        private ParseResult<StackFrameParameterDeclarationNode> ParseParameterNode()
        {
            var (success, typeIdentifier) = TryParseNameNode(scanAtTrivia: false);
            if (!success || typeIdentifier is null)
            {
                return ParseResult<StackFrameParameterDeclarationNode>.Abort;
            }

            if (CurrentCharAsToken().Kind == StackFrameKind.OpenBracketToken)
            {
                (success, var arrayIdentifiers) = ParseArrayRankSpecifiers();
                if (!success || arrayIdentifiers.IsDefault)
                {
                    return ParseResult<StackFrameParameterDeclarationNode>.Abort;
                }

                typeIdentifier = new StackFrameArrayTypeExpression(typeIdentifier, arrayIdentifiers);
            }

            var identifier = _lexer.TryScanIdentifier(scanAtTrivia: false, scanLeadingWhitespace: true, scanTrailingWhitespace: true);
            if (!identifier.HasValue)
            {
                return ParseResult<StackFrameParameterDeclarationNode>.Abort;
            }

            return new(new(typeIdentifier, identifier.Value));
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
                    return new(builder.ToImmutable());
                }

                while (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CommaToken, scanTrailingWhitespace: true, out var commaToken))
                {
                    commaBuilder.Add(commaToken);
                }

                if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseBracketToken, scanTrailingWhitespace: true, out var closeBracket))
                {
                    return ParseResult<ImmutableArray<StackFrameArrayRankSpecifier>>.Abort;
                }

                builder.Add(new StackFrameArrayRankSpecifier(openBracket, closeBracket, commaBuilder.ToImmutableAndClear()));
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
        private ParseResult<StackFrameFileInformationNode> TryParseFileInformation()
        {
            var path = _lexer.TryScanPath();
            if (!path.HasValue)
            {
                return ParseResult<StackFrameFileInformationNode>.Empty;
            }

            if (path.Value.Kind != StackFrameKind.PathToken)
            {
                return ParseResult<StackFrameFileInformationNode>.Abort;
            }

            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.ColonToken, out var colonToken))
            {
                return new(new(path.Value, null, null));
            }

            var lineNumber = _lexer.TryScanLineNumber();

            // TryScanLineNumber can return a token that isn't a number, in which case we want 
            // to bail in error and consider this malformed.
            if (!lineNumber.HasValue || lineNumber.Value.Kind != StackFrameKind.NumberToken)
            {
                return ParseResult<StackFrameFileInformationNode>.Abort;
            }

            return new(new(path.Value, colonToken, lineNumber.Value));
        }
    }
}
