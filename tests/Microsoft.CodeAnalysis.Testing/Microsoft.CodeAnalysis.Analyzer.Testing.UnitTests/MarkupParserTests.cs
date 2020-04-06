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
        }

        [Fact]
        public void SinglePosition2()
        {
            var markup = "first$$second[||]";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPositionAndSpan(markup, out var output, out var cursorPosition, out _);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);
        }

        [Fact]
        public void SinglePosition3()
        {
            var markup = "first$$second";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPositionAndSpans(markup, out var output, out int cursorPosition, out ImmutableArray<TextSpan> _);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);
        }

        [Fact]
        public void SinglePosition4()
        {
            var markup = "first$$second";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPositionAndSpans(markup, out var output, out int cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> _);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);
        }

        [Fact]
        public void SinglePosition5()
        {
            var markup = "first$$second";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPositionAndSpans(markup, out var output, out int? cursorPosition, out ImmutableArray<TextSpan> _);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);
        }

        [Fact]
        public void SinglePosition6()
        {
            var markup = "first$$second";
            var expected = "firstsecond";

            TestFileMarkupParser.GetPositionAndSpans(markup, out var output, out int? cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> _);
            Assert.Equal(expected, output);
            Assert.Equal(markup.IndexOf("$$"), cursorPosition);
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
            TestFileMarkupParser.GetPositionAndSpans(markup, out _, out int? cursorPosition, out ImmutableArray<TextSpan> _);
            Assert.Null(cursorPosition);
        }

        [Fact]
        public void MissingOptionalPosition2()
        {
            var markup = "[|first|] {|x:second|}";
            TestFileMarkupParser.GetPositionAndSpans(markup, out _, out int? cursorPosition, out ImmutableDictionary<string, ImmutableArray<TextSpan>> _);
            Assert.Null(cursorPosition);
        }

        [Fact]
        public void MissingOptionalPosition3()
        {
            var markup = "[|first|] {|x:second|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out _, out ImmutableArray<int> positions, out ImmutableDictionary<string, ImmutableArray<TextSpan>> _);
            Assert.Empty(positions);
        }

        [Fact]
        public void NonOverlappingSpans1()
        {
            var markup = "{|x:first|} {|y:second|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out _, out ImmutableArray<int> _, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 5), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 12), y);
        }

        [Fact]
        public void OverlappingSpans1()
        {
            var markup = "{|x:first {|y:second|}|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out _, out ImmutableArray<int> _, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 12), y);
        }

        [Fact]
        public void OverlappingSpans2()
        {
            var markup = "{|x:first {|y:seco|}nd|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out _, out ImmutableArray<int> _, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 10), y);
        }

        [Fact]
        public void OverlappingSpans3A()
        {
            var markup = "{|#0:first {|y:seco|}nd|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out _, out ImmutableArray<int> _, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("#0", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 10), y);
        }

        [Fact]
        public void OverlappingSpans3B()
        {
            var markup = "{|#0:first {|y:seco|}nd|#0}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out _, out ImmutableArray<int> _, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("#0", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 10), y);
        }

        [Fact]
        public void OverlappingSpans3C()
        {
            var markup = "{|#0:first {|y:seco|#0}nd|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out _, out ImmutableArray<int> _, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("#0", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 10), x);

            Assert.True(spans.TryGetValue("y", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 12), y);
        }

        [Fact]
        public void OverlappingSpans4A()
        {
            var markup = "{|x:first {|#0:seco|}nd|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out _, out ImmutableArray<int> _, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("#0", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 10), y);
        }

        [Fact]
        public void OverlappingSpans4B()
        {
            var markup = "{|x:first {|#0:seco|#0}nd|}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out _, out ImmutableArray<int> _, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 12), x);

            Assert.True(spans.TryGetValue("#0", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 10), y);
        }

        [Fact]
        public void OverlappingSpans4C()
        {
            var markup = "{|x:first {|#0:seco|}nd|#0}";
            TestFileMarkupParser.GetPositionsAndSpans(markup, out _, out ImmutableArray<int> _, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
            Assert.Equal(2, spans.Count);

            Assert.True(spans.TryGetValue("x", out var xs));
            var x = Assert.Single(xs);
            Assert.Equal(TextSpan.FromBounds(0, 10), x);

            Assert.True(spans.TryGetValue("#0", out var ys));
            var y = Assert.Single(ys);
            Assert.Equal(TextSpan.FromBounds(6, 12), y);
        }
    }
}
