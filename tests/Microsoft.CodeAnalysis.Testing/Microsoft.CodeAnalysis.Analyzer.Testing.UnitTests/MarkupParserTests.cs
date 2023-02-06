// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class MarkupParserTests
    {
        [Fact]
        public void SinglePosition1()
        {
            var markup = "first$$second";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPosition(markup, out var output, out var cursorPosition);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, cursorPosition));
        }

        [Fact]
        public void SinglePosition2()
        {
            var markup = "first$$second[||]";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPositionAndSpan(markup, out var output, out var cursorPosition, out var span);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, cursorPosition, ImmutableArray.Create(span)));
        }

        [Fact]
        public void SinglePosition3()
        {
            var markup = "first$$second";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPositionAndSpans(markup, out var output, out int cursorPosition, out ImmutableArray<TextSpan> spans);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, cursorPosition, spans));
        }

        [Fact]
        public void SinglePosition4()
        {
            var markup = "first$$second";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPositionAndSpans(markup, out var output, out int cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, cursorPosition, spans));
        }

        [Fact]
        public void SinglePosition5()
        {
            var markup = "first$$second";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPositionAndSpans(markup, out var output, out int? cursorPosition, out ImmutableArray<TextSpan> spans);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, cursorPosition, spans));
        }

        [Fact]
        public void SinglePosition6()
        {
            var markup = "first$$second";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPositionAndSpans(markup, out var output, out int? cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, cursorPosition, spans));
        }

        [Fact]
        public void MissingRequiredPosition1()
        {
            var markup = "[|first|] {|x:second|}";
            var ex = Assert.ThrowsAny<ArgumentException>(() => TestFileMarkupParser.GetPosition(markup, out _, out _));
            Assert.Equal("input", ex.ParamName);
        }

        [Fact]
        public void MissingRequiredPosition2()
        {
            var markup = "[|first|] {|x:second|}";
            var ex = Assert.ThrowsAny<ArgumentException>(() => TestFileMarkupParser.GetPositionAndSpan(markup, out _, out _, out _));
            Assert.Equal("input", ex.ParamName);
        }

        [Fact]
        public void MissingRequiredPosition3()
        {
            var markup = "[|first|] {|x:second|}";
            var ex = Assert.ThrowsAny<ArgumentException>(() => TestFileMarkupParser.GetPositionAndSpans(markup, out _, out int _, out ImmutableArray<TextSpan> _));
            Assert.Equal("input", ex.ParamName);
        }

        [Fact]
        public void MissingRequiredPosition4()
        {
            var markup = "[|first|] {|x:second|}";
            var ex = Assert.ThrowsAny<ArgumentException>(() => TestFileMarkupParser.GetPositionAndSpans(markup, out _, out int _, out ImmutableDictionary<string, ImmutableArray<TextSpan>> _));
            Assert.Equal("input", ex.ParamName);
        }

        [Fact]
        public void MissingOptionalPosition1()
        {
            var markup = "[|first|] {|x:second|}";
            TestFileMarkupParser.GetPositionAndSpans(markup, out var output, out int? cursorPosition, out ImmutableArray<TextSpan> spans);
            Assert.Null(cursorPosition);

            // Test round-trip. In this case, named spans are ignored due to the API used for parsing the original
            // markup string.
            var equivalentMarkup = "[|first|] second";
            Assert.Equal(equivalentMarkup, TestFileMarkupParser.CreateTestFile(output, cursorPosition, spans));
        }

        [Fact]
        public void MissingOptionalPosition2()
        {
            var markup = "[|first|] {|x:second|}";
            TestFileMarkupParser.GetPositionAndSpans(markup, out var output, out int? cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Null(cursorPosition);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, cursorPosition, spans));
        }

        [Fact]
        public void MissingOptionalPosition3()
        {
            var markup = "[|first|] {|x:second|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Empty(positions);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, positions, spans));
        }

        [Fact]
        public void NonOverlappingSpans1()
        {
            var markup = "{|x:first|} {|y:second|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 5), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 12), y);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, positions, spans));
        }

        [Fact]
        public void OverlappingSpans1()
        {
            var markup = "{|x:first {|y:second|}|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 12), y);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, positions, spans));
        }

        [Fact]
        public void OverlappingSpans2()
        {
            var markup = "{|x:first {|y:seco|}nd|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 10), y);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, positions, spans));
        }

        [Fact]
        public void OverlappingSpans3A()
        {
            var markup = "{|#0:first {|y:seco|}nd|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("#0", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 10), y);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, positions, spans));
        }

        [Fact]
        public void OverlappingSpans3B()
        {
            var markup = "{|#0:first {|y:seco|}nd|#0}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("#0", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 10), y);

            // Test round-trip
            var equivalentMarkup = "{|#0:first {|y:seco|}nd|}";
            Assert.Equal(equivalentMarkup, TestFileMarkupParser.CreateTestFile(output, positions, spans));
        }

        [Fact]
        public void OverlappingSpans3C()
        {
            var markup = "{|#0:first {|y:seco|#0}nd|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("#0", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 10), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 12), y);

            // Test round-trip
            // https://github.com/dotnet/roslyn-sdk/issues/505
            var unexpectedMarkup = "{|#0:first {|y:seco|}nd|}";
            Assert.Equal(unexpectedMarkup, TestFileMarkupParser.CreateTestFile(output, positions, spans));
        }

        [Fact]
        public void OverlappingSpans4A()
        {
            var markup = "{|x:first {|#0:seco|}nd|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("#0", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 10), y);

            // Test round-trip
            Assert.Equal(markup, TestFileMarkupParser.CreateTestFile(output, positions, spans));
        }

        [Fact]
        public void OverlappingSpans4B()
        {
            var markup = "{|x:first {|#0:seco|#0}nd|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("#0", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 10), y);

            // Test round-trip
            var equivalentMarkup = "{|x:first {|#0:seco|}nd|}";
            Assert.Equal(equivalentMarkup, TestFileMarkupParser.CreateTestFile(output, positions, spans));
        }

        [Fact]
        public void OverlappingSpans4C()
        {
            var markup = "{|x:first {|#0:seco|}nd|#0}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var output, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 10), x);

            Assert.True(spans.TryGetValue("#0", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 12), y);

            // Test round-trip
            // https://github.com/dotnet/roslyn-sdk/issues/505
            var unexpectedMarkup = "{|x:first {|#0:seco|}nd|}";
            Assert.Equal(unexpectedMarkup, TestFileMarkupParser.CreateTestFile(output, positions, spans));
        }

        [Fact]
        public void CDataMarkup1()
        {
            var markup = "{|X:[|<![CDATA[|]|}text[|]]>|]";
            var expected = "<![CDATA[text]]>";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var result, out var positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(expected, result);

            Assert.Empty(positions);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("X", out var named));
            Assert.Equal(new[] { new TextSpan(0, 9) }, named);

            Assert.True(spans.TryGetValue(string.Empty, out var unnamed));
            Assert.Equal(new[] { new TextSpan(0, 9), new TextSpan(13, 3) }, unnamed);
        }

        [Fact]
        public void CDataMarkup2()
        {
            var markup = @"[|<![CDATA[|]text{|X:[|]]>|]|}";
            var expected = @"<![CDATA[text]]>";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var result, out var positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(expected, result);

            Assert.Empty(positions);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("X", out var named));
            Assert.Equal(new[] { new TextSpan(13, 3) }, named);

            Assert.True(spans.TryGetValue(string.Empty, out var unnamed));
            Assert.Equal(new[] { new TextSpan(0, 9), new TextSpan(13, 3) }, unnamed);
        }

        [Fact]
        [WorkItem(637, "https://github.com/dotnet/roslyn-sdk/issues/637")]
        public void MarkupSpanSplitsEndOfLine()
        {
            var markup = "class C { }\r{|b:\n|}";
            var expected = "class C { }\r\n";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out var result, out var positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(expected, result);

            Assert.Empty(positions);
            Assert.Single(spans);

            Assert.True(spans.TryGetValue("b", out var named));
            Assert.Equal(new[] { new TextSpan(12, 1) }, named);

            Assert.False(spans.TryGetValue(string.Empty, out _));
        }
    }
}
