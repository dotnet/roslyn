// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json
{
    using static EmbeddedSyntaxHelpers;

    using JsonToken = EmbeddedSyntaxToken<JsonKind>;

    internal partial struct JsonParser
    {
        private static class JsonNetSyntaxChecker
        {
            public static EmbeddedDiagnostic? CheckSyntax(JsonNode node)
            {
                var diagnostic = node.Kind switch
                {
                    JsonKind.Array => CheckArray((JsonArrayNode)node),
                    JsonKind.Object => CheckObject((JsonObjectNode)node),
                    JsonKind.Constructor => CheckConstructor((JsonConstructorNode)node),
                    JsonKind.Property => CheckProperty((JsonPropertyNode)node),
                    JsonKind.Literal => CheckLiteral((JsonLiteralNode)node),
                    JsonKind.NegativeLiteral => CheckNegativeLiteral((JsonNegativeLiteralNode)node),
                    _ => null,
                };

                return Earliest(diagnostic, CheckChildren(node));

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
            }

            private static EmbeddedDiagnostic? CheckLiteral(JsonLiteralNode node)
                => node.LiteralToken.Kind == JsonKind.NumberToken
                    ? CheckNumber(node.LiteralToken)
                    : null;

            private static EmbeddedDiagnostic? CheckNegativeLiteral(JsonNegativeLiteralNode node)
                => node.LiteralToken.Kind == JsonKind.NumberToken
                    ? CheckNumber(node.LiteralToken)
                    : null;

            private static EmbeddedDiagnostic? CheckNumber(JsonToken numberToken)
            {
                // This code was effectively copied from:
                // https://github.com/JamesNK/Newtonsoft.Json/blob/993215529562866719689206e27e413013d4439c/Src/Newtonsoft.Json/JsonTextReader.cs#L1926
                // So as to match Newtonsoft.Json's behavior around number parsing.
                var chars = numberToken.VirtualChars;
                var firstChar = chars[0];

                var singleDigit = firstChar.IsDigit && chars.Length == 1;
                if (singleDigit)
                    return null;

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
                        return new EmbeddedDiagnostic(FeaturesResources.Invalid_number, GetSpan(chars));
                    }
                }
                else if (!double.TryParse(
                    literalText, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out _))
                {
                    return new EmbeddedDiagnostic(FeaturesResources.Invalid_number, GetSpan(chars));
                }

                return null;
            }

            private static EmbeddedDiagnostic? CheckArray(JsonArrayNode node)
                => CheckCommasBetweenSequenceElements(node.Sequence);

            private static EmbeddedDiagnostic? CheckConstructor(JsonConstructorNode node)
                => !IsValidConstructorName(node.NameToken)
                    ? new EmbeddedDiagnostic(FeaturesResources.Invalid_constructor_name, node.NameToken.GetSpan())
                    : CheckCommasBetweenSequenceElements(node.Sequence);

            private static bool IsValidConstructorName(JsonToken nameToken)
            {
                foreach (var vc in nameToken.VirtualChars)
                {
                    if (!vc.IsLetterOrDigit)
                        return false;
                }

                return true;
            }

            private static EmbeddedDiagnostic? CheckCommasBetweenSequenceElements(ImmutableArray<JsonValueNode> sequence)
            {
                // Json.net allows sequences of commas.  But after every non-comma value, you need
                // a comma.
                for (int i = 0, n = sequence.Length - 1; i < n; i++)
                {
                    var child = sequence[i];
                    var nextChild = sequence[i + 1];
                    if (child.Kind != JsonKind.CommaValue && nextChild.Kind != JsonKind.CommaValue)
                        return new EmbeddedDiagnostic(string.Format(FeaturesResources._0_expected, ','), GetFirstToken(nextChild).GetSpan());
                }

                return null;
            }

            private static EmbeddedDiagnostic? CheckObject(JsonObjectNode node)
            {
                foreach (var child in node.Sequence)
                {
                    if (child.Kind != JsonKind.Property)
                        return new EmbeddedDiagnostic(FeaturesResources.Only_properties_allowed_in_an_object, GetFirstToken(child).GetSpan());
                }

                return null;
            }

            private static EmbeddedDiagnostic? CheckProperty(JsonPropertyNode node)
                => node.NameToken.Kind != JsonKind.StringToken && !IsLegalPropertyNameText(node.NameToken)
                    ? new EmbeddedDiagnostic(FeaturesResources.Invalid_property_name, node.NameToken.GetSpan())
                    : null;

            private static bool IsLegalPropertyNameText(JsonToken textToken)
            {
                foreach (var ch in textToken.VirtualChars)
                {
                    if (!IsLegalPropertyNameChar(ch))
                        return false;
                }

                return true;
            }

            private static bool IsLegalPropertyNameChar(VirtualChar ch)
                => ch.IsLetterOrDigit || ch == '_' || ch == '$';
        }
    }
}
