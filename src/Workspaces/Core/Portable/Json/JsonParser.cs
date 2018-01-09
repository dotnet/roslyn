// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VirtualChars;
using Roslyn.Utilities;
 
namespace Microsoft.CodeAnalysis.Json
{
    using System.Globalization;
    using static JsonHelpers;

    internal partial struct JsonParser
    {
        private static readonly string _closeBracketExpected = string.Format(WorkspacesResources._0_expected, ']');
        private static readonly string _closeBraceExpected = string.Format(WorkspacesResources._0_expected, '}');

        private JsonLexer _lexer;
        private JsonToken _currentToken;
        private int _recursionDepth;
        private bool _inObject;
        private bool _inArray;

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
        public static JsonTree TryParse(ImmutableArray<VirtualChar> text)
        {
            try
            {
                var tree1 = new JsonParser(text).ParseTree();
                return tree1;
            }
            catch (Exception e) when (StackGuard.IsInsufficientExecutionStackException(e))
            {
                return null;
            }
        }

        private JsonTree ParseTree()
        {
            var arraySequence = this.ParseSequence();
            Debug.Assert(_lexer.Position == _lexer.Text.Length);
            Debug.Assert(_currentToken.Kind == JsonKind.EndOfFile);

            var root = new JsonCompilationUnit(arraySequence, _currentToken);

            var diagnostic = GetDiagnostic(root) ?? CheckTopLevel(root) ?? CheckSyntax(root);

            var diagnostics = diagnostic == null
                ? ImmutableArray<JsonDiagnostic>.Empty
                : ImmutableArray.Create(diagnostic.Value);

            return new JsonTree(
                _lexer.Text, root, diagnostics);
        }

        private JsonDiagnostic? CheckTopLevel(JsonCompilationUnit compilationUnit)
        {
            var arraySequence = compilationUnit.Sequence;
            if (arraySequence.ChildCount == 0)
            {
                if (_lexer.Text.Length > 0 &&
                    compilationUnit.EndOfFileToken.LeadingTrivia.All(
                        t => t.Kind == JsonKind.WhitespaceTrivia || t.Kind == JsonKind.EndOfLineTrivia))
                {
                    return new JsonDiagnostic(WorkspacesResources.Syntax_error, GetSpan(_lexer.Text));
                }
            }
            else if (arraySequence.ChildCount >= 2)
            {
                var firstToken = GetFirstToken(arraySequence.ChildAt(1).Node);
                return new JsonDiagnostic(
                    string.Format(WorkspacesResources._0_unexpected, firstToken.VirtualChars[0].Char),
                    GetSpan(firstToken));
            }
            foreach (var child in compilationUnit.Sequence)
            {
                if (child.IsNode && child.Node.Kind == JsonKind.EmptyValue)
                {
                    var emptyValue = (JsonEmptyValueNode)child.Node;
                    return new JsonDiagnostic(
                        string.Format(WorkspacesResources._0_unexpected, ','),
                        GetSpan(emptyValue.CommaToken));
                }
            }
            
            //else if (arraySequence.ChildCount == 1)
            //{
            //    var value = (JsonValueNode)arraySequence.ChildAt(0).Node;
            //    if (!value.CommaToken.IsMissing)
            //    {
            //        return new JsonDiagnostic(
            //            string.Format(WorkspacesResources._0_unexpected, ','),
            //            GetSpan(value.CommaToken));
            //    }
            //}
            //else if (arraySequence.ChildCount > 1)
            //{
            //    var value = (JsonValueNode)arraySequence.ChildAt(1).Node;
            //    var firstToken = GetFirstToken(value);
            //    return new JsonDiagnostic(
            //        string.Format(WorkspacesResources._0_unexpected, firstToken.VirtualChars[0].Char),
            //        GetSpan(value));
            //}

            return null;
        }

        private JsonToken GetFirstToken(JsonNode node)
        {
            foreach (var child in node)
            {
                return child.IsNode
                    ? GetFirstToken(child.Node)
                    : child.Token;
            }

            throw new InvalidOperationException();
        }

        private static JsonDiagnostic? GetDiagnostic(JsonNode node)
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

        private static JsonDiagnostic? GetDiagnostic(JsonNodeOrToken child)
        {
            return child.IsNode
                ? GetDiagnostic(child.Node)
                : GetDiagnostic(child.Token);
        }

        private static JsonDiagnostic? GetDiagnostic(JsonToken token)
            => GetDiagnostic(token.LeadingTrivia) ?? token.Diagnostics.FirstOrNullable() ?? GetDiagnostic(token.TrailingTrivia);

        private static JsonDiagnostic? GetDiagnostic(ImmutableArray<JsonTrivia> list)
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

