// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    /// <summary>
    /// Parser used for reading in a sequence of <see cref="VirtualChar"/>s, and producing a <see
    /// cref="JsonTree"/> out of it. Parsing will always succeed (except in the case of a
    /// stack-overflow) and will consume the entire sequence of chars.  General roslyn syntax
    /// principles are held to (i.e. immutable, fully representative, etc.).
    ///
    /// The parser always parses out the same tree regardless of input.  *However*, depending on the
    /// flags passed to it, it may return a different set of *diagnostics*.  Specifically, the
    /// parser supports json.net parsing and strict RFC8259 (https://tools.ietf.org/html/rfc8259).
    /// As such, the parser supports a superset of both, but then does a pass at the end to produce
    /// appropriate diagnostics.
    ///
    /// Note: the json structure we parse out is actually very simple.  It's effectively all lists
    /// of <see cref="JsonValueNode"/> values.  We just treat almost everything as a 'value'.  For
    /// example, a <see cref="JsonPropertyNode"/> (i.e. ```"x" = 0```) is a 'value'.  As such, it
    /// can show up in arrays (i.e.  ```["x" = 0, "y" = 1]```).  This is not legal, but it greatly
    /// simplifies parsing.  Effectively, we just have recursive list parsing, where we accept any
    /// sort of value in any sort of context.  A later pass will then report errors for the wrong
    /// sorts of values showing up in incorrect contexts.
    ///
    /// Note: We also treat commas (```,```) as being a 'value' on its own.  This simplifies parsing
    /// by allowing us to not have to represent Lists and SeparatedLists.  It also helps model
    /// things that are supported in json.net (like ```[1,,2]```).  Our post-parsing pass will
    /// then ensure that these comma-values only show up in the right contexts.
    /// </summary>
    internal partial struct JsonParser
    {
        private static readonly string _closeBracketExpected = string.Format(WorkspacesResources._0_expected, ']');
        private static readonly string _closeBraceExpected = string.Format(WorkspacesResources._0_expected, '}');
        private static readonly string _openParenExpected = string.Format(WorkspacesResources._0_expected, '(');
        private static readonly string _closeParenExpected = string.Format(WorkspacesResources._0_expected, ')');

        private JsonLexer _lexer;
        private JsonToken _currentToken;
        private int _recursionDepth;

        // Fields used to keep track of what types of json values we're in.  They're used for error
        // recovery, specifically with respect to encountering unexpected tokens while parsing out a
        // sequence of values.  For example, if we have:  ```{ a: [1, 2, }```, we will mark that
        // we're both in an object and in an array.  When we then encounter the errant ```}```,
        // we'll see that we were in an object, and thus should stop parsing out the sequence for
        // the array so that the ```}``` can be consume by the object we were in.  However, if we
        // just had ```[1, 2, }```, we would not be in an object, and we would just consume the
        // ```}``` as a bogus value inside the array.
        //
        // This approach of keeping track of the parse contexts we're in, and using them to
        // determine if we should consume or pop-out when encountering an error token, mirrors the
        // same approach that we use in the C# and TS/JS parsers.
        private bool _inObject;
        private bool _inArray;
        private bool _inConstructor;

        private JsonParser(VirtualCharSequence text) : this()
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
        /// diagnostics.  Parsing should always succeed, except in the case of the stack 
        /// overflowing.
        /// </summary>
        public static JsonTree? TryParse(VirtualCharSequence text, bool strict)
        {
            try
            {
                return new JsonParser(text).ParseTree(strict);
            }
            catch (InsufficientExecutionStackException)
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

            // We only report a single diagnostic when parsing out json.  This helps prevent lots of
            // cascading errors from being reported.  First, we see if there are any diagnostics
            // directly in tokens in the tree.  If not, we then check for any incorrect tree
            // structure (that would be incorrect for both json.net or strict-mode).  If we don't
            // run into any problems, we'll then perform specific json.net or strict-mode checks.
            var diagnostic = GetFirstDiagnostic(root) ?? CheckTopLevel(_lexer.Text, root);

            if (diagnostic == null)
            {
                // We didn't have any diagnostics in the tree so far.  Do the json.net/strict checks
                // depending on how we were invoked.
                diagnostic = strict
                    ? StrictSyntaxChecker.CheckSyntax(root)
                    : JsonNetSyntaxChecker.CheckSyntax(root);
            }

            var diagnostics = diagnostic == null
                ? ImmutableArray<EmbeddedDiagnostic>.Empty
                : ImmutableArray.Create(diagnostic.Value);

            return new JsonTree(
                _lexer.Text, root, diagnostics);
        }

        /// <summary>
        /// Checks for errors in json for both json.net and strict mode.
        /// </summary>
        private static EmbeddedDiagnostic? CheckTopLevel(
            VirtualCharSequence text, JsonCompilationUnit compilationUnit)
        {
            var arraySequence = compilationUnit.Sequence;
            if (arraySequence.ChildCount == 0)
            {
                // json is not allowed to be just whitespace.
                if (text.Length > 0 &&
                    compilationUnit.EndOfFileToken.LeadingTrivia.All(
                        t => t.Kind == JsonKind.WhitespaceTrivia || t.Kind == JsonKind.EndOfLineTrivia))
                {
                    return new EmbeddedDiagnostic(WorkspacesResources.Syntax_error, GetSpan(text));
                }
            }
            else if (arraySequence.ChildCount >= 2)
            {
                // the top level can't have more than one actual value.
                var firstToken = GetFirstToken(arraySequence.ChildAt(1).Node);
                return new EmbeddedDiagnostic(
                    string.Format(WorkspacesResources._0_unexpected, firstToken.VirtualChars[0]),
                    firstToken.GetSpan());
            }

            foreach (var child in compilationUnit.Sequence)
            {
                // Commas should never show up in the top level sequence.
                if (child.IsNode && child.Node.Kind == JsonKind.CommaValue)
                {
                    var emptyValue = (JsonCommaValueNode)child.Node;
                    return new EmbeddedDiagnostic(
                        string.Format(WorkspacesResources._0_unexpected, ','),
                        emptyValue.CommaToken.GetSpan());
                }
            }

            return null;
        }

        private static JsonToken GetFirstToken(JsonNodeOrToken nodeOrToken)
        {
            return nodeOrToken.IsNode ? GetFirstToken(nodeOrToken.Node.ChildAt(0)) : nodeOrToken.Token;
        }

        private static EmbeddedDiagnostic? GetFirstDiagnostic(JsonNode node)
        {
            foreach (var child in node)
            {
                var diagnostic = GetFirstDiagnostic(child);
                if (diagnostic != null)
                {
                    return diagnostic;
                }
            }

            return null;
        }

        private static EmbeddedDiagnostic? GetFirstDiagnostic(JsonNodeOrToken child)
        {
            return child.IsNode
                ? GetFirstDiagnostic(child.Node)
                : GetFirstDiagnostic(child.Token);
        }

        private static EmbeddedDiagnostic? GetFirstDiagnostic(JsonToken token)
            => GetFirstDiagnostic(token.LeadingTrivia) ?? token.Diagnostics.FirstOrNull() ?? GetFirstDiagnostic(token.TrailingTrivia);

        private static EmbeddedDiagnostic? GetFirstDiagnostic(ImmutableArray<JsonTrivia> list)
        {
            foreach (var trivia in list)
            {
                var diagnostic = trivia.Diagnostics.FirstOrNull();
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

            while (ShouldConsumeSequenceElement())
            {
                list.Add(ParseValue());
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
                    return ParseCommaValue();
                default:
                    return ParseLiteralOrPropertyOrConstructor();
            }
        }

        private static void SplitLiteral(JsonToken literalToken, out JsonToken minusToken, out JsonToken newLiteralToken)
        {
            minusToken = CreateToken(
                JsonKind.MinusToken, literalToken.LeadingTrivia,
                literalToken.VirtualChars.GetSubSequence(new TextSpan(0, 1)),
                ImmutableArray<JsonTrivia>.Empty);
            newLiteralToken = CreateToken(
                literalToken.Kind,
                ImmutableArray<JsonTrivia>.Empty,
                literalToken.VirtualChars.GetSubSequence(TextSpan.FromBounds(1, literalToken.VirtualChars.Length)),
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
                    new JsonCommaValueNode(CreateMissingToken(JsonKind.CommaToken)));
            }
            else if (_currentToken.Kind == JsonKind.EndOfFile)
            {
                return new JsonPropertyNode(
                    stringLiteralOrText, colonToken,
                    new JsonCommaValueNode(CreateMissingToken(JsonKind.CommaToken).AddDiagnosticIfNone(new EmbeddedDiagnostic(
                        WorkspacesResources.Missing_property_value,
                        GetTokenStartPositionSpan(_currentToken)))));
            }

            var value = ParseValue();
            if (value.Kind == JsonKind.Property)
            {
                // It's always illegal to have something like  ```"a" : "b" : 1```
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

            // Look for constructors (a json.net extension).  We'll report them as an error
            // in strict model.
            if (JsonParser.Matches(token, "new"))
            {
                return ParseConstructor(token);
            }

            // Check for certain literal values.  Some of these (like NaN) are json.net only.
            // We'll check for these later in the strict-mode pass.
            Debug.Assert(token.VirtualChars.Length > 0);
            if (JsonParser.TryMatch(token, "NaN", JsonKind.NaNLiteralToken, out var newKind) ||
                JsonParser.TryMatch(token, "true", JsonKind.TrueLiteralToken, out newKind) ||
                JsonParser.TryMatch(token, "null", JsonKind.NullLiteralToken, out newKind) ||
                JsonParser.TryMatch(token, "false", JsonKind.FalseLiteralToken, out newKind) ||
                JsonParser.TryMatch(token, "Infinity", JsonKind.InfinityLiteralToken, out newKind) ||
                JsonParser.TryMatch(token, "undefined", JsonKind.UndefinedLiteralToken, out newKind))
            {
                return new JsonLiteralNode(token.With(kind: newKind));
            }

            if (JsonParser.Matches(token, "-Infinity"))
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
                    string.Format(WorkspacesResources._0_unexpected, firstChar.ToString()),
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

        private static bool TryMatch(JsonToken token, string val, JsonKind kind, out JsonKind newKind)
        {
            if (JsonParser.Matches(token, val))
            {
                newKind = kind;
                return true;
            }

            newKind = default;
            return false;
        }

        private static bool Matches(JsonToken token, string val)
        {
            var chars = token.VirtualChars;
            if (chars.Length != val.Length)
                return false;

            for (var i = 0; i < val.Length; i++)
            {
                if (chars[i] != val[i])
                    return false;
            }

            return true;
        }

        private static bool IsDigit(VirtualChar ch)
            => ch >= '0' && ch <= '9';

        private static JsonLiteralNode ParseLiteral(JsonToken textToken, JsonKind kind)
            => new JsonLiteralNode(textToken.With(kind: kind));

        private static JsonValueNode ParseNumber(JsonToken textToken)
        {
            var numberToken = textToken.With(kind: JsonKind.NumberToken);
            return new JsonLiteralNode(numberToken);
        }

        private JsonCommaValueNode ParseCommaValue()
            => new JsonCommaValueNode(ConsumeCurrentToken());

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
