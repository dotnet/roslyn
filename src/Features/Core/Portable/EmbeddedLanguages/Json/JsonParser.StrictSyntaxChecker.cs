// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json
{
    using static EmbeddedSyntaxHelpers;

    using JsonToken = EmbeddedSyntaxToken<JsonKind>;
    using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;

    internal partial struct JsonParser
    {
        /// <summary>
        /// Checks the superset-tree for constructs that aren't allowed in strict rfc8259
        /// (https://tools.ietf.org/html/rfc8259) mode.
        /// </summary>
        private static class StrictSyntaxChecker
        {
            private static EmbeddedDiagnostic? CheckChildren(JsonNode node)
            {
                foreach (var child in node)
                {
                    var diagnostic = child.IsNode ? CheckSyntax(child.Node) : CheckToken(child.Token);
                    if (diagnostic != null)
                    {
                        return diagnostic;
                    }
                }

                return null;
            }

            private static EmbeddedDiagnostic? CheckToken(JsonToken token)
                => CheckTrivia(token.LeadingTrivia) ?? CheckTrivia(token.TrailingTrivia);

            private static EmbeddedDiagnostic? CheckTrivia(ImmutableArray<JsonTrivia> triviaList)
            {
                foreach (var trivia in triviaList)
                {
                    var diagnostic = CheckTrivia(trivia);
                    if (diagnostic != null)
                    {
                        return diagnostic;
                    }
                }

                return null;
            }

            private static EmbeddedDiagnostic? CheckTrivia(JsonTrivia trivia)
            {
                switch (trivia.Kind)
                {
                    case JsonKind.MultiLineCommentTrivia:
                    case JsonKind.SingleLineCommentTrivia:
                        // Strict mode doesn't allow comments at all.
                        return new EmbeddedDiagnostic(
                            FeaturesResources.Comments_not_allowed,
                            GetSpan(trivia.VirtualChars));
                    case JsonKind.WhitespaceTrivia:
                        return CheckWhitespace(trivia);
                }

                return null;
            }

            private static EmbeddedDiagnostic? CheckWhitespace(JsonTrivia trivia)
            {
                foreach (var ch in trivia.VirtualChars)
                {
                    switch (ch.Value)
                    {
                        case ' ':
                        case '\t':
                            break;

                        default:
                            // Strict mode only allows spaces and horizontal tabs.  Everything else
                            // is illegal.
                            return new EmbeddedDiagnostic(
                                FeaturesResources.Illegal_whitespace_character,
                                ch.Span);
                    }
                }

                return null;
            }

            public static EmbeddedDiagnostic? CheckSyntax(JsonNode node)
            {
                switch (node.Kind)
                {
                    case JsonKind.Constructor: return CheckConstructor((JsonConstructorNode)node);
                    case JsonKind.Literal: return CheckLiteral((JsonLiteralNode)node);
                    case JsonKind.NegativeLiteral: return CheckNegativeLiteral((JsonNegativeLiteralNode)node);
                    case JsonKind.Property: return CheckProperty((JsonPropertyNode)node);
                    case JsonKind.Array: return CheckArray((JsonArrayNode)node);
                    case JsonKind.Object: return CheckObject((JsonObjectNode)node);
                }

                return CheckChildren(node);
            }

            private static EmbeddedDiagnostic? CheckObject(JsonObjectNode node)
            {
                var sequence = node.Sequence;
                foreach (var child in sequence)
                {
                    if (child.Kind != JsonKind.Property && child.Kind != JsonKind.CommaValue)
                    {
                        return new EmbeddedDiagnostic(
                            FeaturesResources.Only_properties_allowed_in_an_object,
                            GetFirstToken(child).GetSpan());
                    }
                }

                return CheckProperSeparation(sequence) ?? CheckChildren(node);
            }

            private static EmbeddedDiagnostic? CheckArray(JsonArrayNode node)
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

                return CheckProperSeparation(node.Sequence) ?? CheckChildren(node);
            }

            private static EmbeddedDiagnostic? CheckProperSeparation(ImmutableArray<JsonValueNode> sequence)
            {
                // Ensure that this sequence is actually a separated list.
                for (int i = 0, n = sequence.Length; i < n; i++)
                {
                    var child = sequence[i];
                    if (i % 2 == 0)
                    {
                        if (child.Kind == JsonKind.CommaValue)
                        {
                            return new EmbeddedDiagnostic(
                                string.Format(FeaturesResources._0_unexpected, ","),
                                child.GetSpan());
                        }
                    }
                    else
                    {
                        if (child.Kind != JsonKind.CommaValue)
                        {
                            return new EmbeddedDiagnostic(
                                string.Format(FeaturesResources._0_expected, ","),
                                GetFirstToken(child).GetSpan());
                        }
                    }
                }

                if (sequence.Length != 0 &&
                    sequence.Length % 2 == 0)
                {
                    var lastChild = sequence[^1];
                    return new EmbeddedDiagnostic(
                        FeaturesResources.Trailing_comma_not_allowed,
                        lastChild.GetSpan());
                }

                return null;
            }

            private static EmbeddedDiagnostic? CheckProperty(JsonPropertyNode node)
            {
                if (node.NameToken.Kind != JsonKind.StringToken)
                {
                    return new EmbeddedDiagnostic(
                        FeaturesResources.Property_name_must_be_a_string,
                        node.NameToken.GetSpan());
                }

                if (node.Value.Kind == JsonKind.CommaValue)
                {
                    return new EmbeddedDiagnostic(
                        FeaturesResources.Value_required,
                        new TextSpan(node.ColonToken.VirtualChars[0].Span.End, 0));
                }

                return CheckString(node.NameToken) ?? CheckChildren(node);
            }

            private static EmbeddedDiagnostic? CheckLiteral(JsonLiteralNode node)
            {
                switch (node.LiteralToken.Kind)
                {
                    case JsonKind.NaNLiteralToken:
                    case JsonKind.InfinityLiteralToken:
                    case JsonKind.UndefinedLiteralToken:
                        // These are all json.net extensions.  Disallow them all.
                        return InvalidLiteral(node.LiteralToken);
                    case JsonKind.NumberToken:
                        return CheckNumber(node.LiteralToken);
                    case JsonKind.StringToken:
                        return CheckString(node.LiteralToken);
                }

                return CheckChildren(node);
            }

            /*
               From: https://tools.ietf.org/html/rfc8259
             
               The representation of numbers is similar to that used in most
               programming languages.  A number is represented in base 10 using
               decimal digits.  It contains an integer component that may be
               prefixed with an optional minus sign, which may be followed by a
               fraction part and/or an exponent part.  Leading zeros are not
               allowed.

               A fraction part is a decimal point followed by one or more digits.

               An exponent part begins with the letter E in uppercase or lowercase,
               which may be followed by a plus or minus sign.  The E and optional
               sign are followed by one or more digits.

               Numeric values that cannot be represented in the grammar below (such
               as Infinity and NaN) are not permitted.

                  number = [ minus ] int [ frac ] [ exp ]
                  decimal-point = %x2E       ; .
                  digit1-9 = %x31-39         ; 1-9
                  e = %x65 / %x45            ; e E

                  exp = e [ minus / plus ] 1*DIGIT
                  frac = decimal-point 1*DIGIT
                  int = zero / ( digit1-9 *DIGIT )
                  minus = %x2D               ; -
                  plus = %x2B                ; +
                  zero = %x30                ; 0
            */

            private static readonly Regex s_validNumberRegex =
                new Regex(
@"^
-?                 # [ minus ]
(0|([1-9][0-9]*))  # int
(\.[0-9]+)?        # [ frac ]
([eE][-+]?[0-9]+)? # [ exp ]
$",
                    RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

            private static EmbeddedDiagnostic? CheckNumber(JsonToken literalToken)
            {
                var literalText = literalToken.VirtualChars.CreateString();
                if (!s_validNumberRegex.IsMatch(literalText))
                {
                    return new EmbeddedDiagnostic(
                        FeaturesResources.Invalid_number,
                        literalToken.GetSpan());
                }

                return CheckToken(literalToken);
            }

            private static EmbeddedDiagnostic? CheckString(JsonToken literalToken)
            {
                var chars = literalToken.VirtualChars;
                if (chars[0] == '\'')
                {
                    return new EmbeddedDiagnostic(
                        FeaturesResources.Strings_must_start_with_double_quote_not_single_quote,
                        chars[0].Span);
                }

                for (int i = 1, n = chars.Length - 1; i < n; i++)
                {
                    if (chars[i] < ' ')
                    {
                        return new EmbeddedDiagnostic(
                            FeaturesResources.Illegal_string_character,
                            chars[i].Span);
                    }
                }

                // Lexer allows \' as that's ok in json.net.  Check and block that here.
                for (int i = 1, n = chars.Length - 1; i < n;)
                {
                    if (chars[i] == '\\')
                    {
                        if (chars[i + 1] == '\'')
                        {
                            return new EmbeddedDiagnostic(
                                FeaturesResources.Invalid_escape_sequence,
                                TextSpan.FromBounds(chars[i].Span.Start, chars[i + 1].Span.End));
                        }

                        // Legal escape.  just jump forward past it.  Note, this works for simple
                        // escape and unicode \uXXXX escapes.
                        i += 2;
                        continue;
                    }

                    i++;
                }

                return CheckToken(literalToken);
            }

            private static EmbeddedDiagnostic? InvalidLiteral(JsonToken literalToken)
            {
                return new EmbeddedDiagnostic(
                    string.Format(FeaturesResources._0_literal_not_allowed, literalToken.VirtualChars.CreateString()),
                    literalToken.GetSpan());
            }

            private static EmbeddedDiagnostic? CheckNegativeLiteral(JsonNegativeLiteralNode node)
            {
                return new EmbeddedDiagnostic(
                    string.Format(FeaturesResources._0_literal_not_allowed, "-Infinity"),
                    node.GetSpan());
            }

            private static EmbeddedDiagnostic? CheckConstructor(JsonConstructorNode node)
            {
                return new EmbeddedDiagnostic(
                    FeaturesResources.Constructors_not_allowed,
                    node.NewKeyword.GetSpan());
            }
        }
    }
}
