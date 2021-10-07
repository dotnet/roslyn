// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

    internal struct StackFrameParser
    {
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

        /// <summary>
        /// Attempts to parse the full tree. Returns null on malformed data
        /// </summary>
        private StackFrameTree? ParseTree()
        {
            var atTrivia = _lexer.ScanAtTrivia();

            var methodDeclaration = ParseMethodDeclaration();
            if (methodDeclaration is null)
            {
                return null;
            }

            if (atTrivia.HasValue)
            {
                var currentTrivia = methodDeclaration.ChildAt(0).Token.LeadingTrivia;
                var newList = currentTrivia.IsDefaultOrEmpty
                    ? ImmutableArray.Create(atTrivia.Value)
                    : currentTrivia.Prepend(atTrivia.Value).ToImmutableArray();

                methodDeclaration = methodDeclaration.WithLeadingTrivia(newList);
            }

            var inTrivia = _lexer.ScanInTrivia();
            var (fileInformation, isFilePathValid) = inTrivia.HasValue
                ? ParseFileInformation()
                : (null, false);

            using var _ = ArrayBuilder<StackFrameTrivia>.GetInstance(out var trailingTriviaBuilder);

            if (inTrivia.HasValue)
            {
                if (isFilePathValid)
                {
                    // If the path is valid, just add the inTrivia to the file information
                    RoslynDebug.AssertNotNull(fileInformation);
                    fileInformation = fileInformation.WithLeadingTrivia(inTrivia.Value);
                }
                else
                {
                    // If we parsed a path but it's not valid for the file system,
                    // combine both with the remaining trivia as text
                    trailingTriviaBuilder.Add(inTrivia.Value);

                    if (fileInformation is not null)
                    {
                        // If the path isn't valid we don't expect the line or colon trivia
                        // to exist on the expression
                        Debug.Assert(!fileInformation.Line.HasValue);
                        Debug.Assert(!fileInformation.Colon.HasValue);

                        trailingTriviaBuilder.Add(StackFrameLexer.CreateTrivia(StackFrameKind.TextTrivia, fileInformation.Path.VirtualChars));
                    }
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
        private StackFrameMethodDeclarationNode? ParseMethodDeclaration()
        {
            var identifierExpression = ParseIdentifierExpression();
            if (identifierExpression is not StackFrameMemberAccessExpressionNode memberAccessExpression)
            {
                return null;
            }

            var typeArguments = ParseTypeArguments();
            var arguments = ParseMethodParameters();

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
        private StackFrameExpressionNode? ParseIdentifierExpression()
        {
            Queue<(StackFrameBaseIdentifierNode identifier, StackFrameToken separator)> typeIdentifierNodes = new();

            var leadingTrivia = _lexer.ScanWhiteSpace();
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

            var trailingTrivia = _lexer.ScanWhiteSpace();

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
        private StackFrameTypeArgumentList? ParseTypeArguments()
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
                builder.Add(new StackFrameTypeArgument(currentIdentifier.Value));

                if (useCloseBracket && _lexer.ScanIfMatch(StackFrameKind.CloseBracketToken, out closeToken))
                {
                    break;
                }
                else if (_lexer.ScanIfMatch(StackFrameKind.GreaterThanToken, out closeToken))
                {
                    break;
                }

                builder.Add(CurrentToken);
                currentIdentifier = _lexer.ScanIdentifier();
            }

            if (closeToken.IsMissing)
            {
                return null;
            }

            return new StackFrameTypeArgumentList(openToken, builder.ToImmutable(), closeToken);
        }

        /// <summary>
        /// MyNamespace.MyClass.MyMethod[|(string s1, string s2, int i1)|]
        /// Takes parameter declarations from method text and parses them into a <see cref="StackFrameParameterList"/>. 
        /// 
        /// Returns null in cases where the input is malformed.
        /// </summary>
        private StackFrameParameterList? ParseMethodParameters()
        {
            if (!_lexer.ScanIfMatch(StackFrameKind.OpenParenToken, out var openParen))
            {
                return null;
            }

            var spaceTrivia = _lexer.ScanWhiteSpace();
            if (spaceTrivia.HasValue)
            {
                openParen = openParen.With(trailingTrivia: ImmutableArray.Create(spaceTrivia.Value));
            }

            StackFrameToken closeParen;

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            var identifier = ParseIdentifierExpression();

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

                if (_lexer.ScanIfMatch(StackFrameKind.CloseParenToken, out closeParen))
                {
                    return new StackFrameParameterList(openParen, builder.ToImmutable(), closeParen);
                }

                if (_lexer.ScanIfMatch(StackFrameKind.CommaToken, out var commaToken))
                {
                    builder.Add(commaToken);
                }

                var addLeadingWhitespaceAsTrivia = identifier.ChildAt(identifier.ChildCount - 1).Token.TrailingTrivia.IsDefaultOrEmpty;
                identifier = ParseIdentifierExpression();
            }

            if (_lexer.ScanIfMatch(StackFrameKind.CloseParenToken, out closeParen))
            {
                return new StackFrameParameterList(openParen, builder.ToImmutable(), closeParen);
            }

            return null;
        }

        /// <summary>
        /// Given an input like "string[]" where "string" is the existing <paramref name="identifier"/>
        /// passed in, converts it into <see cref="StackFrameExpressionNode"/> by parsing the array portion
        /// of the identifier.
        /// </summary>
        private StackFrameExpressionNode ParseArrayIdentifier(StackFrameExpressionNode identifier)
        {
            if (CurrentToken.Kind != StackFrameKind.OpenBracketToken)
            {
                throw new InvalidOperationException();
            }

            var arrayBrackets = _lexer.ScanArrayBrackets();
            return new StackFrameArrayExpressionNode(identifier, arrayBrackets);
        }

        /// <summary>
        /// Parses text for a valid file path using valid file characters. It's very possible this includes a path that doesn't exist but
        /// forms a valid path identifier. 
        /// </summary>
        public (StackFrameFileInformationNode? fileInformation, bool isPathValid) ParseFileInformation()
        {
            var path = _lexer.ScanPath();
            if (!path.HasValue)
            {
                return (null, false);
            }

            // Make sure all the parts are valid as a file path
            var isValidFile = IOUtilities.PerformIO<bool>(() =>
            {
                var pathStr = path.Value.VirtualChars.CreateString();
                var file = new FileInfo(pathStr);
                var invalidFileChars = Path.GetInvalidFileNameChars();
                if (file.Name.Any(c => invalidFileChars.Contains(c)))
                {
                    return false;
                }

                var directory = file.Directory;
                var invalidDirectoryChars = Path.GetInvalidPathChars();
                if (directory.FullName.Any(c => invalidDirectoryChars.Contains(c)))
                {
                    return false;
                }

                return true;
            });

            if (!isValidFile)
            {
                return (new(path.Value), false);
            }

            if (!_lexer.ScanIfMatch(StackFrameKind.ColonToken, out var colonToken))
            {
                return (new(path.Value), true);
            }

            var lineIdentifier = _lexer.ScanLineTrivia();
            if (!lineIdentifier.HasValue)
            {
                // malformed, we have a "<path>: " with no "line " trivia
                // add the colonToken as trivia to the valid path and return it
                var colonTrivia = StackFrameLexer.CreateTrivia(StackFrameKind.Text, colonToken.VirtualChars);
                return
                    (new(path.Value.With(trailingTrivia: ImmutableArray.Create(colonTrivia)))
                    , true);
            }

            var numbers = _lexer.ScanNumbers();
            if (!numbers.HasValue)
            {
                // malformed, we have a "<path>:line " but no following number. 
                // Add the colon and line trivia as trailing trivia
                var jointTriviaSpan = new TextSpan(colonToken.GetSpan().Start, colonToken.VirtualChars.Length + lineIdentifier.Value.VirtualChars.Length);
                var trailingTrivia = StackFrameLexer.CreateTrivia(StackFrameKind.Text, _lexer.Text.GetSubSequence(jointTriviaSpan));
                return
                    (new(path.Value.With(trailingTrivia: ImmutableArray.Create(trailingTrivia)))
                    , true);
            }

            return
                (new(
                    path.Value,
                    colonToken,
                    numbers.Value.With(leadingTrivia: ImmutableArray.Create(lineIdentifier.Value)))
                , true);
        }
    }
}
