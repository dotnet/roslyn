// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EmbeddedLanguages.Json
{
    using JsonToken = EmbeddedSyntaxToken<JsonKind>;
    using JsonTrivia = EmbeddedSyntaxTrivia<JsonKind>;

    public partial class CSharpJsonParserTests
    {
        private readonly IVirtualCharService _service = CSharpVirtualCharService.Instance;
        private const string _statmentPrefix = "var v = ";

        private static SyntaxToken GetStringToken(string text)
        {
            var statement = _statmentPrefix + text;
            var parsedStatement = SyntaxFactory.ParseStatement(statement);
            var token = parsedStatement.DescendantTokens().ToArray()[3];
            Assert.True(token.Kind() == SyntaxKind.StringLiteralToken);

            return token;
        }

        private void Test(string stringText,
            string expected, string looseDiagnostics, string strictDiagnostics,
            bool runLooseTreeCheck = true, bool runLooseSubTreeCheck = true,
            bool runStrictTreeCheck = true, bool runStrictSubTreeCheck = true,
            [CallerMemberName] string _ = "")
        {
            if (runLooseSubTreeCheck || runLooseSubTreeCheck)
            {
                Test(stringText, strict: false, expected, looseDiagnostics, runLooseTreeCheck, runLooseSubTreeCheck);
            }

            if (runStrictTreeCheck || runStrictSubTreeCheck)
            {
                Test(stringText, strict: true, expected, strictDiagnostics, runStrictTreeCheck, runStrictSubTreeCheck);
            }
        }

        private void Test(
            string stringText, bool strict,
            string expectedTree, string expectedDiagnostics,
            bool runTreeCheck, bool runSubTreeChecks)
        {
            var tree = TryParseTree(stringText, strict, runTreeCheck, conversionFailureOk: false);
            if (tree == null)
            {
                return;
            }

            // Tests are allowed to not run the subtree tests.  This is because some
            // subtrees can cause the native regex parser to exhibit very bad behavior
            // (like not ever actually finishing compiling).
            if (runSubTreeChecks)
            {
                TryParseSubTrees(stringText, strict, runTreeCheck);
            }

            var actualTree = TreeToText(tree).Replace("\"", "\"\"");
            Assert.Equal(expectedTree.Replace("\"", "\"\""), actualTree);

            var actualDiagnostics = DiagnosticsToText(tree.Diagnostics).Replace("\"", "\"\"");
            Assert.Equal(expectedDiagnostics.Replace("\"", "\"\""), actualDiagnostics);
        }

        private void TryParseSubTrees(string stringText, bool strict, bool runTreeCheck)
        {
            // Trim the input from the right and make sure tree invariants hold
            var current = stringText;
            while (current != "@\"\"" && current != "\"\"")
            {
                current = current.Substring(0, current.Length - 2) + "\"";
                TryParseTree(current, strict, runTreeCheck, conversionFailureOk: true);
            }

            // Trim the input from the left and make sure tree invariants hold
            current = stringText;
            while (current != "@\"\"" && current != "\"\"")
            {
                if (current[0] == '@')
                {
                    current = "@\"" + current.Substring(3);
                }
                else
                {
                    current = "\"" + current.Substring(2);
                }

                TryParseTree(current, strict, runTreeCheck, conversionFailureOk: true);
            }

            for (var start = stringText[0] == '@' ? 2 : 1; start < stringText.Length - 1; start++)
            {
                TryParseTree(
                    stringText.Substring(0, start) +
                    stringText.Substring(start + 1, stringText.Length - (start + 1)),
                    strict, runTreeCheck,
                    conversionFailureOk: true);
            }
        }

        private (SyntaxToken, JsonTree?, VirtualCharSequence) JustParseTree(
            string stringText, bool strict, bool conversionFailureOk)
        {
            var token = CSharpJsonParserTests.GetStringToken(stringText);
            if (token.ValueText == "")
            {
                return default;
            }

            var allChars = _service.TryConvertToVirtualChars(token);
            if (allChars.IsDefault)
            {
                Assert.True(conversionFailureOk, "Failed to convert text to token.");
                return (token, null, allChars);
            }

            var tree = JsonParser.TryParse(allChars, strict);
            return (token, tree, allChars);
        }

        private JsonTree? TryParseTree(
            string stringText, bool strict, bool runTreeCheck, bool conversionFailureOk)
        {
            var (token, tree, allChars) = JustParseTree(stringText, strict, conversionFailureOk);
            if (tree == null)
            {
                Assert.True(allChars.IsDefault);
                return null;
            }

            CheckInvariants(tree, allChars);

            if (runTreeCheck)
            {
                if (!strict)
                {
                    try
                    {
                        JToken.Parse(token.ValueText);
                    }
                    catch (Exception)
                    {
                        Assert.NotEmpty(tree.Diagnostics);
                        return tree;
                    }
                }
                else
                {
                    try
                    {
                        var serializer1 = new DataContractJsonSerializer(typeof(object));
                        serializer1.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(token.ValueText)));

                        JsonDocument.Parse(token.ValueText, new JsonDocumentOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow });
                    }
                    catch (Exception)
                    {
                        Assert.NotEmpty(tree.Diagnostics);
                        return tree;
                    }
                }

                Assert.Empty(tree.Diagnostics);
            }

            return tree;
        }

        private static string TreeToText(JsonTree tree)
        {
            return new XElement("Tree",
                NodeToElement(tree.Root)).ToString();
        }

        private static string DiagnosticsToText(ImmutableArray<EmbeddedDiagnostic> diagnostics)
        {
            if (diagnostics.IsEmpty)
            {
                return "";
            }

            return new XElement("Diagnostics",
                diagnostics.Select(d =>
                    new XElement("Diagnostic",
                        new XAttribute("Message", d.Message),
                        new XAttribute("Start", d.Span.Start),
                        new XAttribute("Length", d.Span.Length)))).ToString();
        }

        private static XElement NodeToElement(JsonNode node)
        {
            if (node is JsonArrayNode arrayNode)
                return ArrayNodeToElement(arrayNode);

            if (node is JsonCompilationUnit compilationUnit)
                return CompilationUnitToElement(compilationUnit);

            if (node is JsonObjectNode objectNode)
                return ObjectNodeToElement(objectNode);

            if (node is JsonConstructorNode constructorNode)
                return ConstructorNodeToElement(constructorNode);

            var element = new XElement(node.Kind.ToString());
            foreach (var child in node)
                element.Add(child.IsNode ? NodeToElement(child.Node) : TokenToElement(child.Token));

            return element;
        }

        private static XElement ConstructorNodeToElement(JsonConstructorNode node)
        {
            var element = new XElement(node.Kind.ToString());
            element.Add(TokenToElement(node.NewKeyword));
            element.Add(TokenToElement(node.NameToken));
            element.Add(TokenToElement(node.OpenParenToken));
            element.Add(CreateSequenceNode(node.Sequence));
            element.Add(TokenToElement(node.CloseParenToken));
            return element;
        }

        private static XElement ObjectNodeToElement(JsonObjectNode node)
        {
            var element = new XElement(node.Kind.ToString());
            element.Add(TokenToElement(node.OpenBraceToken));
            element.Add(CreateSequenceNode(node.Sequence));
            element.Add(TokenToElement(node.CloseBraceToken));
            return element;
        }

        private static XElement CompilationUnitToElement(JsonCompilationUnit node)
        {
            var element = new XElement(node.Kind.ToString());
            element.Add(CreateSequenceNode(node.Sequence));
            element.Add(TokenToElement(node.EndOfFileToken));
            return element;
        }

        private static XElement ArrayNodeToElement(JsonArrayNode node)
        {
            var element = new XElement(node.Kind.ToString());
            element.Add(TokenToElement(node.OpenBracketToken));
            element.Add(CreateSequenceNode(node.Sequence));
            element.Add(TokenToElement(node.CloseBracketToken));
            return element;
        }

        private static XElement CreateSequenceNode(ImmutableArray<JsonValueNode> sequence)
        {
            var element = new XElement("Sequence");
            foreach (var child in sequence)
                element.Add(NodeToElement(child));
            return element;
        }

        private static XElement TokenToElement(JsonToken token)
        {
            var element = new XElement(token.Kind.ToString());

            if (token.Value != null)
            {
                element.Add(new XAttribute("value", token.Value));
            }

            if (token.LeadingTrivia.Length > 0)
            {
                element.Add(new XElement("Trivia", token.LeadingTrivia.Select(t => TriviaToElement(t))));
            }

            if (token.VirtualChars.Length > 0)
            {
                element.Add(token.VirtualChars.CreateString());
            }

            if (token.TrailingTrivia.Length > 0)
            {
                element.Add(new XElement("Trivia", token.TrailingTrivia.Select(t => TriviaToElement(t))));
            }

            return element;
        }

        private static XElement TriviaToElement(JsonTrivia trivia)
            => new(
                trivia.Kind.ToString(),
                trivia.VirtualChars.CreateString().Replace("\f", "\\f"));

        private static void CheckInvariants(JsonTree tree, VirtualCharSequence allChars)
        {
            var root = tree.Root;
            var position = 0;
            CheckInvariants(root, ref position, allChars);
            Assert.Equal(allChars.Length, position);
        }

        private static void CheckInvariants(JsonNode node, ref int position, VirtualCharSequence allChars)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    CheckInvariants(child.Node, ref position, allChars);
                }
                else
                {
                    CheckInvariants(child.Token, ref position, allChars);
                }
            }
        }

        private static void CheckInvariants(JsonToken token, ref int position, VirtualCharSequence allChars)
        {
            CheckInvariants(token.LeadingTrivia, ref position, allChars);
            CheckCharacters(token.VirtualChars, ref position, allChars);
            CheckInvariants(token.TrailingTrivia, ref position, allChars);
        }

        private static void CheckInvariants(ImmutableArray<JsonTrivia> leadingTrivia, ref int position, VirtualCharSequence allChars)
        {
            foreach (var trivia in leadingTrivia)
            {
                CheckInvariants(trivia, ref position, allChars);
            }
        }

        private static void CheckInvariants(JsonTrivia trivia, ref int position, VirtualCharSequence allChars)
        {
            switch (trivia.Kind)
            {
                case JsonKind.SingleLineCommentTrivia:
                case JsonKind.MultiLineCommentTrivia:
                case JsonKind.WhitespaceTrivia:
                case JsonKind.EndOfLineTrivia:
                    break;
                default:
                    Assert.False(true, "Incorrect trivia kind");
                    return;
            }

            CheckCharacters(trivia.VirtualChars, ref position, allChars);
        }

        private static void CheckCharacters(VirtualCharSequence virtualChars, ref int position, VirtualCharSequence allChars)
        {
            for (var i = 0; i < virtualChars.Length; i++)
            {
                Assert.Equal(allChars[position + i], virtualChars[i]);
            }

            position += virtualChars.Length;
        }

        private void TestNST(
            string stringText, string expected, string _, string strictDiagnostics, [CallerMemberName] string caller = "")
        {
            var (_, tree, allChars) = JustParseTree(stringText, strict: true, conversionFailureOk: false);
            Assert.NotNull(tree);
            Roslyn.Utilities.Contract.ThrowIfNull(tree);
            var actualTree = TreeToText(tree!).Replace("\"", "\"\"");
            Assert.Equal(expected.Replace("\"", "\"\""), actualTree);

            var actualDiagnostics = DiagnosticsToText(tree.Diagnostics).Replace("\"", "\"\"");
            Assert.Equal(strictDiagnostics.Replace("\"", "\"\""), actualDiagnostics);

            CheckInvariants(tree, allChars);

            if (caller.StartsWith("y_"))
            {
                // y_ tests must produce no diagnostics.
                Assert.Empty(strictDiagnostics);
            }
            else if (caller.StartsWith("i_"))
            {
                // We don't want to have diagnostics for i_ tests even though we're allowed to.
                // That's because we want our parser to be permissive when possible so we don't
                // error on json that is legal under some other parser.
                Assert.Empty(strictDiagnostics);
            }
            else if (caller.StartsWith("n_"))
            {
                // n_ tests must always produce diagnostics.
                Assert.NotEmpty(strictDiagnostics);
            }
            else
            {
                Assert.False(true, "Unexpected test name.");
            }
        }

        private string RemoveSequenceNode(string expected)
        {
            var element = XElement.Parse(expected);
            var result = RemoveSequenceNode(element);
            return result.ToString()!;
        }

        private object RemoveSequenceNode(XNode node)
        {
            if (node is not XElement element)
                return node;

            var children = element.Nodes().Select(RemoveSequenceNode);

            if (element.Name == "Sequence")
                return children;
            return new XElement(element.Name, children);
        }

        [Fact]
        public void TestDeepRecursion()
        {
            var (token, tree, chars) =
                JustParseTree(
@"@""[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[
[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[""",
strict: false, conversionFailureOk: false);
            Assert.False(token.IsMissing);
            Assert.False(chars.IsDefaultOrEmpty);
            Assert.Null(tree);
        }
    }
}
