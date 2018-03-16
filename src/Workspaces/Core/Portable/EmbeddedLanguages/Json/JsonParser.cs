// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json
{
    using static EmbeddedSyntaxHelpers;
    using static JsonHelpers;

    using JsonNodeOrToken = EmbeddedSyntaxNodeOrToken<JsonKind, JsonNode>;
    using JsonToken = EmbeddedSyntaxToken<JsonKind>;
    using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;

    internal partial struct JsonParser
    {
        private static readonly string _closeBracketExpected = string.Format(WorkspacesResources._0_expected, ']');
        private static readonly string _closeBraceExpected = string.Format(WorkspacesResources._0_expected, '}');
        private static readonly string _openParenExpected = string.Format(WorkspacesResources._0_expected, '(');
        private static readonly string _closeParenExpected = string.Format(WorkspacesResources._0_expected, ')');

        private JsonLexer _lexer;
        private JsonToken _currentToken;
        private int _recursionDepth;
        private bool _inObject;
        private bool _inArray;
        private bool _inConstructor;

        private JsonParser(
            ImmutableArray<VirtualChar> text) : this()
        {
            _lexer = new JsonLexer(text);

            // Get the first token.
            ConsumeCurrentToken();
        }

        /// <summary>
        /// Returns the latest token the lexer has produced, and then asks the lexer to 
        /// produce the next token after that.
        /// </summary>
        private JsonToken ConsumeCurrentToken()
        {
            var previous = _currentToken;
            _currentToken = _lexer.ScanNextToken();
            return previous;
        }

        /// <summary>
        /// Given an input text, parses out a fully representative syntax tree  and list of 
        /// diagnotics.  Parsing should always succeed, except in the case of the stack 
        /// overflowing.
        /// </summary>
        public static JsonTree TryParse(ImmutableArray<VirtualChar> text, bool strict)
        {
            try
            {
                var tree1 = new JsonParser(text).ParseTree(strict);
                return tree1;
            }
            catch (Exception e) when (StackGuard.IsInsufficientExecutionStackException(e))
            {
                return null;
            }
        }

        private JsonTree ParseTree(bool strict)
        {
            var arraySequence = this.ParseSequence();
            Debug.Assert(_lexer.Position == _lexer.Text.Length);
            Debug.Assert(_currentToken.Kind == JsonKind.EndOfFile);

            var root = new JsonCompilationUnit(arraySequence, _currentToken);

            var diagnostic = GetDiagnostic(root) ?? CheckTopLevel(_lexer.Text, root);
            if (diagnostic == null)
            {
                diagnostic = strict
                    ? new StrictSyntaxChecker().CheckSyntax(root)
                    : new JsonNetSyntaxChecker().CheckSyntax(root);
            }

            var diagnostics = diagnostic == null
                ? ImmutableArray<EmbeddedDiagnostic>.Empty
                : ImmutableArray.Create(diagnostic.Value);

            return new JsonTree(
                _lexer.Text, root, diagnostics);
        }

        private EmbeddedDiagnostic? CheckTopLevel(
            ImmutableArray<VirtualChar> text, JsonCompilationUnit compilationUnit)
        {
            var arraySequence = compilationUnit.Sequence;
            if (arraySequence.ChildCount == 0)
            {
                if (text.Length > 0 &&
                    compilationUnit.EndOfFileToken.LeadingTrivia.All(
                        t => t.Kind == JsonKind.WhitespaceTrivia || t.Kind == JsonKind.EndOfLineTrivia))
                {
                    return new EmbeddedDiagnostic(WorkspacesResources.Syntax_error, GetSpan(text));
                }
            }
            else if (arraySequence.ChildCount >= 2)
            {
                var firstToken = GetFirstToken(arraySequence.ChildAt(1).Node);
                return new EmbeddedDiagnostic(
                    string.Format(WorkspacesResources._0_unexpected, firstToken.VirtualChars[0].Char),
                    firstToken.GetSpan());
            }
            foreach (var child in compilationUnit.Sequence)
            {
                if (child.IsNode && child.Node.Kind == JsonKind.EmptyValue)
                {
                    var emptyValue = (JsonEmptyValueNode)child.Node;
                    return new EmbeddedDiagnostic(
                        string.Format(WorkspacesResources._0_unexpected, ','),
                        emptyValue.CommaToken.GetSpan());
                }
            }

            return null;
        }

        private static JsonToken GetFirstToken(JsonNode node)
        {
            foreach (var child in node)
            {
                return child.IsNode
                    ? GetFirstToken(child.Node)
                    : child.Token;
            }

            throw new InvalidOperationException();
        }

        private static EmbeddedDiagnostic? GetDiagnostic(JsonNode node)
        {
            foreach (var child in node)
            {
                var diagnostic = GetDiagnostic(child);
                if (diagnostic != null)
                {
                    return diagnostic;
                }
            }

            return null;
        }

        private static EmbeddedDiagnostic? GetDiagnostic(JsonNodeOrToken child)
        {
            return child.IsNode
                ? GetDiagnostic(child.Node)
                : GetDiagnostic(child.Token);
        }

        private static EmbeddedDiagnostic? GetDiagnostic(JsonToken token)
            => GetDiagnostic(token.LeadingTrivia) ?? token.Diagnostics.FirstOrNullable() ?? GetDiagnostic(token.TrailingTrivia);

        private static EmbeddedDiagnostic? GetDiagnostic(ImmutableArray<JsonTrivia> list)
        {
            foreach (var trivia in list)
            {
                var diagnostic = trivia.Diagnostics.FirstOrNullable();
                if (diagnostic != null)
                {
                    return diagnostic;
                }
            }

            return null;
        }

        private JsonSequenceNode ParseSequence()
        {
            try
            {
                _recursionDepth++;
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                return ParseSequenceWorker();
            }
            finally
            {
                _recursionDepth--;
            }
        }

        private JsonSequenceNode ParseSequenceWorker()
        {
            var list = ArrayBuilder<JsonValueNode>.GetInstance();

            if (ShouldConsumeSequenceElement())
            {
                do
                {
                    list.Add(ParseValue());
                }
                while (ShouldConsumeSequenceElement());
            }

            return new JsonSequenceNode(list.ToImmutableAndFree());
        }

        private bool ShouldConsumeSequenceElement()
        {
            if (_currentToken.Kind == JsonKind.EndOfFile)
            {
                return false;
            }

            if (_currentToken.Kind == JsonKind.CloseBraceToken)
            {
                return !_inObject;
            }

            if (_currentToken.Kind == JsonKind.CloseBracketToken)
            {
                return !_inArray;
            }

            if (_currentToken.Kind == JsonKind.CloseParenToken)
            {
                return !_inConstructor;
            }

            return true;
        }

        private JsonValueNode ParseValue()
        {
            switch (_currentToken.Kind)
            {
                case JsonKind.OpenBraceToken:
                    return ParseObject();
                case JsonKind.OpenBracketToken:
                    return ParseArray();
                case JsonKind.CommaToken:
                    return ParseEmptyValue();
                default:
                    return ParseLiteralOrPropertyOrConstructor();
            }
        }

        private static void SplitLiteral(JsonToken literalToken, out JsonToken minusToken, out JsonToken newLiteralToken)
        {
            minusToken = CreateToken(
                JsonKind.MinusToken, literalToken.LeadingTrivia,
                ImmutableArray.Create(literalToken.VirtualChars[0]),
                ImmutableArray<JsonTrivia>.Empty);
            newLiteralToken = CreateToken(
                literalToken.Kind,
                ImmutableArray<JsonTrivia>.Empty,
                literalToken.VirtualChars.Skip(1).ToImmutableArray(),
                literalToken.TrailingTrivia,
                literalToken.Diagnostics);
        }

        private JsonPropertyNode ParseProperty(JsonToken stringLiteralOrText)
        {
            Debug.Assert(_currentToken.Kind == JsonKind.ColonToken);
            if (stringLiteralOrText.Kind != JsonKind.StringToken)
            {
                stringLiteralOrText = stringLiteralOrText.With(kind: JsonKind.TextToken);
            }

            var colonToken = ConsumeCurrentToken();
            // Newtonsoft allows "{ a: , }" as a legal property.
            if (_currentToken.Kind == JsonKind.CommaToken)
            {
                return new JsonPropertyNode(
                    stringLiteralOrText, colonToken,
                    new JsonEmptyValueNode(CreateMissingToken(JsonKind.CommaToken)));
            }
            else if (_currentToken.Kind == JsonKind.EndOfFile)
            {
                return new JsonPropertyNode(
                    stringLiteralOrText, colonToken,
                    new JsonEmptyValueNode(CreateMissingToken(JsonKind.CommaToken).AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        WorkspacesResources.Missing_property_value,
                        GetTokenStartPositionSpan(_currentToken)))));
            }

            var value = ParseValue();
            if (value.Kind == JsonKind.Property)
            {
                var nestedProperty = (JsonPropertyNode)value;
                value = new JsonPropertyNode(
                    nestedProperty.NameToken,
                    nestedProperty.ColonToken.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        WorkspacesResources.Nested_properties_not_allowed,
                        nestedProperty.ColonToken.GetSpan())),
                    nestedProperty.Value);
            }

            return new JsonPropertyNode(
                stringLiteralOrText, colonToken, value);
        }

        private JsonValueNode ParseLiteralOrPropertyOrConstructor()
        {
            // var token = ConsumeCurrentToken().With(kind: JsonKind.TextToken);
            var textToken = ConsumeCurrentToken();
            if (_currentToken.Kind != JsonKind.ColonToken)
            {
                return ParseLiteralOrTextOrConstructor(textToken);
            }

            return ParseProperty(textToken);
        }

        private JsonValueNode ParseLiteralOrTextOrConstructor(JsonToken token)
        {
            if (token.Kind == JsonKind.StringToken)
            {
                return new JsonLiteralNode(token);
            }

            if (Matches(token, "new"))
            {
                return ParseConstructor(token);
            }

            Debug.Assert(token.VirtualChars.Length > 0);
            if (TryMatch(token, "NaN", JsonKind.NaNLiteralToken, out var newKind) ||
                TryMatch(token, "true", JsonKind.TrueLiteralToken, out newKind) ||
                TryMatch(token, "null", JsonKind.NullLiteralToken, out newKind) ||
                TryMatch(token, "false", JsonKind.FalseLiteralToken, out newKind) ||
                TryMatch(token, "Infinity", JsonKind.InfinityLiteralToken, out newKind) ||
                TryMatch(token, "undefined", JsonKind.UndefinedLiteralToken, out newKind))
            {
                return new JsonLiteralNode(token.With(kind: newKind));
            }

            if (Matches(token, "-Infinity"))
            {
                SplitLiteral(token, out var minusToken, out var newLiteralToken);

                return new JsonNegativeLiteralNode(
                    minusToken, newLiteralToken.With(kind: JsonKind.InfinityLiteralToken));
            }

            var firstChar = token.VirtualChars[0];
            if (firstChar == '-' || firstChar == '.' || IsDigit(firstChar))
            {
                return ParseNumber(token);
            }

            return new JsonTextNode(
                token.With(kind: JsonKind.TextToken).AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    string.Format(WorkspacesResources._0_unexpected, firstChar.Char),
                    firstChar.Span)));
        }

        private JsonConstructorNode ParseConstructor(JsonToken token)
        {
            var newKeyword = token.With(kind: JsonKind.NewKeyword);
            var nameToken = ConsumeToken(JsonKind.TextToken, WorkspacesResources.Name_expected);
            var openParen = ConsumeToken(JsonKind.OpenParenToken, _openParenExpected);

            var savedInConstructor = _inConstructor;
            _inConstructor = true;

            var result = new JsonConstructorNode(
                newKeyword,
                nameToken,
                openParen,
                ParseSequence(),
                ConsumeToken(JsonKind.CloseParenToken, _closeParenExpected));

            _inConstructor = savedInConstructor;
            return result;
        }

        private bool TryMatch(JsonToken token, string val, JsonKind kind, out JsonKind newKind)
        {
            if (Matches(token, val))
            {
                newKind = kind;
                return true;
            }

            newKind = default;
            return false;
        }

        private bool Matches(JsonToken token, string val)
        {
            var chars = token.VirtualChars;
            if (chars.Length != val.Length)
            {
                return false;
            }

            for (int i = 0, n = val.Length; i < n; i++)
            {
                if (chars[i].Char != val[i])
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsDigit(char ch)
            => ch >= '0' && ch <= '9';

        private JsonLiteralNode ParseLiteral(JsonToken textToken, JsonKind kind)
            => new JsonLiteralNode(textToken.With(kind: kind));

        private JsonValueNode ParseNumber(JsonToken textToken)
        {
            var numberToken = textToken.With(kind: JsonKind.NumberToken);
            return new JsonLiteralNode(numberToken);
        }

        private JsonEmptyValueNode ParseEmptyValue()
            => new JsonEmptyValueNode(ConsumeCurrentToken());

        private JsonArrayNode ParseArray()
        {
            var savedInArray = _inArray;
            _inArray = true;

            var result = new JsonArrayNode(
                ConsumeCurrentToken(),
                ParseSequence(),
                ConsumeToken(JsonKind.CloseBracketToken, _closeBracketExpected));

            _inArray = savedInArray;
            return result;
        }

        private JsonObjectNode ParseObject()
        {
            var savedInObject = _inObject;
            _inObject = true;

            var result = new JsonObjectNode(
                ConsumeCurrentToken(),
                ParseSequence(),
                ConsumeToken(JsonKind.CloseBraceToken, _closeBraceExpected));

            _inObject = savedInObject;
            return result;
        }

        private JsonToken ConsumeToken(JsonKind kind, string error)
        {
            if (_currentToken.Kind == kind)
            {
                return ConsumeCurrentToken();
            }
            else
            {
                return CreateMissingToken(kind).AddDiagnosticIfNone(
                    new EmbeddedDiagnostic(error, GetTokenStartPositionSpan(_currentToken)));
            }
        }

        private TextSpan GetTokenStartPositionSpan(JsonToken token)
        {
            return token.Kind == JsonKind.EndOfFile
                ? new TextSpan(_lexer.Text.Last().Span.End, 0)
                : new TextSpan(token.VirtualChars[0].Span.Start, 0);
        }
    }
}
