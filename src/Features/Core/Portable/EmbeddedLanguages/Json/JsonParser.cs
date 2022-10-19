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

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json
{
    using static EmbeddedSyntaxHelpers;
    using static JsonHelpers;

    using JsonNodeOrToken = EmbeddedSyntaxNodeOrToken<JsonKind, JsonNode>;
    using JsonToken = EmbeddedSyntaxToken<JsonKind>;
    using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;
    using JsonSeparatedList = EmbeddedSeparatedSyntaxNodeList<JsonKind, JsonNode, JsonValueNode>;

    /// <summary>
    /// Parser used for reading in a sequence of <see cref="VirtualChar"/>s, and producing a <see
    /// cref="JsonTree"/> out of it. Parsing will always succeed (except in the case of a
    /// stack-overflow) and will consume the entire sequence of chars.  General roslyn syntax
    /// principles are held to (i.e. immutable, fully representative, etc.).
    /// <para>
    /// The parser always parses out the same tree regardless of input.  *However*, depending on the
    /// flags passed to it, it may return a different set of *diagnostics*.  Specifically, the
    /// parser supports json.net parsing and strict RFC8259 (https://tools.ietf.org/html/rfc8259).
    /// As such, the parser supports a superset of both, but then does a pass at the end to produce
    /// appropriate diagnostics.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Note: the json structure we parse out is actually very simple.  It's effectively all lists
    /// of <see cref="JsonValueNode"/> values.  We just treat almost everything as a 'value'.  For
    /// example, a <see cref="JsonPropertyNode"/> (i.e. <c>"x" = 0</c>) is a 'value'.  As such, it
    /// can show up in arrays (i.e.  <c>["x" = 0, "y" = 1]</c>).  This is not legal, but it greatly
    /// simplifies parsing.  Effectively, we just have recursive list parsing, where we accept any
    /// sort of value in any sort of context.  A later pass will then report errors for the wrong
    /// sorts of values showing up in incorrect contexts.
    /// <para>
    /// Note: We also treat commas (<c>,</c>) as being a 'value' on its own.  This simplifies parsing
    /// by allowing us to not have to represent Lists and SeparatedLists.  It also helps model
    /// things that are supported in json.net (like <c>[1,,2]</c>).  Our post-parsing pass will
    /// then ensure that these comma-values only show up in the right contexts.
    /// </para>
    /// </remarks>
    [NonCopyable]
    internal partial struct JsonParser
    {
        private static readonly string s_closeBracketExpected = string.Format(FeaturesResources._0_expected, ']');
        private static readonly string s_closeBraceExpected = string.Format(FeaturesResources._0_expected, '}');
        private static readonly string s_openParenExpected = string.Format(FeaturesResources._0_expected, '(');
        private static readonly string s_closeParenExpected = string.Format(FeaturesResources._0_expected, ')');
        private static readonly string s_commaExpected = string.Format(FeaturesResources._0_expected, ',');

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
        public static JsonTree? TryParse(VirtualCharSequence text, JsonOptions options)
        {
            try
            {
                if (text.IsDefaultOrEmpty)
                    return null;

                return new JsonParser(text).ParseTree(options);
            }
            catch (InsufficientExecutionStackException)
            {
                return null;
            }
        }

        private JsonTree ParseTree(JsonOptions options)
        {
            var arraySequence = this.ParseSequence();
            Debug.Assert(_lexer.Position == _lexer.Text.Length);
            Debug.Assert(_currentToken.Kind == JsonKind.EndOfFile);

            var root = new JsonCompilationUnit(arraySequence, _currentToken);

            // There are three forms of diagnostics we can detect.  The first were generated directly when parsing and
            // relate to unknown tokens encountered or tokens that were needed but not found.  The second relates to a
            // set of grammar check rules that apply to both strict and non-strict json.  The last is the specific
            // strict/loose checks we perform.  We look for all three forms, but only report the first issue we found.
            // We want to avoid reporting a ton of cascaded errors.
            var diagnostic1 = GetFirstDiagnostic(root);
            var diagnostic2 = CheckTopLevel(_lexer.Text, root);
            var diagnostic3 = options.HasFlag(JsonOptions.Strict)
                ? StrictSyntaxChecker.CheckRootSyntax(root, options)
                : JsonNetSyntaxChecker.CheckSyntax(root);

            var diagnostic = Earliest(Earliest(diagnostic1, diagnostic2), diagnostic3);

            return new JsonTree(_lexer.Text, root, diagnostic == null
                ? ImmutableArray<EmbeddedDiagnostic>.Empty
                : ImmutableArray.Create(diagnostic.Value));
        }

        private static EmbeddedDiagnostic? Earliest(EmbeddedDiagnostic? d1, EmbeddedDiagnostic? d2)
        {
            if (d1 == null)
                return d2;

            if (d2 == null)
                return d1;

            return d1.Value.Span.Start <= d2.Value.Span.Start ? d1 : d2;
        }

        /// <summary>
        /// Checks for errors in json for both json.net and strict mode.
        /// </summary>
        private static EmbeddedDiagnostic? CheckTopLevel(
            VirtualCharSequence text, JsonCompilationUnit compilationUnit)
        {
            var sequence = compilationUnit.Sequence;
            if (sequence.IsEmpty)
            {
                // json is not allowed to be just whitespace.
                //
                // Note: we always have at least some content (either real nodes in the tree) or trivia on the EOF token
                // as we only parse when we have a non-empty sequence of virtual chars to begin with.
                if (text.Length > 0 &&
                    compilationUnit.EndOfFileToken.LeadingTrivia.All(
                        t => t.Kind is JsonKind.WhitespaceTrivia or JsonKind.EndOfLineTrivia))
                {
                    return new EmbeddedDiagnostic(FeaturesResources.Syntax_error, GetSpan(text));
                }

                return null;
            }
            else if (sequence.Length >= 2)
            {
                // the top level can't have more than one actual value.
                var firstToken = GetFirstToken(sequence[1]);
                return new EmbeddedDiagnostic(
                    string.Format(FeaturesResources._0_unexpected, firstToken.VirtualChars[0]),
                    firstToken.GetSpan());
            }
            else
            {
                var child = sequence.Single();

                // Commas should never show up in the top level sequence.
                if (child.Kind == JsonKind.CommaValue)
                {
                    var emptyValue = (JsonCommaValueNode)child;
                    return new EmbeddedDiagnostic(
                        string.Format(FeaturesResources._0_unexpected, ','),
                        emptyValue.CommaToken.GetSpan());
                }
                else if (child.Kind == JsonKind.Property)
                {
                    var propertyValue = (JsonPropertyNode)child;
                    return new EmbeddedDiagnostic(
                        string.Format(FeaturesResources._0_unexpected, ':'),
                        propertyValue.ColonToken.GetSpan());
                }

                return CheckSyntax(child);
            }

            static EmbeddedDiagnostic? CheckSyntax(JsonNode node)
            {
                var diagnostic = node.Kind switch
                {
                    JsonKind.Array => CheckArray((JsonArrayNode)node),
                    JsonKind.Object => CheckObject((JsonObjectNode)node),
                    _ => null,
                };

                return Earliest(diagnostic, CheckChildren(node));
            }

            static EmbeddedDiagnostic? CheckChildren(JsonNode node)
            {
                foreach (var child in node)
                {
                    if (child.IsNode)
                    {
                        var diagnostic = CheckSyntax(child.Node);
                        if (diagnostic != null)
                            return diagnostic;
                    }
                }

                return null;
            }

            static EmbeddedDiagnostic? CheckArray(JsonArrayNode node)
            {
                foreach (var child in node.Sequence)
                {
                    if (child.Kind == JsonKind.Property)
                    {
                        return new EmbeddedDiagnostic(
                            FeaturesResources.Properties_not_allowed_in_an_array,
                            ((JsonPropertyNode)child).ColonToken.GetSpan());
                    }
                }

                return null;
            }

            static EmbeddedDiagnostic? CheckObject(JsonObjectNode node)
            {
                foreach (var child in node.Sequence)
                {
                    if (child is JsonLiteralNode { LiteralToken.Kind: JsonKind.StringToken })
                    {
                        return new EmbeddedDiagnostic(
                            FeaturesResources.Property_name_must_be_followed_by_a_colon,
                            child.GetSpan());
                    }
                }

                return null;
            }
        }

        private static JsonToken GetFirstToken(JsonNodeOrToken nodeOrToken)
            => nodeOrToken.IsNode ? GetFirstToken(nodeOrToken.Node.ChildAt(0)) : nodeOrToken.Token;

        private static EmbeddedDiagnostic? GetFirstDiagnostic(JsonNode node)
        {
            foreach (var child in node)
            {
                var diagnostic = GetFirstDiagnostic(child);
                if (diagnostic != null)
                    return diagnostic;
            }

            return null;
        }

        private static EmbeddedDiagnostic? GetFirstDiagnostic(JsonNodeOrToken child)
            => child.IsNode
                ? GetFirstDiagnostic(child.Node)
                : GetFirstDiagnostic(child.Token);

        private static EmbeddedDiagnostic? GetFirstDiagnostic(JsonToken token)
            => GetFirstDiagnostic(token.LeadingTrivia) ?? token.Diagnostics.FirstOrNull() ?? GetFirstDiagnostic(token.TrailingTrivia);

        private static EmbeddedDiagnostic? GetFirstDiagnostic(ImmutableArray<JsonTrivia> list)
        {
            foreach (var trivia in list)
            {
                var diagnostic = trivia.Diagnostics.FirstOrNull();
                if (diagnostic != null)
                    return diagnostic;
            }

            return null;
        }

        private ImmutableArray<JsonValueNode> ParseSequence()
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

        private ImmutableArray<JsonValueNode> ParseSequenceWorker()
        {
            using var _ = ArrayBuilder<JsonValueNode>.GetInstance(out var result);

            while (ShouldConsumeSequenceElement())
                result.Add(ParseValue());

            return result.ToImmutable();
        }

        private JsonSeparatedList ParseCommaSeparatedSequence()
        {
            try
            {
                _recursionDepth++;
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
                return ParseCommaSeparatedSequenceWorker();
            }
            finally
            {
                _recursionDepth--;
            }
        }

        private JsonSeparatedList ParseCommaSeparatedSequenceWorker()
        {
            using var _ = ArrayBuilder<JsonNodeOrToken>.GetInstance(out var result);
            var allProperties = true;
            while (ShouldConsumeSequenceElement())
            {
                var value = ParseValue();
                allProperties = allProperties && value.Kind == JsonKind.Property;
                result.Add(value);

                // Try to consume a comma.  If we don't see one, consume an empty one as a placeholder. Create a
                // diagnostic message depending on if we've seen only properties before this point.  If not, don't give
                // a message about a missing comma.  Instead, we'll give a specific message that we didn't get a
                // property when we expected one.
                if (ShouldConsumeSequenceElement())
                    result.Add(ConsumeToken(JsonKind.CommaToken, allProperties ? s_commaExpected : null));
            }

            return new JsonSeparatedList(result.ToImmutable());
        }

        private bool ShouldConsumeSequenceElement()
            => _currentToken.Kind switch
            {
                JsonKind.EndOfFile => false,
                JsonKind.CloseBraceToken => !_inObject,
                JsonKind.CloseBracketToken => !_inArray,
                JsonKind.CloseParenToken => !_inConstructor,
                _ => true
            };

        private JsonValueNode ParseValue()
            => _currentToken.Kind switch
            {
                JsonKind.OpenBraceToken => ParseObject(),
                JsonKind.OpenBracketToken => ParseArray(),
                JsonKind.CommaToken => ParseCommaValue(),
                _ => ParseLiteralOrPropertyOrConstructor(),
            };

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

            // Kind could be anything else we might have parsed as a value (for example, an integer/boolean literal).
            if (stringLiteralOrText.Kind != JsonKind.StringToken)
                stringLiteralOrText = stringLiteralOrText.With(kind: JsonKind.TextToken);

            var colonToken = ConsumeCurrentToken();

            // Newtonsoft allows "{ a: , }" as a legal property. In that case, synthesize a missing value and allow the
            // comma to be parsed as the next value in the sequence.  The strict pass will error if it sees this missing
            // comma-value as the value of a property.
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
                        FeaturesResources.Missing_property_value,
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
                        FeaturesResources.Nested_properties_not_allowed,
                        nestedProperty.ColonToken.GetSpan())),
                    nestedProperty.Value);
            }

            return new JsonPropertyNode(stringLiteralOrText, colonToken, value);
        }

        private JsonValueNode ParseLiteralOrPropertyOrConstructor()
        {
            var textToken = ConsumeCurrentToken();
            return _currentToken.Kind == JsonKind.ColonToken
                ? ParseProperty(textToken)
                : ParseLiteralOrTextOrConstructor(textToken);
        }

        private JsonValueNode ParseLiteralOrTextOrConstructor(JsonToken token)
        {
            if (token.Kind == JsonKind.StringToken)
                return new JsonLiteralNode(token);

            // Look for constructors (a json.net extension).  We'll report them as an error
            // in strict model.
            if (Matches(token, "new"))
                return ParseConstructor(token);

            // Check for certain literal values.  Some of these (like NaN) are json.net only.
            // We'll check for these later in the strict-mode pass.
            Debug.Assert(token.VirtualChars.Length > 0);
            if (TryMatch(token, "NaN", JsonKind.NaNLiteralToken, out var newKind) ||
                TryMatch(token, "null", JsonKind.NullLiteralToken, out newKind) ||
                TryMatch(token, "true", JsonKind.TrueLiteralToken, out newKind) ||
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
                return new JsonLiteralNode(token.With(kind: JsonKind.NumberToken));

            return new JsonTextNode(
                token.With(kind: JsonKind.TextToken).AddDiagnosticIfNone(new EmbeddedDiagnostic(
                    string.Format(FeaturesResources._0_unexpected, firstChar.ToString()),
                    firstChar.Span)));
        }

        private JsonConstructorNode ParseConstructor(JsonToken token)
        {
            var savedInConstructor = _inConstructor;
            _inConstructor = true;

            var result = new JsonConstructorNode(
                token.With(kind: JsonKind.NewKeyword),
                ConsumeToken(JsonKind.TextToken, FeaturesResources.Name_expected),
                ConsumeToken(JsonKind.OpenParenToken, s_openParenExpected),
                ParseSequence(),
                ConsumeToken(JsonKind.CloseParenToken, s_closeParenExpected));

            _inConstructor = savedInConstructor;
            return result;
        }

        private static bool TryMatch(JsonToken token, string val, JsonKind kind, out JsonKind newKind)
        {
            if (Matches(token, val))
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
            => ch.Value is >= '0' and <= '9';

        private JsonCommaValueNode ParseCommaValue()
            => new(ConsumeCurrentToken());

        private JsonArrayNode ParseArray()
        {
            var savedInArray = _inArray;
            _inArray = true;

            var result = new JsonArrayNode(
                ConsumeCurrentToken(),
                ParseSequence(),
                ConsumeToken(JsonKind.CloseBracketToken, s_closeBracketExpected));

            _inArray = savedInArray;
            return result;
        }

        private JsonObjectNode ParseObject()
        {
            var savedInObject = _inObject;
            _inObject = true;

            var result = new JsonObjectNode(
                ConsumeCurrentToken(),
                ParseCommaSeparatedSequence(),
                ConsumeToken(JsonKind.CloseBraceToken, s_closeBraceExpected));

            _inObject = savedInObject;
            return result;
        }

        private JsonToken ConsumeToken(JsonKind kind, string? error)
        {
            if (_currentToken.Kind == kind)
                return ConsumeCurrentToken();

            var result = CreateMissingToken(kind);
            if (error == null)
                return result;

            return result.AddDiagnosticIfNone(new EmbeddedDiagnostic(error, GetTokenStartPositionSpan(_currentToken)));
        }

        private TextSpan GetTokenStartPositionSpan(JsonToken token)
            => token.Kind == JsonKind.EndOfFile
                ? new TextSpan(_lexer.Text.Last().Span.End, 0)
                : new TextSpan(token.VirtualChars[0].Span.Start, 0);
    }
}
