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
    internal struct StackFrameParser
    {
        private StackFrameLexer _lexer;

        private StackFrameParser(StackFrameLexer lexer)
        {
            _lexer = lexer;
        }

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
                var lexer = StackFrameLexer.TryCreate(text);
                if (!lexer.HasValue)
                {
                    return null;
                }

                return new StackFrameParser(lexer.Value).TryParseTree();
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
            var methodDeclaration = TryParseRequiredMethodDeclaration();
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

            var eolToken = CurrentCharAsToken().With(leadingTrivia: remainingTrivia.ToImmutableArray());

            Contract.ThrowIfFalse(_lexer.Position == _lexer.Text.Length);
            Contract.ThrowIfFalse(eolToken.Kind == StackFrameKind.EndOfFrame);

            var root = new StackFrameCompilationUnit(methodDeclaration, fileInformationResult.Value, eolToken);

            return new(_lexer.Text, root);
        }

        /// <summary>
        /// Attempts to parse the full method declaration, optionally adding leading whitespace as trivia. Includes
        /// all of the generic indicators for types, 
        /// 
        /// Ex: [|MyClass.MyMethod(string s)|]
        /// </summary>
        private StackFrameMethodDeclarationNode? TryParseRequiredMethodDeclaration()
        {
            var identifierNode = TryParseRequiredNameNode(scanAtTrivia: true);

            //
            // TryParseRequiredNameNode does not necessarily return a qualified name even if 
            // it parses a name. For method declarations, a fully qualified name is required so
            // we know both the class (and namespace) that the method is contained in.  
            //
            if (identifierNode is not StackFrameQualifiedNameNode memberAccessExpression)
            {
                return null;
            }

            var (success, typeArguments) = TryParseTypeArguments();
            if (!success)
            {
                return null;
            }

            var methodParameters = TryParseRequiredMethodParameters();
            if (methodParameters is null)
            {
                return null;
            }

            return new StackFrameMethodDeclarationNode(memberAccessExpression, typeArguments, methodParameters);
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
        private StackFrameNameNode? TryParseRequiredNameNode(bool scanAtTrivia)
        {
            var currentIdentifer = _lexer.TryScanIdentifier(scanAtTrivia: scanAtTrivia, scanLeadingWhitespace: true, scanTrailingWhitespace: false);
            if (!currentIdentifer.HasValue)
            {
                return null;
            }

            var (success, genericIdentifier) = TryScanGenericTypeIdentifier(currentIdentifer.Value);
            if (!success)
            {
                return null;
            }

            RoslynDebug.AssertNotNull(genericIdentifier);
            StackFrameNameNode nameNode = genericIdentifier;

            while (true)
            {
                (success, var memberAccess) = TryParseQualifiedName(nameNode);
                if (!success)
                {
                    return null;
                }

                if (memberAccess is null)
                {
                    Debug.Assert(nameNode is StackFrameQualifiedNameNode or StackFrameSimpleNameNode);
                    return nameNode;
                }

                nameNode = memberAccess;
            }
        }

        /// <summary>
        /// Given an existing left hand side node or token, parse a qualified name if possible. Returns 
        /// null with success if a dot token is not available
        /// </summary>
        private Result<StackFrameQualifiedNameNode> TryParseQualifiedName(StackFrameNameNode lhs)
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.DotToken, out var dotToken))
            {
                return Result<StackFrameQualifiedNameNode>.Empty;
            }

            // Check if this is a generated identifier
            (var success, StackFrameSimpleNameNode? rhs) = TryScanGeneratedName();
            if (!success)
            {
                return Result<StackFrameQualifiedNameNode>.Abort;
            }

            if (rhs is not null)
            {
                return new StackFrameQualifiedNameNode(lhs, dotToken, rhs);
            }

            // The identifier is not a generated name, parse as a normal identifier and check for generics
            var identifier = _lexer.TryScanIdentifier();
            if (!identifier.HasValue)
            {
                return Result<StackFrameQualifiedNameNode>.Abort;
            }

            (success, rhs) = TryScanGenericTypeIdentifier(identifier.Value);

            if (!success)
            {
                return Result<StackFrameQualifiedNameNode>.Abort;
            }

            RoslynDebug.AssertNotNull(rhs);
            return new StackFrameQualifiedNameNode(lhs, dotToken, rhs);
        }

        /// <summary>
        /// Generated names are unutterables made by the compiler. This can include async code, top level statement main, local
        /// functions, anonymous types, etc. 
        /// 
        /// <code>
        /// 
        ///     examples:
        ///     
        ///     1. GeneratedMethodName
        ///            Program.&lt;Main&gt;$
        ///                    ^-------------- Beginning of generated name
        ///                        ^---^------ Identifier "Main"
        ///                             ^--^-- End of generated name with "&lt;$" 
        ///     2. LocalMethodName
        ///            C.&lt;MyMethod&gt;g__Local|0_0(String s)
        ///              ^--------------------------------------- Beginning of generated name
        ///                  ^------^---------------------------- Encapsulating method name
        ///                              ^----------------------- "g__" identifies this as a local function. 
        ///                                 ^----^--------------- "Local" is the name of the local function
        ///                                      ^---^----------- "|0_0" is suffix information such as slot 
        ///                                           ^--------^- "(String s)" identifiers the method paramters
        /// </code>
        /// </summary>
        private Result<StackFrameGeneratedNameNode> TryScanGeneratedName()
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.LessThanToken, out var lessThanToken))
            {
                return Result<StackFrameGeneratedNameNode>.Empty;
            }

            if (_lexer.CurrentCharAsToken().Kind == StackFrameKind.LessThanToken)
            {
                // Nested generated names? Abort for now
                // TODO: Actually handle this
                return Result<StackFrameGeneratedNameNode>.Abort;
            }

            var identifier = _lexer.TryScanIdentifier();
            if (!identifier.HasValue)
            {
                return Result<StackFrameGeneratedNameNode>.Abort;
            }

            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.GreaterThanToken, out var greaterThanToken))
            {
                return Result<StackFrameGeneratedNameNode>.Abort;
            }

            if (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.DollarToken, out var dollarToken))
            {
                return new StackFrameGeneratedMethodNameNode(lessThanToken, identifier.Value, greaterThanToken, dollarToken);
            }

            var currentChar = _lexer.CurrentChar.Value;

            // Check for generated name kinds we can handle
            // See https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Symbols/Synthesized/GeneratedNameKind.cs 
            if (currentChar == 'g')
            {
                // Local function
                var encapsulatingMethod = new StackFrameGeneratedMethodNameNode(lessThanToken, identifier.Value, greaterThanToken, dollarToken: null);
                var (success, generatedNameSeparator) = _lexer.TryScanRequiredGeneratedNameSeparator();
                if (!success)
                {
                    return Result<StackFrameGeneratedNameNode>.Abort;
                }

                var generatedIdentifier = _lexer.TryScanIdentifier();
                if (!generatedIdentifier.HasValue)
                {
                    return Result<StackFrameGeneratedNameNode>.Abort;
                }

                if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.PipeToken, out var suffixSeparator))
                {
                    return Result<StackFrameGeneratedNameNode>.Abort;
                }

                (success, var suffix) = _lexer.TryScanRequiredGeneratedNameSuffix();
                if (!success)
                {
                    return Result<StackFrameGeneratedNameNode>.Abort;
                }

                return new StackFrameLocalMethodNameNode(encapsulatingMethod, generatedNameSeparator, generatedIdentifier.Value, suffixSeparator, suffix);
            }
            else
            {
                return Result<StackFrameGeneratedNameNode>.Abort;
            }
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
        /// </summary>
        private Result<StackFrameSimpleNameNode> TryScanGenericTypeIdentifier(StackFrameToken identifierToken)
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.GraveAccentToken, out var graveAccentToken))
            {
                return new(new StackFrameIdentifierNameNode(identifierToken));
            }

            var arity = _lexer.TryScanNumbers();
            if (!arity.HasValue)
            {
                return Result<StackFrameSimpleNameNode>.Abort;
            }

            return new StackFrameGenericNameNode(identifierToken, graveAccentToken, arity.Value);
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
        private Result<StackFrameTypeArgumentList> TryParseTypeArguments()
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(
                    kind => kind is StackFrameKind.OpenBracketToken or StackFrameKind.LessThanToken,
                    out var openToken))
            {
                return Result<StackFrameTypeArgumentList>.Empty;
            }

            var closeBracketKind = openToken.Kind is StackFrameKind.OpenBracketToken
                ? StackFrameKind.CloseBracketToken
                : StackFrameKind.GreaterThanToken;

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);
            var currentIdentifier = _lexer.TryScanIdentifier(scanAtTrivia: false, scanLeadingWhitespace: true, scanTrailingWhitespace: true);
            StackFrameToken closeToken = default;

            while (currentIdentifier.HasValue && currentIdentifier.Value.Kind == StackFrameKind.IdentifierToken)
            {
                builder.Add(new StackFrameIdentifierNameNode(currentIdentifier.Value));

                if (_lexer.ScanCurrentCharAsTokenIfMatch(closeBracketKind, out closeToken))
                {
                    break;
                }

                if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CommaToken, out var commaToken))
                {
                    return Result<StackFrameTypeArgumentList>.Abort;
                }

                builder.Add(commaToken);
                currentIdentifier = _lexer.TryScanIdentifier();
            }

            if (builder.Count == 0)
            {
                return Result<StackFrameTypeArgumentList>.Abort;
            }

            if (closeToken.IsMissing)
            {
                return Result<StackFrameTypeArgumentList>.Abort;
            }

            var separatedList = new EmbeddedSeparatedSyntaxNodeList<StackFrameKind, StackFrameNode, StackFrameIdentifierNameNode>(builder.ToImmutable());
            return new StackFrameTypeArgumentList(openToken, separatedList, closeToken);
        }

        /// <summary>
        /// MyNamespace.MyClass.MyMethod[|(string s1, string s2, int i1)|]
        /// Takes parameter declarations from method text and parses them into a <see cref="StackFrameParameterList"/>.
        /// </summary>
        /// <remarks>
        /// This method assumes that the caller requires method parameters, and returns null for all failure cases. The caller
        /// should escalate to abort parsing on null values. 
        /// </remarks>
        private StackFrameParameterList? TryParseRequiredMethodParameters()
        {
            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.OpenParenToken, scanTrailingWhitespace: true, out var openParen))
            {
                return null;
            }

            if (_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.CloseParenToken, out var closeParen))
            {
                return new(openParen, EmbeddedSeparatedSyntaxNodeList<StackFrameKind, StackFrameNode, StackFrameParameterDeclarationNode>.Empty, closeParen);
            }

            using var _ = ArrayBuilder<StackFrameNodeOrToken>.GetInstance(out var builder);

            while (true)
            {
                var (success, parameterNode) = ParseParameterNode();
                if (!success)
                {
                    return null;
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
                return null;
            }

            var parameters = new EmbeddedSeparatedSyntaxNodeList<StackFrameKind, StackFrameNode, StackFrameParameterDeclarationNode>(builder.ToImmutable());
            return new(openParen, parameters, closeParen);
        }

        /// <summary>
        /// Parses a <see cref="StackFrameParameterDeclarationNode"/> by parsing identifiers first representing the type and then the parameter identifier.
        /// Ex: System.String[] s
        ///     ^--------------^ -- Type = "System.String[]"
        ///                     ^-- Identifier = "s"    
        /// </summary>
        private Result<StackFrameParameterDeclarationNode> ParseParameterNode()
        {
            var nameNode = TryParseRequiredNameNode(scanAtTrivia: false);
            if (nameNode is null)
            {
                return Result<StackFrameParameterDeclarationNode>.Abort;
            }

            StackFrameTypeNode? typeIdentifier = nameNode;
            if (CurrentCharAsToken().Kind == StackFrameKind.OpenBracketToken)
            {
                var (success, arrayIdentifiers) = ParseArrayRankSpecifiers();
                if (!success || arrayIdentifiers.IsDefault)
                {
                    return Result<StackFrameParameterDeclarationNode>.Abort;
                }

                typeIdentifier = new StackFrameArrayTypeNode(nameNode, arrayIdentifiers);
            }

            var identifier = _lexer.TryScanIdentifier(scanAtTrivia: false, scanLeadingWhitespace: true, scanTrailingWhitespace: true);
            if (!identifier.HasValue)
            {
                return Result<StackFrameParameterDeclarationNode>.Abort;
            }

            return new StackFrameParameterDeclarationNode(typeIdentifier, identifier.Value);
        }

        /// <summary>
        /// Parses the array rank specifiers for an identifier. 
        /// Ex: string[,][]
        ///           ^----^ both are array rank specifiers
        ///                  0: "[,]
        ///                  1: "[]"
        /// </summary>
        private Result<ImmutableArray<StackFrameArrayRankSpecifier>> ParseArrayRankSpecifiers()
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
                    return Result<ImmutableArray<StackFrameArrayRankSpecifier>>.Abort;
                }

                builder.Add(new StackFrameArrayRankSpecifier(openBracket, closeBracket, commaBuilder.ToImmutableAndClear()));
            }
        }

        /// <summary>
        /// Parses text for a valid file path using valid file characters. It's very possible this includes a path that doesn't exist but
        /// forms a valid path identifier. 
        /// 
        /// Can return if only a path is available but not line numbers, but throws if the value after the path is a colon as the expectation
        /// is that line number should follow.
        /// </summary>
        private Result<StackFrameFileInformationNode> TryParseFileInformation()
        {
            var (success, path) = _lexer.TryScanPath();
            if (!success)
            {
                return Result<StackFrameFileInformationNode>.Abort;
            }

            if (path.Kind == StackFrameKind.None)
            {
                return Result<StackFrameFileInformationNode>.Empty;
            }

            if (!_lexer.ScanCurrentCharAsTokenIfMatch(StackFrameKind.ColonToken, out var colonToken))
            {
                return new StackFrameFileInformationNode(path, colon: null, line: null);
            }

            var lineNumber = _lexer.TryScanRequiredLineNumber();
            if (!lineNumber.HasValue)
            {
                return Result<StackFrameFileInformationNode>.Abort;
            }

            return new StackFrameFileInformationNode(path, colonToken, lineNumber);
        }
    }
}
