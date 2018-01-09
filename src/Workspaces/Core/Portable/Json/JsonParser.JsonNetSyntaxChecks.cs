// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.VirtualChars;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Json
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using static JsonHelpers;

    internal partial struct JsonParser
    {
        private partial struct JsonNetSyntaxChecker
        {
            private JsonDiagnostic? CheckChildren(JsonNode node)
            {
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

            public JsonDiagnostic? CheckSyntax(JsonNode node)
            {
                switch (node.Kind)
                {
                    case JsonKind.Array: return CheckArray((JsonArrayNode)node);
                    case JsonKind.Object: return CheckObject((JsonObjectNode)node);
                    case JsonKind.Constructor: return CheckConstructor((JsonConstructorNode)node);
                    case JsonKind.Property: return CheckProperty((JsonPropertyNode)node);
                    case JsonKind.Literal: return CheckLiteral((JsonLiteralNode)node);
                    case JsonKind.NegativeLiteral: return CheckNegativeLiteral((JsonNegativeLiteralNode)node);
                }

                return CheckChildren(node);
            }

            private JsonDiagnostic? CheckLiteral(JsonLiteralNode node)
            {
                if (node.LiteralToken.Kind == JsonKind.NumberToken)
                {
                    return CheckNumber(node.LiteralToken);
                }

                return CheckChildren(node);
            }

            private JsonDiagnostic? CheckNegativeLiteral(JsonNegativeLiteralNode node)
            {
                if (node.LiteralToken.Kind == JsonKind.NumberToken)
                {
                    return CheckNumber(node.LiteralToken);
                }

                return CheckChildren(node);
            }

            private JsonDiagnostic? CheckNumber(JsonToken numberToken)
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

                var literalText = numberToken.VirtualChars.CreateString();
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

                var diagnostic = CheckCommasBetweenSequenceElements(node.Sequence);
                return diagnostic ?? CheckChildren(node);
            }

            private JsonDiagnostic? CheckConstructor(JsonConstructorNode node)
            {
                if (!IsValidConstructorName(node.NameToken))
                {
                    return new JsonDiagnostic(
                        WorkspacesResources.Invalid_constructor_name,
                        GetSpan(node.NameToken));
                }

                return CheckCommasBetweenSequenceElements(node.Sequence) ?? CheckChildren(node);
            }

            private static bool IsValidConstructorName(JsonToken nameToken)
            {
                foreach (var vc in nameToken.VirtualChars)
                {
                    if (!char.IsLetterOrDigit(vc.Char))
                    {
                        return false;
                    }
                }

                return true;
            }

            private JsonDiagnostic? CheckCommasBetweenSequenceElements(JsonSequenceNode node)
            {
                for (int i = 0, n = node.ChildCount - 1; i < n; i++)
                {
                    var child = node.ChildAt(i).Node;
                    if (child.Kind != JsonKind.EmptyValue)
                    {
                        var next = node.ChildAt(i + 1).Node;

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
                               WorkspacesResources.Only_properties_allowed_in_an_object,
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

                return CheckChildren(node);
            }

            private JsonDiagnostic? CheckProperty(JsonPropertyNode node)
            {
                if (node.NameToken.Kind != JsonKind.StringToken &&
                    !IsLegalPropertyNameText(node.NameToken))
                {
                    return new JsonDiagnostic(
                        WorkspacesResources.Invalid_property_name,
                        GetSpan(node.NameToken));
                }

                return CheckChildren(node);
            }

            private static bool IsLegalPropertyNameText(JsonToken textToken)
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

            private static bool IsLegalPropertyNameChar(char ch)
                => char.IsLetterOrDigit(ch) | ch == '_' || ch == '$';
        }
    }
}
