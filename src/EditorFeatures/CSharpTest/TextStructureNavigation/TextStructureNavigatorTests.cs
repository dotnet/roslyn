﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.TextStructureNavigation;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TextStructureNavigation
{
    [UseExportProvider]
    public class TextStructureNavigatorTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void Empty()
        {
            AssertExtent(
                string.Empty,
                pos: 0,
                isSignificant: false,
                start: 0, length: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void Whitespace()
        {
            AssertExtent(
                "   ",
                pos: 0,
                isSignificant: false,
                start: 0, length: 3);

            AssertExtent(
                "   ",
                pos: 1,
                isSignificant: false,
                start: 0, length: 3);

            AssertExtent(
                "   ",
                pos: 3,
                isSignificant: false,
                start: 0, length: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void EndOfFile()
        {
            AssertExtent(
                "using System;",
                pos: 13,
                isSignificant: true,
                start: 12, length: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void NewLine()
        {
            AssertExtent(
                "class Class1 {\r\n\r\n}",
                pos: 14,
                isSignificant: false,
                start: 14, length: 2);

            AssertExtent(
                "class Class1 {\r\n\r\n}",
                pos: 15,
                isSignificant: false,
                start: 14, length: 2);

            AssertExtent(
                "class Class1 {\r\n\r\n}",
                pos: 16,
                isSignificant: false,
                start: 16, length: 2);

            AssertExtent(
                "class Class1 {\r\n\r\n}",
                pos: 17,
                isSignificant: false,
                start: 16, length: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void SingleLineComment()
        {
            AssertExtent(
                "// Comment  ",
                pos: 0,
                isSignificant: true,
                start: 0, length: 12);

            // It is important that this returns just the comment banner. Returning the whole comment
            // means Ctrl+Right before the slash will cause it to jump across the entire comment
            AssertExtent(
                "// Comment  ",
                pos: 1,
                isSignificant: true,
                start: 0, length: 2);

            AssertExtent(
                "// Comment  ",
                pos: 5,
                isSignificant: true,
                start: 3, length: 7);

            AssertExtent(
                "// () test",
                pos: 4,
                isSignificant: true,
                start: 3, length: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void MultiLineComment()
        {
            AssertExtent(
                "/* Comment */",
                pos: 0,
                isSignificant: true,
                start: 0, length: 13);

            // It is important that this returns just the comment banner. Returning the whole comment
            // means Ctrl+Right before the slash will cause it to jump across the entire comment
            AssertExtent(
                "/* Comment */",
                pos: 1,
                isSignificant: true,
                start: 0, length: 2);

            AssertExtent(
                "/* Comment */",
                pos: 5,
                isSignificant: true,
                start: 3, length: 7);

            AssertExtent(
                "/* () test */",
                pos: 4,
                isSignificant: true,
                start: 3, length: 2);

            AssertExtent(
               "/* () test */",
               pos: 11,
               isSignificant: true,
               start: 11, length: 2);

            // It is important that this returns just the comment banner. Returning the whole comment
            // means Ctrl+Left after the slash will cause it to jump across the entire comment
            AssertExtent(
               "/* () test */",
               pos: 12,
               isSignificant: true,
               start: 11, length: 2);

            AssertExtent(
               "/* () test */",
               pos: 13,
               isSignificant: true,
               start: 11, length: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void Keyword()
        {
            for (var i = 7; i <= 7 + 4; i++)
            {
                AssertExtent(
                    "public class Class1",
                    pos: i,
                    isSignificant: true,
                    start: 7, length: 5);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void Identifier()
        {
            for (var i = 13; i <= 13 + 8; i++)
            {
                AssertExtent(
                    "public class SomeClass : IDisposable",
                    pos: i,
                    isSignificant: true,
                    start: 13, length: 9);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void EscapedIdentifier()
        {
            for (var i = 12; i <= 12 + 9; i++)
            {
                AssertExtent(
                    "public enum @interface : int",
                    pos: i,
                    isSignificant: true,
                    start: 12, length: 10);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void Number()
        {
            for (var i = 37; i <= 37 + 10; i++)
            {
                AssertExtent(
                    "class Test { private double num   = -1.234678e10; }",
                    pos: i,
                    isSignificant: true,
                    start: 37, length: 11);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void String()
        {
            const string TestString = "class Test { private string s1 = \" () test  \"; }";
            var startOfString = TestString.IndexOf('"');
            var lengthOfStringIncludingQuotes = TestString.LastIndexOf('"') - startOfString + 1;

            AssertExtent(
                TestString,
                pos: startOfString,
                isSignificant: true,
                start: startOfString, length: 1);

            // Selects whitespace
            AssertExtent(
                TestString,
                pos: startOfString + 1,
                isSignificant: false,
                start: startOfString + 1, length: 1);

            AssertExtent(
                TestString,
                pos: startOfString + 2,
                isSignificant: true,
                start: startOfString + 2, length: 2);

            AssertExtent(
                TestString,
                pos: TestString.IndexOf("  \"", StringComparison.Ordinal),
                isSignificant: false,
                start: TestString.IndexOf("  \"", StringComparison.Ordinal), length: 2);

            AssertExtent(
                TestString,
                pos: TestString.LastIndexOf('"'),
                isSignificant: true,
                start: startOfString + lengthOfStringIncludingQuotes - 1, length: 1);

            AssertExtent(
                TestString,
                pos: TestString.LastIndexOf('"') + 1,
                isSignificant: true,
                start: TestString.LastIndexOf('"') + 1, length: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void InterpolatedString1()
        {
            const string TestString = "class Test { string x = \"hello\"; string s = $\" { x } hello\"; }";

            var startOfFirstString = TestString.IndexOf('"');
            var endOfFirstString = TestString.IndexOf('"', startOfFirstString + 1);
            var startOfString = TestString.IndexOf("$\"", endOfFirstString + 1, StringComparison.Ordinal);
            var lengthOfStringIncludingQuotes = TestString.LastIndexOf('"') - startOfString + 1;

            // Selects interpolated string start token
            AssertExtent(
                TestString,
                pos: startOfString,
                isSignificant: true,
                start: startOfString, length: 2);

            // Selects whitespace
            AssertExtent(
                TestString,
                pos: startOfString + 2,
                isSignificant: false,
                start: startOfString + 2, length: 1);

            // Selects the opening curly brace
            AssertExtent(
                TestString,
                pos: startOfString + 3,
                isSignificant: true,
                start: startOfString + 3, length: 1);

            // Selects whitespace
            AssertExtent(
                TestString,
                pos: startOfString + 4,
                isSignificant: false,
                start: startOfString + 4, length: 1);

            // Selects identifier
            AssertExtent(
                TestString,
                pos: startOfString + 5,
                isSignificant: true,
                start: startOfString + 5, length: 1);

            // Selects whitespace
            AssertExtent(
                TestString,
                pos: startOfString + 6,
                isSignificant: false,
                start: startOfString + 6, length: 1);

            // Selects the closing curly brace
            AssertExtent(
                TestString,
                pos: startOfString + 7,
                isSignificant: true,
                start: startOfString + 7, length: 1);

            // Selects whitespace
            AssertExtent(
                TestString,
                pos: startOfString + 8,
                isSignificant: false,
                start: startOfString + 8, length: 1);

            // Selects hello
            AssertExtent(
                TestString,
                pos: startOfString + 9,
                isSignificant: true,
                start: startOfString + 9, length: 5);

            // Selects closing quote
            AssertExtent(
                TestString,
                pos: startOfString + 14,
                isSignificant: true,
                start: startOfString + 14, length: 1);
        }

        private static void AssertExtent(string code, int pos, bool isSignificant, int start, int length)
        {
            AssertExtent(code, pos, isSignificant, start, length, null);
            AssertExtent(code, pos, isSignificant, start, length, Options.Script);
        }

        private static void AssertExtent(string code, int pos, bool isSignificant, int start, int length, CSharpParseOptions options)
        {
            using var workspace = TestWorkspace.CreateCSharp(code, options);
            var buffer = workspace.Documents.First().GetTextBuffer();

            var provider = new TextStructureNavigatorProvider(
                workspace.GetService<ITextStructureNavigatorSelectorService>(),
                workspace.GetService<IContentTypeRegistryService>(),
                workspace.GetService<IWaitIndicator>());

            var navigator = provider.CreateTextStructureNavigator(buffer);

            var extent = navigator.GetExtentOfWord(new SnapshotPoint(buffer.CurrentSnapshot, pos));
            Assert.Equal(isSignificant, extent.IsSignificant);

            var expectedSpan = new SnapshotSpan(buffer.CurrentSnapshot, start, length);
            Assert.Equal(expectedSpan, extent.Span);
        }

        private static void TestNavigator(
            string code,
            Func<ITextStructureNavigator, SnapshotSpan, SnapshotSpan> func,
            int startPosition,
            int startLength,
            int endPosition,
            int endLength)
        {
            TestNavigator(code, func, startPosition, startLength, endPosition, endLength, null);
            TestNavigator(code, func, startPosition, startLength, endPosition, endLength, Options.Script);
        }

        private static void TestNavigator(
            string code,
            Func<ITextStructureNavigator, SnapshotSpan, SnapshotSpan> func,
            int startPosition,
            int startLength,
            int endPosition,
            int endLength,
            CSharpParseOptions options)
        {
            using var workspace = TestWorkspace.CreateCSharp(code, options);
            var buffer = workspace.Documents.First().GetTextBuffer();

            var provider = new TextStructureNavigatorProvider(
                workspace.GetService<ITextStructureNavigatorSelectorService>(),
                workspace.GetService<IContentTypeRegistryService>(),
                workspace.GetService<IWaitIndicator>());

            var navigator = provider.CreateTextStructureNavigator(buffer);

            var actualSpan = func(navigator, new SnapshotSpan(buffer.CurrentSnapshot, startPosition, startLength));
            var expectedSpan = new SnapshotSpan(buffer.CurrentSnapshot, endPosition, endLength);
            Assert.Equal(expectedSpan, actualSpan.Span);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void GetSpanOfEnclosingTest()
        {
            // First operation returns span of 'Class1'
            TestNavigator(
@"class Class1 { }", (n, s) => n.GetSpanOfEnclosing(s), 10, 0, 6, 6);

            // Second operation returns span of 'class Class1 { }'
            TestNavigator(
@"class Class1 { }", (n, s) => n.GetSpanOfEnclosing(s), 6, 6, 0, 16);

            // Last operation does nothing
            TestNavigator(
@"class Class1 { }", (n, s) => n.GetSpanOfEnclosing(s), 0, 16, 0, 16);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void GetSpanOfFirstChildTest()
        {
            // Go from 'class Class1 { }' to 'class'
            TestNavigator(
@"class Class1
{
}", (n, s) => n.GetSpanOfFirstChild(s), 0, 16, 0, 5);

            // Next operation should do nothing as we're at the bottom
            TestNavigator(
@"class Class1
{
}", (n, s) => n.GetSpanOfFirstChild(s), 0, 5, 0, 5);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void GetSpanOfNextSiblingTest()
        {
            // Go from 'class' to 'Class1'
            TestNavigator(
@"class Class1
{
}", (n, s) => n.GetSpanOfNextSibling(s), 0, 5, 6, 6);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
        public void GetSpanOfPreviousSiblingTest()
        {
            // Go from '{' to 'Class1'
            TestNavigator(
@"class Class1 { }", (n, s) => n.GetSpanOfPreviousSibling(s), 13, 1, 6, 6);
        }
    }
}
