// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private SyntaxToken GetStringToken(string text)
        {
            var statement = _statmentPrefix + text;
            var parsedStatement = SyntaxFactory.ParseStatement(statement);
            var token = parsedStatement.DescendantTokens().ToArray()[3];
            Assert.True(token.Kind() == SyntaxKind.StringLiteralToken);

            return token;
        }

        private void Test(string stringText, string expected, RegexOptions options,
            bool runSubTreeTests = true, [CallerMemberName]string name = "",
            bool allowIndexOutOfRange = false,
            bool allowNullReference = false,
            bool allowOutOfMemory = false)
        {
            var (tree, sourceText) = TryParseTree(stringText, options, conversionFailureOk: false,
                allowIndexOutOfRange, allowNullReference, allowOutOfMemory);

            // Tests are allowed to not run the subtree tests.  This is because some
            // subtrees can cause the native regex parser to exhibit very bad behavior
            // (like not ever actually finishing compiling).
            if (runSubTreeTests)
            {
                TryParseSubTrees(stringText, options,
                    allowIndexOutOfRange,
                    allowNullReference,
                    allowOutOfMemory);
            }

            var actual = TreeToText(sourceText, tree).Replace("\"", "\"\"");
            Assert.Equal(expected.Replace("\"", "\"\""), actual);
        }

        private void TryParseSubTrees(
            string stringText, RegexOptions options,
            bool allowIndexOutOfRange,
            bool allowNullReference,
            bool allowOutOfMemory)
        {
            // Trim the input from the right and make sure tree invariants hold
            var current = stringText;
            while (current != "@\"\"" && current != "\"\"")
            {
                current = current.Substring(0, current.Length - 2) + "\"";
                TryParseTree(current, options, conversionFailureOk: true,
                    allowIndexOutOfRange,
                    allowNullReference,
                    allowOutOfMemory);
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

                TryParseTree(current, options, conversionFailureOk: true,
                    allowIndexOutOfRange,
                    allowNullReference,
                    allowOutOfMemory);
            }

            for (var start = stringText[0] == '@' ? 2 : 1; start < stringText.Length - 1; start++)
            {
                TryParseTree(
                    stringText.Substring(0, start) +
                    stringText.Substring(start + 1, stringText.Length - (start + 1)),
                    options, conversionFailureOk: true,
                    allowIndexOutOfRange,
                    allowNullReference,
                    allowOutOfMemory);
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
            bool allowOutOfMemeory)
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
            catch (OutOfMemoryException) when (allowOutOfMemeory)
            {
                // bug with .NET regex parser.  can happen with patterns like: a{2147483647,}
                return treeAndText;
            }
            catch (ArgumentException ex)
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

                return treeAndText;
            }

            Assert.Empty(tree.Diagnostics);

            Assert.True(regex.GetGroupNumbers().OrderBy(v => v).SequenceEqual(
                tree.CaptureNumbersToSpan.Keys.OrderBy(v => v)));

            Assert.True(regex.GetGroupNames().Where(v => !int.TryParse(v, out _)).OrderBy(v => v).SequenceEqual(
                tree.CaptureNamesToSpan.Keys.OrderBy(v => v)));

            return treeAndText;
        }

        private string TreeToText(SourceText text, RegexTree tree)
        {
            var element = new XElement("Tree",
                NodeToElement(tree.Root));

            if (tree.Diagnostics.Length > 0)
            {
                element.Add(new XElement("Diagnostics",
                    tree.Diagnostics.Select(d =>
                        new XElement("Diagnostic",
                            new XAttribute("Message", d.Message),
                            new XAttribute("Span", d.Span),
                            GetTextAttribute(text, d.Span)))));
            }

            element.Add(new XElement("Captures",
                tree.CaptureNumbersToSpan.OrderBy(kvp => kvp.Key).Select(kvp =>
                    new XElement("Capture", new XAttribute("Name", kvp.Key), new XAttribute("Span", kvp.Value), GetTextAttribute(text, kvp.Value))),
                tree.CaptureNamesToSpan.OrderBy(kvp => kvp.Key).Select(kvp =>
                    new XElement("Capture", new XAttribute("Name", kvp.Key), new XAttribute("Span", kvp.Value), GetTextAttribute(text, kvp.Value)))));

            return element.ToString();
        }

        private static XAttribute GetTextAttribute(SourceText text, TextSpan span)
            => new XAttribute("Text", text.ToString(span));

        private XElement NodeToElement(RegexNode node)
        {
            var element = new XElement(node.Kind.ToString());
            foreach (var child in node)
            {
                element.Add(child.IsNode ? NodeToElement(child.Node) : TokenToElement(child.Token));
            }

            return element;
        }

        private XElement TokenToElement(RegexToken token)
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

        private XElement TriviaToElement(RegexTrivia trivia)
            => new XElement(
                trivia.Kind.ToString(),
                trivia.VirtualChars.CreateString());

        private void CheckInvariants(RegexTree tree, VirtualCharSequence allChars)
        {
            var root = tree.Root;
            var position = 0;
            CheckInvariants(root, ref position, allChars);
            Assert.Equal(allChars.Length, position);
        }

        private void CheckInvariants(RegexNode node, ref int position, VirtualCharSequence allChars)
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

        private void CheckInvariants(RegexToken token, ref int position, VirtualCharSequence allChars)
        {
            CheckInvariants(token.LeadingTrivia, ref position, allChars);
            CheckCharacters(token.VirtualChars, ref position, allChars);
        }

        private void CheckInvariants(ImmutableArray<RegexTrivia> leadingTrivia, ref int position, VirtualCharSequence allChars)
        {
            foreach (var trivia in leadingTrivia)
            {
                CheckInvariants(trivia, ref position, allChars);
            }
        }

        private void CheckInvariants(RegexTrivia trivia, ref int position, VirtualCharSequence allChars)
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
    }
}
