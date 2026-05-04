// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;

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
        public static EmbeddedDiagnostic? CheckRootSyntax(JsonCompilationUnit node, JsonOptions options)
        {
            var allowComments = options.HasFlag(JsonOptions.Comments);
            var allowTrailingCommas = options.HasFlag(JsonOptions.TrailingCommas);
            return CheckSyntax(node, allowComments, allowTrailingCommas);
        }

        private static EmbeddedDiagnostic? CheckSyntax(
            JsonNode node, bool allowComments, bool allowTrailingCommas)
        {
            var diagnostic = node.Kind switch
            {
                JsonKind.Constructor => CheckConstructor((JsonConstructorNode)node),
                JsonKind.Literal => CheckLiteral((JsonLiteralNode)node, allowComments),
                JsonKind.NegativeLiteral => CheckNegativeLiteral((JsonNegativeLiteralNode)node),
                JsonKind.Property => CheckProperty((JsonPropertyNode)node, allowComments),
                JsonKind.Array => CheckArray((JsonArrayNode)node, allowTrailingCommas),
                JsonKind.Object => CheckObject((JsonObjectNode)node, allowTrailingCommas),
                _ => null,
            };

            return Earliest(diagnostic, CheckChildren(node));

            EmbeddedDiagnostic? CheckChildren(JsonNode node)
            {
                foreach (var child in node)
                {
                    var diagnostic = child.IsNode
                        ? CheckSyntax(child.Node, allowComments, allowTrailingCommas)
                        : CheckToken(child.Token, allowComments);
                    if (diagnostic != null)
                        return diagnostic;
                }

                return null;
            }
        }

        private static EmbeddedDiagnostic? CheckToken(JsonToken token, bool allowComments)
            => CheckTrivia(token.LeadingTrivia, allowComments) ?? CheckTrivia(token.TrailingTrivia, allowComments);

        private static EmbeddedDiagnostic? CheckTrivia(
            ImmutableArray<JsonTrivia> triviaList, bool allowComments)
        {
            foreach (var trivia in triviaList)
            {
                var diagnostic = CheckTrivia(trivia, allowComments);
                if (diagnostic != null)
                    return diagnostic;
            }

            return null;
        }

        private static EmbeddedDiagnostic? CheckTrivia(JsonTrivia trivia, bool allowComments)
            => trivia.Kind switch
            {
                // Strict mode doesn't allow comments at all.
                JsonKind.MultiLineCommentTrivia or JsonKind.SingleLineCommentTrivia when !allowComments
                    => new EmbeddedDiagnostic(FeaturesResources.Comments_not_allowed, GetSpan(trivia.VirtualChars)),
                JsonKind.WhitespaceTrivia => CheckWhitespace(trivia),
                _ => null,
            };

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
                        return new EmbeddedDiagnostic(FeaturesResources.Illegal_whitespace_character, ch.Span);
                }
            }

            return null;
        }

        private static EmbeddedDiagnostic? CheckObject(JsonObjectNode node, bool allowTrailingComma)
        {
            foreach (var child in node.Sequence)
            {
                if (child.Kind != JsonKind.Property)
                    return new EmbeddedDiagnostic(FeaturesResources.Only_properties_allowed_in_an_object, GetFirstToken(child).GetSpan());
            }

            if (!allowTrailingComma && node.Sequence.NodesAndTokens.Length != 0 && node.Sequence.NodesAndTokens.Length % 2 == 0)
                return new EmbeddedDiagnostic(FeaturesResources.Trailing_comma_not_allowed, node.Sequence.NodesAndTokens[^1].Token.GetSpan());

            return null;
        }

        private static EmbeddedDiagnostic? CheckArray(JsonArrayNode node, bool allowTrailingComma)
            => CheckProperSeparation(node.Sequence, allowTrailingComma);

        private static EmbeddedDiagnostic? CheckProperSeparation(
            ImmutableArray<JsonValueNode> sequence,
            bool allowTrailingComma)
        {
            // Ensure that this sequence is actually a separated list.
            for (int i = 0, n = sequence.Length; i < n; i++)
            {
                var child = sequence[i];
                if (i % 2 == 0)
                {
                    if (child.Kind == JsonKind.CommaValue)
                        return new EmbeddedDiagnostic(string.Format(FeaturesResources._0_unexpected, ","), child.GetSpan());
                }
                else
                {
                    if (child.Kind != JsonKind.CommaValue)
                        return new EmbeddedDiagnostic(string.Format(FeaturesResources._0_expected, ","), GetFirstToken(child).GetSpan());
                }
            }

            if (!allowTrailingComma && sequence.Length != 0 && sequence.Length % 2 == 0)
                return new EmbeddedDiagnostic(FeaturesResources.Trailing_comma_not_allowed, sequence[^1].GetSpan());

            return null;
        }

        private static EmbeddedDiagnostic? CheckProperty(JsonPropertyNode node, bool allowComments)
        {
            if (node.NameToken.Kind != JsonKind.StringToken)
                return new EmbeddedDiagnostic(FeaturesResources.Property_name_must_be_a_string, node.NameToken.GetSpan());

            if (node.Value.Kind == JsonKind.CommaValue)
                return new EmbeddedDiagnostic(FeaturesResources.Value_required, new TextSpan(node.ColonToken.VirtualChars[0].Span.End, 0));

            return CheckString(node.NameToken, allowComments);
        }

        private static EmbeddedDiagnostic? CheckLiteral(JsonLiteralNode node, bool allowComments)
            => node.LiteralToken.Kind switch
            {
                // These are all json.net extensions.  Disallow them all.
                JsonKind.NaNLiteralToken or JsonKind.InfinityLiteralToken or JsonKind.UndefinedLiteralToken
                    => InvalidLiteral(node.LiteralToken),
                JsonKind.NumberToken => CheckNumber(node.LiteralToken, allowComments),
                JsonKind.StringToken => CheckString(node.LiteralToken, allowComments),
                _ => null,
            };

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
            new(
@"^
-?                 # [ minus ]
(0|([1-9][0-9]*))  # int
(\.[0-9]+)?        # [ frac ]
([eE][-+]?[0-9]+)? # [ exp ]
$",
                RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private static EmbeddedDiagnostic? CheckNumber(JsonToken literalToken, bool allowComments)
        {
            var literalText = literalToken.VirtualChars.CreateString();
            return !s_validNumberRegex.IsMatch(literalText)
                ? new EmbeddedDiagnostic(FeaturesResources.Invalid_number, literalToken.GetSpan())
                : CheckToken(literalToken, allowComments);
        }

        private static EmbeddedDiagnostic? CheckString(JsonToken literalToken, bool allowComments)
        {
            var chars = literalToken.VirtualChars;
            if (chars[0] == '\'')
                return new EmbeddedDiagnostic(FeaturesResources.Strings_must_start_with_double_quote_not_single_quote, chars[0].Span);

            for (int i = 1, n = chars.Length - 1; i < n; i++)
            {
                if (chars[i] < ' ')
                    return new EmbeddedDiagnostic(FeaturesResources.Illegal_string_character, chars[i].Span);
            }

            // Lexer allows \' as that's ok in json.net.  Check and block that here.
            for (int i = 1, n = chars.Length - 1; i < n;)
            {
                if (chars[i] == '\\')
                {
                    if (chars[i + 1] == '\'')
                        return new EmbeddedDiagnostic(FeaturesResources.Invalid_escape_sequence, TextSpan.FromBounds(chars[i].Span.Start, chars[i + 1].Span.End));

                    // Legal escape.  just jump forward past it.  Note, this works for simple
                    // escape and unicode \uXXXX escapes.
                    i += 2;
                    continue;
                }

                i++;
            }

            return CheckToken(literalToken, allowComments);
        }

        private static EmbeddedDiagnostic? InvalidLiteral(JsonToken literalToken)
            => new(string.Format(FeaturesResources._0_literal_not_allowed, literalToken.VirtualChars.CreateString()), literalToken.GetSpan());

        private static EmbeddedDiagnostic? CheckNegativeLiteral(JsonNegativeLiteralNode node)
            => new(string.Format(FeaturesResources._0_literal_not_allowed, "-Infinity"), node.GetSpan());

        private static EmbeddedDiagnostic? CheckConstructor(JsonConstructorNode node)
            => new(FeaturesResources.Constructors_not_allowed, node.NewKeyword.GetSpan());
    }
}
