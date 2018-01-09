// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Json
{
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis.Text;
    using Microsoft.CodeAnalysis.VirtualChars;
    using static JsonHelpers;

    internal partial struct JsonParser
    {
        private struct StrictSyntaxChecker
        {
            private JsonDiagnostic? CheckChildren(JsonNode node)
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

            private JsonDiagnostic? CheckToken(JsonToken token)
                => CheckTrivia(token.LeadingTrivia) ?? CheckTrivia(token.TrailingTrivia);

            private JsonDiagnostic? CheckTrivia(ImmutableArray<JsonTrivia> triviaList)
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

            private JsonDiagnostic? CheckTrivia(JsonTrivia trivia)
            {
                switch (trivia.Kind)
                {
                    case JsonKind.MultiLineCommentTrivia:
                    case JsonKind.SingleLineCommentTrivia:
                        return new JsonDiagnostic(
                            WorkspacesResources.Comments_not_allowed,
                            GetSpan(trivia.VirtualChars));
                    case JsonKind.WhitespaceTrivia:
                        return CheckWhitespace(trivia);
                }

                return null;
            }

            private JsonDiagnostic? CheckWhitespace(JsonTrivia trivia)
            {
                foreach (var ch in trivia.VirtualChars)
                {
                    switch (ch)
                    {
                        case ' ': case '\t':
                            break;

                        default:
                            return new JsonDiagnostic(
                                WorkspacesResources.Illegal_whitespace_character,
                                ch.Span);
                    }
                }

                return null;
            }

            public JsonDiagnostic? CheckSyntax(JsonNode node)
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

            private JsonDiagnostic? CheckObject(JsonObjectNode node)
            {
                var sequence = node.Sequence;
                foreach (var child in sequence)
                {
                    var childNode = child.Node;
                    if (childNode.Kind != JsonKind.Property && childNode.Kind != JsonKind.EmptyValue)
                    {
                        return new JsonDiagnostic(
                            WorkspacesResources.Only_properties_allowed_in_an_object,
                            GetSpan(GetFirstToken(childNode)));
                    }
                }

                return CheckProperSeparation(sequence) ?? CheckChildren(node);
            }

            private JsonDiagnostic? CheckArray(JsonArrayNode node)
            {
                foreach (var child in node.Sequence)
                {
                    var childNode = child.Node;
                    if (childNode.Kind == JsonKind.Property)
                    {
                        return new JsonDiagnostic(
                            WorkspacesResources.Properties_not_allowed_in_an_array,
                            GetSpan(((JsonPropertyNode)childNode).ColonToken));
                    }
                }

                return CheckProperSeparation(node.Sequence) ?? CheckChildren(node);
            }

            private JsonDiagnostic? CheckProperSeparation(JsonSequenceNode sequence)
            {
                for (int i = 0, n = sequence.ChildCount; i < n; i++)
                {
                    var child = sequence.ChildAt(i).Node;
                    if (i % 2 == 0)
                    {
                        if (child.Kind == JsonKind.EmptyValue)
                        {
                            return new JsonDiagnostic(
                                string.Format(WorkspacesResources._0_unexpected, ","),
                                GetSpan(child));
                        }
                    }
                    else
                    {
                        if (child.Kind != JsonKind.EmptyValue)
                        {
                            return new JsonDiagnostic(
                                string.Format(WorkspacesResources._0_expected, ","),
                                GetSpan(GetFirstToken(child)));
                        }
                    }
                }

                if (sequence.ChildCount != 0 && sequence.ChildCount % 2 == 0)
                {
                    return new JsonDiagnostic(
                        WorkspacesResources.Trailing_comma_not_allowed,
                        GetSpan(sequence.ChildAt(sequence.ChildCount - 1).Node));
                }

                return null;
            }

            private JsonDiagnostic? CheckProperty(JsonPropertyNode node)
            {
                if (node.NameToken.Kind != JsonKind.StringToken)
                {
                    return new JsonDiagnostic(
                        WorkspacesResources.Property_name_must_be_a_string,
                        GetSpan(node.NameToken));
                }

                if (node.Value.Kind == JsonKind.EmptyValue)
                {
                    return new JsonDiagnostic(
                        WorkspacesResources.Value_required,
                        new TextSpan(node.ColonToken.VirtualChars[0].Span.End, 0));
                }

                return CheckString(node.NameToken) ?? CheckChildren(node);
            }

            private JsonDiagnostic? CheckLiteral(JsonLiteralNode node)
            {
                switch (node.LiteralToken.Kind)
                {
                    case JsonKind.UndefinedLiteralToken:
                        return InvalidLiteral(node.LiteralToken);
                    case JsonKind.NumberToken:
                        return CheckNumber(node.LiteralToken);
                    case JsonKind.StringToken:
                        return CheckString(node.LiteralToken);
                }

                return CheckChildren(node);
            }

            private static readonly Regex s_validNumberRegex =
                new Regex(@"-?[0-9]*(\.[0-9]*)?([eE][-+]?[0-9]*)?", RegexOptions.Compiled);

            private JsonDiagnostic? CheckNumber(JsonToken literalToken)
            {
                var literalText = literalToken.VirtualChars.CreateString();
                if (!s_validNumberRegex.IsMatch(literalText))
                {
                    return new JsonDiagnostic(
                        WorkspacesResources.Invalid_number,
                        GetSpan(literalToken));
                }

                if (!double.TryParse(literalText, out var val) ||
                    double.IsNaN(val) ||
                    double.IsInfinity(val))
                {
                    return new JsonDiagnostic(
                        WorkspacesResources.Invalid_number,
                        GetSpan(literalToken));
                }

                return CheckToken(literalToken);
            }

            private JsonDiagnostic? CheckString(JsonToken literalToken)
            {
                var chars = literalToken.VirtualChars;
                if (chars[0].Char == '\'')
                {
                    return new JsonDiagnostic(
                        WorkspacesResources.Strings_must_start_with_double_quote_not_single_quote,
                        chars[0].Span);
                }

                for (int i = 1, n = chars.Length - 1; i < n; i++)
                {
                    if (chars[i].Char < ' ')
                    {
                        return new JsonDiagnostic(
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
                            return new JsonDiagnostic(
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

            private JsonDiagnostic? InvalidLiteral(JsonToken literalToken)
            {
                return new JsonDiagnostic(
                    string.Format(WorkspacesResources._0_literal_not_allowed, literalToken.VirtualChars.CreateString()),
                    GetSpan(literalToken));
            }

            private JsonDiagnostic? CheckNegativeLiteral(JsonNegativeLiteralNode node)
            {
                return null;
                //return new JsonDiagnostic(
                //    string.Format(WorkspacesResources._0_literal_not_allowed, "-Infinity"),
                //    GetSpan(node));
            }

            private JsonDiagnostic? CheckConstructor(JsonConstructorNode node)
            {
                return new JsonDiagnostic(
                    WorkspacesResources.Constructors_not_allowed,
                    GetSpan(node.NewKeyword));
            }
        }
    }
}
