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
        private partial struct JsonNetSyntaxChecker
        {
            public JsonDiagnostic? Check(ImmutableArray<VirtualChar> text, JsonCompilationUnit root)
                => CheckTopLevel(text, root) ?? CheckSyntax(root);

            private JsonDiagnostic? CheckTopLevel(
                ImmutableArray<VirtualChar> text, JsonCompilationUnit compilationUnit)
            {
                var arraySequence = compilationUnit.Sequence;
                if (arraySequence.ChildCount == 0)
                {
                    if (text.Length > 0 &&
                        compilationUnit.EndOfFileToken.LeadingTrivia.All(
                            t => t.Kind == JsonKind.WhitespaceTrivia || t.Kind == JsonKind.EndOfLineTrivia))
                    {
                        return new JsonDiagnostic(WorkspacesResources.Syntax_error, GetSpan(text));
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

                return null;
            }

            private JsonDiagnostic? CheckSyntax(JsonNode node)
            {
                switch (node.Kind)
                {
                    case JsonKind.Array: return CheckArray((JsonArrayNode)node);
                    case JsonKind.Object: return CheckObject((JsonObjectNode)node);
                    case JsonKind.Constructor: return CheckConstructor((JsonConstructorNode)node);
                    case JsonKind.Property: return CheckProperty((JsonPropertyNode)node);
                }

                return CheckChildren(node);
            }

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

                var diagnostic = CheckCommasBetweenSequenceElements(node.Sequence);
                return diagnostic ?? CheckChildren(node);
            }

            private JsonDiagnostic? CheckConstructor(JsonConstructorNode node)
                => CheckCommasBetweenSequenceElements(node.Sequence) ?? CheckChildren(node);

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