        private JsonDiagnostic? CheckSyntax(JsonNode node)
        {
            switch (node.Kind)
            {
                case JsonKind.Array:
                {
                    var diagnostic = CheckArray((JsonArrayNode)node);
                    if (diagnostic != null)
                    {
                        return diagnostic;
                    }
                }
                break;

                case JsonKind.Object:
                {
                    var diagnostic = CheckObject((JsonObjectNode)node);
                    if (diagnostic != null)
                    {
                        return diagnostic;
                    }
                }
                break;
            }

            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    var diagnostic = CheckSyntax(child.Node);
                    if (diagnostic != null)
                    {
                        return diagnostic;
                    }
                }
            }

            return null;
        }

        private JsonDiagnostic? CheckArray(JsonArrayNode node)
        {
            foreach (var child in node.Sequence)
            {
                var childNode = child.Node;
                if (childNode.Kind == JsonKind.Property)
                {
                    return new JsonDiagnostic(
                        WorkspacesResources.Property_not_allowed_in_a_json_array,
                        GetSpan(((JsonPropertyNode)childNode).ColonToken));
                }
            }


            for (int i = 0, n = node.Sequence.ChildCount - 1; i < n; i++)
            {
                var child = node.Sequence.ChildAt(i).Node;
                if (child.Kind != JsonKind.EmptyValue)
                {
                    var next = node.Sequence.ChildAt(i + 1).Node;

                    if (next.Kind != JsonKind.EmptyValue)
                    {
                        return new JsonDiagnostic(
                           string.Format(WorkspacesResources._0_expected, ','),
                           GetSpan(GetFirstToken(next)));
                    }
                }
            }

            return null;
        }

        private JsonDiagnostic? CheckObject(JsonObjectNode node)
        {
            for (int i = 0, n = node.Sequence.ChildCount; i < n; i++)
            {
                var child = node.Sequence.ChildAt(i).Node;

                if (i % 2 == 0)
                {
                    if (child.Kind != JsonKind.Property)
                    {
                        return new JsonDiagnostic(
                           WorkspacesResources.Only_properties_allowed_in_a_json_object,
                           GetSpan(GetFirstToken(child)));
                    }
                }
                else
                {
                    if (child.Kind != JsonKind.EmptyValue)
                    {
                        return new JsonDiagnostic(
                           string.Format(WorkspacesResources._0_expected, ','),
                           GetSpan(GetFirstToken(child)));
                    }
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
                    return ParseLiteralOrProperty();
            }
        }

        private JsonNegativeLiteralNode ParseNegativeInfinity(JsonToken literalToken)
        {
            SplitLiteral(literalToken, out var minusToken, out var newLiteralToken);

            return new JsonNegativeLiteralNode(
                minusToken, newLiteralToken.With(kind: JsonKind.InfinityLiteralToken));
        }

        private static void SplitLiteral(JsonToken literalToken, out JsonToken minusToken, out JsonToken newLiteralToken)
        {
            minusToken = new JsonToken(
                JsonKind.MinusToken, literalToken.LeadingTrivia,
                ImmutableArray.Create(literalToken.VirtualChars[0]),
                ImmutableArray<JsonTrivia>.Empty);
            newLiteralToken = new JsonToken(
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
                if (!IsLegalPropertyNameText(stringLiteralOrText))
                {
                    stringLiteralOrText = stringLiteralOrText.AddDiagnosticIfNone(new JsonDiagnostic(
                        WorkspacesResources.Invalid_property_name,
                        GetSpan(stringLiteralOrText)));
                }
            }

            var colonToken = ConsumeCurrentToken();
            // Newtonsoft allows "{ a: , }" as a legal property.
            if (_currentToken.Kind == JsonKind.CommaToken)
            {
                return new JsonPropertyNode(
                    stringLiteralOrText, colonToken,
                    new JsonEmptyValueNode(JsonToken.CreateMissing(JsonKind.CommaToken)));
            }
            else if (_currentToken.Kind == JsonKind.EndOfFile)
            {
                return new JsonPropertyNode(
                    stringLiteralOrText, colonToken,
                    new JsonEmptyValueNode(JsonToken.CreateMissing(JsonKind.CommaToken).AddDiagnosticIfNone(new JsonDiagnostic(
                        WorkspacesResources.Missing_property_value,
                        GetTokenStartPositionSpan(_currentToken)))));
            }

            var value = ParseValue();
            if (value.Kind == JsonKind.Property)
            {
                var nestedProperty = (JsonPropertyNode)value;
                value = new JsonPropertyNode(
                    nestedProperty.NameToken,
                    nestedProperty.ColonToken.AddDiagnosticIfNone(new JsonDiagnostic(
                        WorkspacesResources.Nested_properties_not_allowed,
                        GetSpan(nestedProperty.ColonToken))),
                    nestedProperty.Value);
            }

            return new JsonPropertyNode(
                stringLiteralOrText, colonToken, value);
        }

        private bool IsLegalPropertyNameText(JsonToken textToken)
        {
            foreach (var ch in textToken.VirtualChars)
            {
                if (!IsLegalPropertyNameChar(ch))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsLegalPropertyNameChar(char ch)
            => char.IsLetterOrDigit(ch) | ch == '_' || ch == '$';

        private JsonValueNode ParseLiteralOrProperty()
        {
            // var token = ConsumeCurrentToken().With(kind: JsonKind.TextToken);
            var textToken = ConsumeCurrentToken();
            if (_currentToken.Kind != JsonKind.ColonToken)
            {
                return ParseLiteralOrTextNode(textToken);
            }

            return ParseProperty(textToken);
        }

        private JsonValueNode ParseLiteralOrTextNode(JsonToken token)
        {
            if (token.Kind == JsonKind.StringToken)
            {
                return new JsonLiteralNode(token);
            }

            Debug.Assert(token.VirtualChars.Length > 0);
            var literalText = token.VirtualChars.CreateString();

            switch (literalText)
            {
                case "NaN": return ParseLiteral(token, JsonKind.NaNLiteralToken);
                case "true": return ParseLiteral(token, JsonKind.TrueLiteralToken);
                case "null": return ParseLiteral(token, JsonKind.NullLiteralToken);
                case "false": return ParseLiteral(token, JsonKind.FalseLiteralToken);
                case "Infinity": return ParseLiteral(token, JsonKind.InfinityLiteralToken);
                case "undefined": return ParseLiteral(token, JsonKind.UndefinedLiteralToken);
                case "-Infinity": return ParseNegativeInfinity(token);
            }

            var firstChar = token.VirtualChars[0];
            if (firstChar == '-' || firstChar == '.' || IsDigit(firstChar))
            {
                return ParseNumber(token, literalText);
            }

            return new JsonTextNode(
                token.With(kind: JsonKind.TextToken).AddDiagnosticIfNone(new JsonDiagnostic(
                    string.Format(WorkspacesResources._0_unexpected, firstChar.Char),
                    firstChar.Span)));
        }

        private bool IsDigit(char ch)
            => ch >= '0' && ch <= '9';

        private JsonLiteralNode ParseLiteral(JsonToken textToken, JsonKind kind)
            => new JsonLiteralNode(textToken.With(kind: kind));

        private JsonValueNode ParseNumber(JsonToken textToken, string literalText)
        {
            var numberToken = textToken.With(kind: JsonKind.NumberToken);
            var diagnostic = CheckNumberChars(numberToken, literalText);

            if (diagnostic != null)
            {
                numberToken = numberToken.AddDiagnosticIfNone(diagnostic.Value);
            }

            return new JsonLiteralNode(numberToken);
        }

        private JsonDiagnostic? CheckNumberChars(JsonToken numberToken, string literalText)
        {
            var chars = numberToken.VirtualChars;
            var firstChar = chars[0].Char;

            var singleDigit = char.IsDigit(firstChar) && chars.Length == 1;
            if (singleDigit)
            {
                return null;
            }

            var nonBase10 =
                firstChar == '0' && chars.Length > 1 &&
                chars[1] != '.' && chars[1] != 'e' && chars[1] != 'E';

            if (nonBase10)
            {
                Debug.Assert(chars.Length > 1);
                var b = chars[1] == 'x' || chars[1] == 'X' ? 16 : 8;

                try
                {
                    Convert.ToInt64(literalText, b);
                }
                catch (Exception)
                {
                    return new JsonDiagnostic(
                        WorkspacesResources.Invalid_number,
                        GetSpan(chars));
                }
            }
            else if (!double.TryParse(
                literalText, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out _))
            {
                return new JsonDiagnostic(
                    WorkspacesResources.Invalid_number,
                    GetSpan(chars));
            }

            return null;
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
                ParseClose(JsonKind.CloseBracketToken, _closeBracketExpected));

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
                ParseClose(JsonKind.CloseBraceToken, _closeBraceExpected));

            _inObject = savedInObject;
            return result;
        }

        //private JsonToken ConsumeOptionalCommaToken()
        //{
        //    return _currentToken.Kind == JsonKind.CommaToken
        //        ? ConsumeCurrentToken()
        //        : JsonToken.CreateMissing(JsonKind.CommaToken);
        //}

        private JsonToken ParseClose(JsonKind kind, string error)
        {
            if (_currentToken.Kind == kind)
            {
                return ConsumeCurrentToken();
            }
            else
            {
                return JsonToken.CreateMissing(kind).AddDiagnosticIfNone(
                    new JsonDiagnostic(error, GetTokenStartPositionSpan(_currentToken)));
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
