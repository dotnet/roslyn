// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Json
{
    using static EmbeddedSyntaxHelpers;

    using JsonToken = EmbeddedSyntaxToken<JsonKind>;
    using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;

    internal partial struct JsonParser
    {
        private struct StrictSyntaxChecker
        {
            private EmbeddedDiagnostic? CheckChildren(JsonNode node)
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

            private EmbeddedDiagnostic? CheckToken(JsonToken token)
                => CheckTrivia(token.LeadingTrivia) ?? CheckTrivia(token.TrailingTrivia);

            private EmbeddedDiagnostic? CheckTrivia(ImmutableArray<JsonTrivia> triviaList)
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

            private EmbeddedDiagnostic? CheckTrivia(JsonTrivia trivia)
            {
                switch (trivia.Kind)
                {
                    case JsonKind.MultiLineCommentTrivia:
                    case JsonKind.SingleLineCommentTrivia:
                        return new EmbeddedDiagnostic(
                            WorkspacesResources.Comments_not_allowed,
                            GetSpan(trivia.VirtualChars));
                    case JsonKind.WhitespaceTrivia:
                        return CheckWhitespace(trivia);
                }

                return null;
            }

            private EmbeddedDiagnostic? CheckWhitespace(JsonTrivia trivia)
            {
                foreach (var ch in trivia.VirtualChars)
                {
                    switch (ch)
                    {
                        case ' ': case '\t':
                            break;

                        default:
                            return new EmbeddedDiagnostic(
                                WorkspacesResources.Illegal_whitespace_character,
                                ch.Span);
                    }
                }

                return null;
            }

            public EmbeddedDiagnostic? CheckSyntax(JsonNode node)
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

            private EmbeddedDiagnostic? CheckObject(JsonObjectNode node)
            {
                var sequence = node.Sequence;
                foreach (var child in sequence)
                {
                    var childNode = child.Node;
                    if (childNode.Kind != JsonKind.Property && childNode.Kind != JsonKind.EmptyValue)
                    {
                        return new EmbeddedDiagnostic(
                            WorkspacesResources.Only_properties_allowed_in_an_object,
                            GetFirstToken(childNode).GetSpan());
                    }
                }

                return CheckProperSeparation(sequence) ?? CheckChildren(node);
            }

            private EmbeddedDiagnostic? CheckArray(JsonArrayNode node)
            {
                foreach (var child in node.Sequence)
                {
                    var childNode = child.Node;
                    if (childNode.Kind == JsonKind.Property)
                    {
                        return new EmbeddedDiagnostic(
                            WorkspacesResources.Properties_not_allowed_in_an_array,
                            ((JsonPropertyNode)childNode).ColonToken.GetSpan());
                    }
                }

                return CheckProperSeparation(node.Sequence) ?? CheckChildren(node);
            }

            private EmbeddedDiagnostic? CheckProperSeparation(JsonSequenceNode sequence)
            {
                for (int i = 0, n = sequence.ChildCount; i < n; i++)
                {
                    var child = sequence.ChildAt(i).Node;
                    if (i % 2 == 0)
                    {
                        if (child.Kind == JsonKind.EmptyValue)
                        {
                            return new EmbeddedDiagnostic(
                                string.Format(WorkspacesResources._0_unexpected, ","),
                                child.GetSpan());
                        }
                    }
                    else
                    {
                        if (child.Kind != JsonKind.EmptyValue)
                        {
                            return new EmbeddedDiagnostic(
                                string.Format(WorkspacesResources._0_expected, ","),
                                GetFirstToken(child).GetSpan());
                        }
                    }
                }

                if (sequence.ChildCount != 0 && sequence.ChildCount % 2 == 0)
                {
                    return new EmbeddedDiagnostic(
                        WorkspacesResources.Trailing_comma_not_allowed,
                        sequence.ChildAt(sequence.ChildCount - 1).Node.GetSpan());
                }

                return null;
            }

            private EmbeddedDiagnostic? CheckProperty(JsonPropertyNode node)
            {
                if (node.NameToken.Kind != JsonKind.StringToken)
                {
                    return new EmbeddedDiagnostic(
                        WorkspacesResources.Property_name_must_be_a_string,
                        node.NameToken.GetSpan());
                }

                if (node.Value.Kind == JsonKind.EmptyValue)
                {
                    return new EmbeddedDiagnostic(
                        WorkspacesResources.Value_required,
                        new TextSpan(node.ColonToken.VirtualChars[0].Span.End, 0));
                }

                return CheckString(node.NameToken) ?? CheckChildren(node);
            }

            private EmbeddedDiagnostic? CheckLiteral(JsonLiteralNode node)
            {
                switch (node.LiteralToken.Kind)
                {
                    case JsonKind.NaNLiteralToken:
                    case JsonKind.InfinityLiteralToken:
                    case JsonKind.UndefinedLiteralToken:
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

            private EmbeddedDiagnostic? CheckNumber(JsonToken literalToken)
            {
                var literalText = literalToken.VirtualChars.CreateString();
                if (!s_validNumberRegex.IsMatch(literalText))
                {
                    return new EmbeddedDiagnostic(
                        WorkspacesResources.Invalid_number,
                        literalToken.GetSpan());
                }

                if (!double.TryParse(literalText, out var val) ||
                    double.IsNaN(val) ||
                    double.IsInfinity(val))
                {
                    return new EmbeddedDiagnostic(
                        WorkspacesResources.Invalid_number,
                        literalToken.GetSpan());
                }

                return CheckToken(literalToken);
            }

            private EmbeddedDiagnostic? CheckString(JsonToken literalToken)
            {
                var chars = literalToken.VirtualChars;
                if (chars[0].Char == '\'')
                {
                    return new EmbeddedDiagnostic(
                        WorkspacesResources.Strings_must_start_with_double_quote_not_single_quote,
                        chars[0].Span);
                }

                for (int i = 1, n = chars.Length - 1; i < n; i++)
                {
                    if (chars[i].Char < ' ')
                    {
                        return new EmbeddedDiagnostic(
                            WorkspacesResources.Illegal_string_character,
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
                                WorkspacesResources.Invalid_escape_sequence,
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

            private EmbeddedDiagnostic? InvalidLiteral(JsonToken literalToken)
            {
                return new EmbeddedDiagnostic(
                    string.Format(WorkspacesResources._0_literal_not_allowed, literalToken.VirtualChars.CreateString()),
                    literalToken.GetSpan());
            }

            private EmbeddedDiagnostic? CheckNegativeLiteral(JsonNegativeLiteralNode node)
            {
                return new EmbeddedDiagnostic(
                    string.Format(WorkspacesResources._0_literal_not_allowed, "-Infinity"),
                    node.GetSpan());
            }

            private EmbeddedDiagnostic? CheckConstructor(JsonConstructorNode node)
            {
                return new EmbeddedDiagnostic(
                    WorkspacesResources.Constructors_not_allowed,
                    node.NewKeyword.GetSpan());
            }
        }
    }
}
