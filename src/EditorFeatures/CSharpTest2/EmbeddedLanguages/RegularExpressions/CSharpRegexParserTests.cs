// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EmbeddedLanguages.RegularExpressions
{
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;
    using RegexTrivia = EmbeddedSyntaxTrivia<RegexKind>;

    public partial class CSharpRegexParserTests
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

        private void Test(string stringText, string expected, RegexOptions options,
            bool runSubTreeTests = true,
            bool allowIndexOutOfRange = false,
            bool allowNullReference = false,
            bool allowOutOfMemory = false,
            bool allowDiagnosticsMismatch = false)
        {
            var (tree, sourceText) = TryParseTree(stringText, options, conversionFailureOk: false,
                allowIndexOutOfRange, allowNullReference, allowOutOfMemory, allowDiagnosticsMismatch);

            // Tests are allowed to not run the subtree tests.  This is because some
            // subtrees can cause the native regex parser to exhibit very bad behavior
            // (like not ever actually finishing compiling).
            if (runSubTreeTests)
            {
                TryParseSubTrees(stringText, options,
                    allowIndexOutOfRange, allowNullReference, allowOutOfMemory, allowDiagnosticsMismatch);
            }

            const string DoubleQuoteEscaping = "\"\"";
            var actual = TreeToText(sourceText, tree)
                .Replace("\"", DoubleQuoteEscaping)
                .Replace("&quot;", DoubleQuoteEscaping);
            Assert.Equal(expected.Replace("\"", DoubleQuoteEscaping), actual);
        }

        private void TryParseSubTrees(
            string stringText, RegexOptions options,
            bool allowIndexOutOfRange,
            bool allowNullReference,
            bool allowOutOfMemory,
            bool allowDiagnosticsMismatch)
        {
            // Trim the input from the right and make sure tree invariants hold
            var current = stringText;
            while (current is not "@\"\"" and not "\"\"")
            {
                current = current.Substring(0, current.Length - 2) + "\"";
                TryParseTree(current, options, conversionFailureOk: true,
                    allowIndexOutOfRange, allowNullReference, allowOutOfMemory, allowDiagnosticsMismatch);
            }

            // Trim the input from the left and make sure tree invariants hold
            current = stringText;
            while (current is not "@\"\"" and not "\"\"")
            {
                if (current[0] == '@')
                {
                    current = "@\"" + current.Substring(3);
                }
                else
                {
                    current = "\"" + current.Substring(2);
                }

                TryParseTree(current, options, conversionFailureOk: true,
                    allowIndexOutOfRange, allowNullReference, allowOutOfMemory, allowDiagnosticsMismatch);
            }

            for (var start = stringText[0] == '@' ? 2 : 1; start < stringText.Length - 1; start++)
            {
                TryParseTree(
                    stringText.Substring(0, start) +
                    stringText.Substring(start + 1, stringText.Length - (start + 1)),
                    options, conversionFailureOk: true,
                    allowIndexOutOfRange, allowNullReference, allowOutOfMemory, allowDiagnosticsMismatch);
            }
        }

        private (SyntaxToken, RegexTree, VirtualCharSequence) JustParseTree(
            string stringText, RegexOptions options, bool conversionFailureOk)
        {
            var token = GetStringToken(stringText);
            var allChars = _service.TryConvertToVirtualChars(token);
            if (allChars.IsDefault)
            {
                Assert.True(conversionFailureOk, "Failed to convert text to token.");
                return (token, null, allChars);
            }

            var tree = RegexParser.TryParse(allChars, options);
            return (token, tree, allChars);
        }

        private (RegexTree, SourceText) TryParseTree(
            string stringText, RegexOptions options,
            bool conversionFailureOk,
            bool allowIndexOutOfRange,
            bool allowNullReference,
            bool allowOutOfMemory,
            bool allowDiagnosticsMismatch = false)
        {
            var (token, tree, allChars) = JustParseTree(stringText, options, conversionFailureOk);
            if (tree == null)
            {
                Assert.True(allChars.IsDefault);
                return default;
            }

            CheckInvariants(tree, allChars);
            var sourceText = token.SyntaxTree.GetText();
            var treeAndText = (tree, sourceText);

            Regex regex = null;
            try
            {
                regex = new Regex(token.ValueText, options);
            }
            catch (IndexOutOfRangeException) when (allowIndexOutOfRange)
            {
                // bug with .NET regex parser.  Can happen with patterns like: (?<-0
                Assert.NotEmpty(tree.Diagnostics);
                return treeAndText;
            }
            catch (NullReferenceException) when (allowNullReference)
            {
                // bug with .NET regex parser.  can happen with patterns like: (?(?S))
                return treeAndText;
            }
            catch (OutOfMemoryException) when (allowOutOfMemory)
            {
                // bug with .NET regex parser.  can happen with patterns like: a{2147483647,}
                return treeAndText;
            }
            catch (ArgumentException ex)
            {
                if (!allowDiagnosticsMismatch)
                {
                    Assert.NotEmpty(tree.Diagnostics);

                    // Ensure the diagnostic we emit is the same as the .NET one. Note: we can only
                    // do this in en-US as that's the only culture where we control the text exactly
                    // and can ensure it exactly matches Regex.  We depend on localization to do a 
                    // good enough job here for other languages.
                    if (Thread.CurrentThread.CurrentCulture.Name == "en-US")
                    {
                        Assert.True(tree.Diagnostics.Any(d => ex.Message.Contains(d.Message)));
                    }
                }

                return treeAndText;
            }

            if (!tree.Diagnostics.IsEmpty && !allowDiagnosticsMismatch)
            {
                var expectedDiagnostics = CreateDiagnosticsElement(sourceText, tree);
                Assert.False(true, "Expected diagnostics: \r\n" + expectedDiagnostics.ToString().Replace(@"""", @""""""));
            }

            Assert.True(regex.GetGroupNumbers().OrderBy(v => v).SequenceEqual(
                tree.CaptureNumbersToSpan.Keys.OrderBy(v => v)));

            Assert.True(regex.GetGroupNames().Where(v => !int.TryParse(v, out _)).OrderBy(v => v).SequenceEqual(
                tree.CaptureNamesToSpan.Keys.OrderBy(v => v)));

            return treeAndText;
        }

        private static string TreeToText(SourceText text, RegexTree tree)
        {
            var element = new XElement("Tree",
                NodeToElement(tree.Root));

            if (tree.Diagnostics.Length > 0)
            {
                element.Add(CreateDiagnosticsElement(text, tree));
            }

            element.Add(new XElement("Captures",
                tree.CaptureNumbersToSpan.OrderBy(kvp => kvp.Key).Select(kvp =>
                    new XElement("Capture", new XAttribute("Name", kvp.Key), new XAttribute("Span", kvp.Value), GetTextAttribute(text, kvp.Value))),
                tree.CaptureNamesToSpan.OrderBy(kvp => kvp.Key).Select(kvp =>
                    new XElement("Capture", new XAttribute("Name", kvp.Key), new XAttribute("Span", kvp.Value), GetTextAttribute(text, kvp.Value)))));

            return element.ToString();
        }

        private static XElement CreateDiagnosticsElement(SourceText text, RegexTree tree)
            => new XElement("Diagnostics",
                tree.Diagnostics.Select(d =>
                    new XElement("Diagnostic",
                        new XAttribute("Message", d.Message),
                        new XAttribute("Span", d.Span),
                        GetTextAttribute(text, d.Span))));

        private static XAttribute GetTextAttribute(SourceText text, TextSpan span)
            => new("Text", text.ToString(span));

        private static XElement NodeToElement(RegexNode node)
        {
            if (node is RegexAlternationNode alternationNode)
                return AlternationToElement(alternationNode, alternationNode.SequenceList.NodesAndTokens.Length);

            var element = new XElement(node.Kind.ToString());
            foreach (var child in node)
                element.Add(child.IsNode ? NodeToElement(child.Node) : TokenToElement(child.Token));

            return element;
        }

        private static XElement AlternationToElement(RegexAlternationNode alternationNode, int end)
        {
            // to keep tests in sync with how we used to structure alternations, we specially handle this node.
            // First, if the node only has a single element, then just print that element as that's what would
            // normally be inlined into the parent.
            if (end == 1)
                return NodeToElement(alternationNode.SequenceList.NodesAndTokens[0].Node);

            var element = new XElement(alternationNode.Kind.ToString());
            element.Add(AlternationToElement(alternationNode, end - 2));
            element.Add(TokenToElement(alternationNode.SequenceList.NodesAndTokens[end - 2].Token));
            element.Add(NodeToElement(alternationNode.SequenceList.NodesAndTokens[end - 1].Node));
            return element;
        }

        private static XElement TokenToElement(RegexToken token)
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

            return element;
        }

        private static XElement TriviaToElement(RegexTrivia trivia)
            => new XElement(
                trivia.Kind.ToString(),
                trivia.VirtualChars.CreateString());

        private static void CheckInvariants(RegexTree tree, VirtualCharSequence allChars)
        {
            var root = tree.Root;
            var position = 0;
            CheckInvariants(root, ref position, allChars);
            Assert.Equal(allChars.Length, position);
        }

        private static void CheckInvariants(RegexNode node, ref int position, VirtualCharSequence allChars)
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

        private static void CheckInvariants(RegexToken token, ref int position, VirtualCharSequence allChars)
        {
            CheckInvariants(token.LeadingTrivia, ref position, allChars);
            CheckCharacters(token.VirtualChars, ref position, allChars);
        }

        private static void CheckInvariants(ImmutableArray<RegexTrivia> leadingTrivia, ref int position, VirtualCharSequence allChars)
        {
            foreach (var trivia in leadingTrivia)
            {
                CheckInvariants(trivia, ref position, allChars);
            }
        }

        private static void CheckInvariants(RegexTrivia trivia, ref int position, VirtualCharSequence allChars)
        {
            switch (trivia.Kind)
            {
                case RegexKind.CommentTrivia:
                case RegexKind.WhitespaceTrivia:
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

        private static string And(params string[] regexes)
        {
            var conj = $"({regexes[regexes.Length - 1]})";
            for (var i = regexes.Length - 2; i >= 0; i--)
                conj = $"(?({regexes[i]}){conj}|[0-[0]])";

            return conj;
        }

        private static string Not(string regex)
            => $"(?({regex})[0-[0]]|.*)";

        [Fact]
        public void TestDeepRecursion()
        {
            var (token, tree, chars) =
                JustParseTree(
@"@""((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((
(((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((((""", RegexOptions.None, conversionFailureOk: false);
            Assert.False(token.IsMissing);
            Assert.False(chars.IsDefaultOrEmpty);
            Assert.Null(tree);
        }

        [Fact]
        public void TestNoStackOverflow()
        {
            for (var i = 1; i < 1200; i++)
            {
                var text = new string('(', i);
                var (token, _, chars) = JustParseTree($@"@""{text}""", RegexOptions.None, conversionFailureOk: false);
                Assert.False(token.IsMissing);
                Assert.False(chars.IsDefaultOrEmpty);
            }
        }
    }
}
