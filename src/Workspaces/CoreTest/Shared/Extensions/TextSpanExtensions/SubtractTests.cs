// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Shared.Extensions.TextSpanExtensions
{
    public class SubtractTests : TestBase
    {
        // There are several interesting locations relative to the original span in which the excluded span may start or end:
        // - Right before the start of the original span
        // - On the start of the original span
        // - Right after the start of the original span
        // - Right before the end of the original span
        // - On the end of the original span
        // - Right after the end of the original span
        //
        // And several lengths of original span: 0, 1, and longer.
        //
        // This file attempts to test every valid combination.

        private static TextSpan LongSpan { get; } = TextSpan.FromBounds(10, 20);
        private static TextSpan UnitSpan { get; } = TextSpan.FromBounds(10, 11);
        private static TextSpan EmptySpan { get; } = TextSpan.FromBounds(10, 10);

        private static int RightBeforeStart(TextSpan span) => span.Start - 1;
        private static int AtStart(TextSpan span) => span.Start;
        private static int RightAfterStart(TextSpan span) => span.Start + 1;
        private static int RightBeforeEnd(TextSpan span) => span.End - 1;
        private static int AtEnd(TextSpan span) => span.End;
        private static int RightAfterEnd(TextSpan span) => span.End + 1;

        [Fact]
        public void StartingBeforeStartAndEndingBeforeStart()
        {
            Assert.Equal(
                [LongSpan],
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), RightBeforeStart(LongSpan))));
        }

        [Fact]
        public void StartingBeforeStartAndEndingAtStart()
        {
            Assert.Equal(
                [LongSpan],
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), AtStart(LongSpan))));
        }

        [Fact]
        public void StartingBeforeStartAndEndingAfterStart()
        {
            Assert.Equal(
                [TextSpan.FromBounds(RightAfterStart(LongSpan), AtEnd(LongSpan))],
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), RightAfterStart(LongSpan))));
        }

        [Fact]
        public void StartingBeforeStartAndEndingBeforeEnd()
        {
            Assert.Equal(
                [TextSpan.FromBounds(RightBeforeEnd(LongSpan), AtEnd(LongSpan))],
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), RightBeforeEnd(LongSpan))));
        }

        [Fact]
        public void StartingBeforeStartAndEndingAtEnd()
        {
            Assert.Empty(
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), AtEnd(LongSpan))));
        }

        [Fact]
        public void StartingBeforeStartAndEndingAfterEnd()
        {
            Assert.Empty(
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void StartingAtStartAndEndingAtStart()
        {
            Assert.Equal(
                [LongSpan],
                LongSpan.Subtract(TextSpan.FromBounds(AtStart(LongSpan), AtStart(LongSpan))));
        }

        [Fact]
        public void StartingAtStartAndEndingAfterStart()
        {
            Assert.Equal(
                [TextSpan.FromBounds(RightAfterStart(LongSpan), AtEnd(LongSpan))],
                LongSpan.Subtract(TextSpan.FromBounds(AtStart(LongSpan), RightAfterStart(LongSpan))));
        }

        [Fact]
        public void StartingAtStartAndEndingBeforeEnd()
        {
            Assert.Equal(
                [TextSpan.FromBounds(RightBeforeEnd(LongSpan), AtEnd(LongSpan))],
                LongSpan.Subtract(TextSpan.FromBounds(AtStart(LongSpan), RightBeforeEnd(LongSpan))));
        }

        [Fact]
        public void StartingAtStartAndEndingAtEnd()
        {
            Assert.Empty(
                LongSpan.Subtract(TextSpan.FromBounds(AtStart(LongSpan), AtEnd(LongSpan))));
        }

        [Fact]
        public void StartingAtStartAndEndingAfterEnd()
        {
            Assert.Empty(
                LongSpan.Subtract(TextSpan.FromBounds(AtStart(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void StartingAfterStartAndEndingAfterStart()
        {
            Assert.Equal(
                [LongSpan],
                LongSpan.Subtract(TextSpan.FromBounds(RightAfterStart(LongSpan), RightAfterStart(LongSpan))));
        }

        [Fact]
        public void StartingAfterStartAndEndingBeforeEnd()
        {
            Assert.Equal(
                [
                    TextSpan.FromBounds(AtStart(LongSpan), RightAfterStart(LongSpan)),
                    TextSpan.FromBounds(RightBeforeEnd(LongSpan), AtEnd(LongSpan))
                ],
                LongSpan.Subtract(TextSpan.FromBounds(RightAfterStart(LongSpan), RightBeforeEnd(LongSpan))));
        }

        [Fact]
        public void StartingAfterStartAndEndingAtEnd()
        {
            Assert.Equal(
                [TextSpan.FromBounds(AtStart(LongSpan), RightAfterStart(LongSpan))],
                LongSpan.Subtract(TextSpan.FromBounds(RightAfterStart(LongSpan), AtEnd(LongSpan))));
        }

        [Fact]
        public void StartingAfterStartAndEndingAfterEnd()
        {
            Assert.Equal(
                [TextSpan.FromBounds(AtStart(LongSpan), RightAfterStart(LongSpan))],
                LongSpan.Subtract(TextSpan.FromBounds(RightAfterStart(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void StartingBeforeEndAndEndingBeforeEnd()
        {
            Assert.Equal(
                [LongSpan],
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeEnd(LongSpan), RightBeforeEnd(LongSpan))));
        }

        [Fact]
        public void StartingBeforeEndAndEndingAtEnd()
        {
            Assert.Equal(
                [TextSpan.FromBounds(AtStart(LongSpan), RightBeforeEnd(LongSpan))],
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeEnd(LongSpan), AtEnd(LongSpan))));
        }

        [Fact]
        public void StartingBeforeEndAndEndingAfterEnd()
        {
            Assert.Equal(
                [TextSpan.FromBounds(AtStart(LongSpan), RightBeforeEnd(LongSpan))],
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeEnd(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void StartingAtEndAndEndingAtEnd()
        {
            Assert.Equal(
                [LongSpan],
                LongSpan.Subtract(TextSpan.FromBounds(AtEnd(LongSpan), AtEnd(LongSpan))));
        }

        [Fact]
        public void StartingAtEndAndEndingAfterEnd()
        {
            Assert.Equal(
                [LongSpan],
                LongSpan.Subtract(TextSpan.FromBounds(AtEnd(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void StartingAfterEndAndEndingAfterEnd()
        {
            Assert.Equal(
                [LongSpan],
                LongSpan.Subtract(TextSpan.FromBounds(RightAfterEnd(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void UnitSpanStartingBeforeStartAndEndingBeforeStart()
        {
            Assert.Equal(
                [UnitSpan],
                UnitSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(UnitSpan), RightBeforeStart(UnitSpan))));
        }

        [Fact]
        public void UnitSpanStartingBeforeStartAndEndingAtStart()
        {
            Assert.Equal(
                [UnitSpan],
                UnitSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(UnitSpan), AtStart(UnitSpan))));
        }

        [Fact]
        public void UnitSpanStartingBeforeStartAndEndingAtEnd()
        {
            Assert.Empty(
                UnitSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(UnitSpan), AtEnd(UnitSpan))));
        }

        [Fact]
        public void UnitSpanStartingBeforeStartAndEndingAfterEnd()
        {
            Assert.Empty(
                UnitSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(UnitSpan), RightAfterEnd(UnitSpan))));
        }

        [Fact]
        public void UnitSpanStartingAtStartAndEndingAtStart()
        {
            Assert.Equal(
                [UnitSpan],
                UnitSpan.Subtract(TextSpan.FromBounds(AtStart(UnitSpan), AtStart(UnitSpan))));
        }

        [Fact]
        public void UnitSpanStartingAtStartAndEndingAtEnd()
        {
            Assert.Empty(
                UnitSpan.Subtract(TextSpan.FromBounds(AtStart(UnitSpan), AtEnd(UnitSpan))));
        }

        [Fact]
        public void UnitSpanStartingAtStartAndEndingAfterEnd()
        {
            Assert.Empty(
                UnitSpan.Subtract(TextSpan.FromBounds(AtStart(UnitSpan), RightAfterEnd(UnitSpan))));
        }

        [Fact]
        public void UnitSpanStartingAtEndAndEndingAtEnd()
        {
            Assert.Equal(
                [UnitSpan],
                UnitSpan.Subtract(TextSpan.FromBounds(AtEnd(UnitSpan), AtEnd(UnitSpan))));
        }

        [Fact]
        public void UnitSpanStartingAtEndAndEndingAfterEnd()
        {
            Assert.Equal(
                [UnitSpan],
                UnitSpan.Subtract(TextSpan.FromBounds(AtEnd(UnitSpan), RightAfterEnd(UnitSpan))));
        }

        [Fact]
        public void UnitSpanStartingAfterEndAndEndingAfterEnd()
        {
            Assert.Equal(
                [UnitSpan],
                UnitSpan.Subtract(TextSpan.FromBounds(RightAfterEnd(UnitSpan), RightAfterEnd(UnitSpan))));
        }

        [Fact]
        public void EmptySpanStartingBeforeStartAndEndingBeforeStart()
        {
            Assert.Equal(
                [EmptySpan],
                EmptySpan.Subtract(TextSpan.FromBounds(RightBeforeStart(EmptySpan), RightBeforeStart(EmptySpan))));
        }

        [Fact]
        public void EmptySpanStartingBeforeStartAndEndingAtSpan()
        {
            Assert.Empty(
                EmptySpan.Subtract(TextSpan.FromBounds(RightBeforeStart(EmptySpan), EmptySpan.Start)));
        }

        [Fact]
        public void EmptySpanStartingBeforeStartAndEndingAfterEnd()
        {
            Assert.Empty(
                EmptySpan.Subtract(TextSpan.FromBounds(RightBeforeStart(EmptySpan), RightAfterEnd(EmptySpan))));
        }

        [Fact]
        public void EmptySpanStartingAtSpanAndEndingAtSpan()
        {
            Assert.Empty(
                EmptySpan.Subtract(TextSpan.FromBounds(EmptySpan.Start, EmptySpan.Start)));
        }

        [Fact]
        public void EmptySpanStartingAtSpanAndEndingAfterEnd()
        {
            Assert.Empty(
                EmptySpan.Subtract(TextSpan.FromBounds(EmptySpan.Start, RightAfterEnd(EmptySpan))));
        }

        [Fact]
        public void EmptySpanStartingAfterEndAndEndingAfterEnd()
        {
            Assert.Equal(
                [EmptySpan],
                EmptySpan.Subtract(TextSpan.FromBounds(RightAfterEnd(EmptySpan), RightAfterEnd(EmptySpan))));
        }
    }
}
