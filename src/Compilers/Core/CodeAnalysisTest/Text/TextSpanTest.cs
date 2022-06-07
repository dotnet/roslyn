// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    /// <summary>
    /// This is a test class for TextSpan and is intended
    /// to contain all TextSpan Unit Tests
    /// </summary>
    public class TextSpanTest
    {
        [Fact]
        public void Ctor1()
        {
            var span = new TextSpan(0, 42);
            Assert.Equal(0, span.Start);
            Assert.Equal(42, span.Length);
            Assert.Equal(42, span.End);
        }

        [Fact]
        public void Ctor2()
        {
            var span = new TextSpan(1, 40);
            Assert.Equal(1, span.Start);
            Assert.Equal(40, span.Length);
            Assert.Equal(41, span.End);
        }

        /// <summary>
        /// 0 length spans are valid
        /// </summary>
        [Fact]
        public void Ctor3()
        {
            var span = new TextSpan(0, 0);
            Assert.Equal(0, span.Start);
            Assert.Equal(0, span.Length);
        }

        [Fact]
        public void Equals1()
        {
            var s1 = new TextSpan(1, 40);
            var s2 = new TextSpan(1, 40);
            Assert.True(s1.Equals(s2), s1.ToString() + " : " + s2.ToString());
            Assert.True(s1 == s2, s1.ToString() + " : " + s2.ToString());
            Assert.False(s1 != s2, s1.ToString() + " : " + s2.ToString());
            Assert.Equal(s1, s2);
        }

        /// <summary>
        /// Different start values
        /// </summary>
        [Fact]
        public void Equals2()
        {
            var s1 = new TextSpan(1, 40);
            var s2 = new TextSpan(2, 40);
            Assert.False(s1.Equals(s2), s1.ToString() + " : " + s2.ToString());
            Assert.False(s1 == s2, s1.ToString() + " : " + s2.ToString());
            Assert.True(s1 != s2, s1.ToString() + " : " + s2.ToString());
            Assert.NotEqual(s1, s2);
        }

        /// <summary>
        /// Different length values
        /// </summary>
        [Fact]
        public void Equals3()
        {
            var s1 = new TextSpan(1, 5);
            var s2 = new TextSpan(1, 40);
            Assert.False(s1.Equals(s2), s1.ToString() + " : " + s2.ToString());
            Assert.False(s1 == s2, s1.ToString() + " : " + s2.ToString());
            Assert.True(s1 != s2, s1.ToString() + " : " + s2.ToString());
            Assert.NotEqual(s1, s2);
        }

        [Fact]
        public void TextSpan00()
        {
            TextSpan span = new TextSpan(0, 0);
            Assert.Equal(0, span.Start);
            Assert.Equal(0, span.End);
            Assert.Equal(0, span.Length);
            Assert.Equal("[0..0)", span.ToString());
            Assert.True(span.IsEmpty);
        }

        [Fact]
        public void TextSpan01()
        {
            TextSpan span = new TextSpan(0, 1);
            Assert.Equal(0, span.Start);
            Assert.Equal(1, span.End);
            Assert.Equal(1, span.Length);
            Assert.Equal("[0..1)", span.ToString());
            Assert.False(span.IsEmpty);
            span.GetHashCode();
        }

        [Fact]
        public void TextSpan02()
        {
            TextSpan span = new TextSpan(15, 1485);
            Assert.Equal(15, span.Start);
            Assert.Equal(1500, span.End);
            Assert.Equal(1485, span.Length);
            Assert.Equal("[15..1500)", span.ToString());
        }

        [Fact]
        public void TextSpan03()
        {
            TextSpan span = new TextSpan(0, int.MaxValue - 1);
            Assert.Equal(0, span.Start);
            Assert.Equal(int.MaxValue - 1, span.End);
            Assert.Equal(int.MaxValue - 1, span.Length);
        }

        [Fact]
        public void TextSpanContains00()
        {
            TextSpan span = new TextSpan(0, 10);
            Assert.True(span.Contains(3));
            Assert.False(span.Contains(30));
            Assert.False(span.Contains(11));
            Assert.False(span.Contains(-1));
        }

        [Fact]
        public void TextSpanContains01()
        {
            TextSpan span_05_15 = new TextSpan(5, 10);
            TextSpan span_03_10 = new TextSpan(3, 7);
            TextSpan span_10_11 = new TextSpan(10, 1);
            TextSpan span_00_03 = new TextSpan(0, 3);

            // non-overlapping
            Assert.False(span_05_15.Contains(span_00_03));
            Assert.False(span_00_03.Contains(span_05_15));

            // overlap with slop
            Assert.True(span_05_15.Contains(span_10_11));

            // same span
            Assert.True(span_05_15.Contains(span_05_15));

            // partial overlap
            Assert.False(span_05_15.Contains(span_03_10));
            Assert.False(span_03_10.Contains(span_05_15));
        }

        [Fact]
        public void TextSpanContainsEmpty()
        {
            // non-overlapping
            Assert.False(new TextSpan(2, 5).Contains(new TextSpan(0, 0)));
            Assert.False(new TextSpan(2, 5).Contains(new TextSpan(10, 0)));

            // contains
            Assert.True(new TextSpan(2, 5).Contains(new TextSpan(3, 0)));

            // same start
            Assert.True(new TextSpan(2, 5).Contains(new TextSpan(2, 0)));

            // same end
            Assert.True(new TextSpan(2, 5).Contains(new TextSpan(7, 0)));

            // same start and end
            Assert.True(new TextSpan(2, 0).Contains(new TextSpan(2, 0)));
        }

        [Fact]
        public void TextSpanEmptyContains()
        {
            // non-overlapping
            Assert.False(new TextSpan(0, 0).Contains(new TextSpan(2, 5)));
            Assert.False(new TextSpan(10, 0).Contains(new TextSpan(2, 5)));

            // contains
            Assert.False(new TextSpan(3, 0).Contains(new TextSpan(2, 5)));

            // same start
            Assert.False(new TextSpan(2, 0).Contains(new TextSpan(2, 5)));

            // same end
            Assert.False(new TextSpan(7, 0).Contains(new TextSpan(2, 5)));
        }

        [Fact]
        public void TextSpanEquality00()
        {
            TextSpan span1 = new TextSpan(0, 10);
            TextSpan span2 = new TextSpan(0, 10);

            Assert.True(span1.Equals(span2));
            Assert.NotEqual(default, span1);
            Assert.True(span1 == span2, span1.ToString() + " : " + span2.ToString());
            Assert.False(span1 != span2, span1.ToString() + " : " + span2.ToString());

            Assert.True(span2.Equals(span1));
            Assert.NotEqual(default, span2);
            Assert.True(span2 == span1, span2.ToString() + " : " + span1.ToString());
            Assert.False(span2 != span1, span2.ToString() + " : " + span1.ToString());
        }

        [Fact]
        public void TextSpanEquality01()
        {
            TextSpan span1 = new TextSpan(0, 10);
            TextSpan span2 = new TextSpan(0, 11);
            TextSpan span3 = new TextSpan(1, 11);

            Assert.False(span1.Equals(span2));
            Assert.False(span1.Equals(span3));
            Assert.False(span2.Equals(span3));
            Assert.True(span1 != span2, span1.ToString() + " : " + span2.ToString());
            Assert.True(span1 != span3, span1.ToString() + " : " + span3.ToString());
            Assert.True(span2 != span3, span2.ToString() + " : " + span3.ToString());
            Assert.False(span1 == span2, span1.ToString() + " : " + span2.ToString());

            Assert.NotEqual<object>(new string('a', 3), span1);
        }

        [Fact]
        public void TextSpanOverlap00()
        {
            TextSpan span1 = new TextSpan(10, 10); // 10..20
            TextSpan span2 = new TextSpan(5, 5); // 5..10

            Assert.False(span1.OverlapsWith(span2));
            Assert.False(span2.OverlapsWith(span1));
            Assert.Null(span1.Overlap(span2));
            Assert.Null(span2.Overlap(span1));
        }

        [Fact]
        public void TextSpanOverlap01()
        {
            TextSpan span1 = new TextSpan(10, 10); // 10..20
            TextSpan span2 = new TextSpan(5, 2); // 5..7

            Assert.False(span1.OverlapsWith(span2));
            Assert.False(span2.OverlapsWith(span1));
            Assert.Null(span1.Overlap(span2));
            Assert.Null(span2.Overlap(span1));
        }

        [Fact]
        public void TextSpanOverlap02()
        {
            TextSpan span1 = new TextSpan(10, 10); // 10..20
            TextSpan span2 = new TextSpan(5, 10); // 5..15

            Assert.True(span1.OverlapsWith(span2));
            Assert.True(span2.OverlapsWith(span1));
            Assert.Equal(span1.Overlap(span2), new TextSpan(10, 5));
            Assert.Equal(span2.Overlap(span1), new TextSpan(10, 5));
        }

        [Fact]
        public void TextSpanOverlap03()
        {
            TextSpan span1 = new TextSpan(10, 0); // [10, 10)
            TextSpan span2 = new TextSpan(10, 0); // [10, 10)

            Assert.False(span1.OverlapsWith(span2));
            Assert.False(span2.OverlapsWith(span1));
            Assert.Null(span1.Overlap(span2));
            Assert.Null(span2.Overlap(span1));
        }

        [Fact]
        public void TextSpanOverlap04()
        {
            TextSpan span1 = new TextSpan(10, 0);   // [10, 10)
            TextSpan span2 = new TextSpan(5, 10);   // [5, 15)

            Assert.False(span1.OverlapsWith(span2));
            Assert.False(span2.OverlapsWith(span1));
            Assert.Null(span1.Overlap(span2));
            Assert.Null(span2.Overlap(span1));
        }

        [Fact]
        public void TextSpanIntersection00()
        {
            TextSpan span1 = new TextSpan(10, 10); // 10..20
            TextSpan span2 = new TextSpan(5, 5); // 5..10

            Assert.True(span1.IntersectsWith(span2));
            Assert.True(span2.IntersectsWith(span1));
            Assert.Equal(span1.Intersection(span2), new TextSpan(10, 0));
            Assert.Equal(span2.Intersection(span1), new TextSpan(10, 0));
        }

        [Fact]
        public void TextSpanIntersection01()
        {
            TextSpan span1 = new TextSpan(10, 10); // 10..20
            TextSpan span2 = new TextSpan(5, 2); // 5..7

            Assert.False(span1.IntersectsWith(span2));
            Assert.False(span2.IntersectsWith(span1));
            Assert.Null(span1.Intersection(span2));
            Assert.Null(span2.Intersection(span1));
        }

        [Fact]
        public void TextSpanIntersection02()
        {
            TextSpan span1 = new TextSpan(10, 10); // 10..20
            TextSpan span2 = new TextSpan(5, 10); // 5..15

            Assert.True(span1.IntersectsWith(span2));
            Assert.True(span2.IntersectsWith(span1));
            Assert.Equal(span1.Intersection(span2), new TextSpan(10, 5));
            Assert.Equal(span2.Intersection(span1), new TextSpan(10, 5));
        }

        [Fact]
        public void TextSpanIntersection03()
        {
            TextSpan span1 = new TextSpan(10, 0); // [10, 10)
            TextSpan span2 = new TextSpan(10, 0); // [10, 10)

            Assert.True(span1.IntersectsWith(span2));
            Assert.True(span2.IntersectsWith(span1));
            Assert.Equal(span1.Intersection(span2), new TextSpan(10, 0));
            Assert.Equal(span2.Intersection(span1), new TextSpan(10, 0));
        }

        [Fact]
        public void TextSpanIntersection04()
        {
            TextSpan span1 = new TextSpan(2, 5); // [2, 7)
            TextSpan span2 = new TextSpan(7, 5); // [7, 12)

            Assert.True(span1.IntersectsWith(span2));
            Assert.True(span2.IntersectsWith(span1));
            Assert.Equal(span1.Intersection(span2), new TextSpan(7, 0));
            Assert.Equal(span2.Intersection(span1), new TextSpan(7, 0));
        }

        [Fact]
        public void TextSpanIntersectionEmpty01()
        {
            TextSpan span1 = new TextSpan(2, 5); // [2, 7)
            TextSpan span2 = new TextSpan(3, 0); // [3, 3)

            Assert.True(span1.IntersectsWith(span2));
            Assert.True(span2.IntersectsWith(span1));
            Assert.Equal(span1.Intersection(span2), new TextSpan(3, 0));
            Assert.Equal(span2.Intersection(span1), new TextSpan(3, 0));
        }

        [Fact]
        public void TextSpanIntersectionEmpty02()
        {
            TextSpan span1 = new TextSpan(2, 5); // [2, 7)
            TextSpan span2 = new TextSpan(2, 0); // [2, 2)

            Assert.True(span1.IntersectsWith(span2));
            Assert.True(span2.IntersectsWith(span1));
            Assert.Equal(span1.Intersection(span2), new TextSpan(2, 0));
            Assert.Equal(span2.Intersection(span1), new TextSpan(2, 0));
        }

        [Fact]
        public void TextSpanIntersectionEmpty03()
        {
            TextSpan span1 = new TextSpan(2, 5); // [2, 7)
            TextSpan span2 = new TextSpan(7, 0); // [7, 0)

            Assert.True(span1.IntersectsWith(span2));
            Assert.True(span2.IntersectsWith(span1));
            Assert.Equal(span1.Intersection(span2), new TextSpan(7, 0));
            Assert.Equal(span2.Intersection(span1), new TextSpan(7, 0));
        }
    }
}
