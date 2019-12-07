// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public void Starting_before_start_and_ending_before_start()
        {
            Assert.Equal(
                new[] { LongSpan },
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), RightBeforeStart(LongSpan))));
        }

        [Fact]
        public void Starting_before_start_and_ending_at_start()
        {
            Assert.Equal(
                new[] { LongSpan },
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), AtStart(LongSpan))));
        }

        [Fact]
        public void Starting_before_start_and_ending_after_start()
        {
            Assert.Equal(
                new[] { TextSpan.FromBounds(RightAfterStart(LongSpan), AtEnd(LongSpan)) },
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), RightAfterStart(LongSpan))));
        }

        [Fact]
        public void Starting_before_start_and_ending_before_end()
        {
            Assert.Equal(
                new[] { TextSpan.FromBounds(RightBeforeEnd(LongSpan), AtEnd(LongSpan)) },
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), RightBeforeEnd(LongSpan))));
        }

        [Fact]
        public void Starting_before_start_and_ending_at_end()
        {
            Assert.Empty(
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), AtEnd(LongSpan))));
        }

        [Fact]
        public void Starting_before_start_and_ending_after_end()
        {
            Assert.Empty(
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void Starting_at_start_and_ending_at_start()
        {
            Assert.Equal(
                new[] { LongSpan },
                LongSpan.Subtract(TextSpan.FromBounds(AtStart(LongSpan), AtStart(LongSpan))));
        }

        [Fact]
        public void Starting_at_start_and_ending_after_start()
        {
            Assert.Equal(
                new[] { TextSpan.FromBounds(RightAfterStart(LongSpan), AtEnd(LongSpan)) },
                LongSpan.Subtract(TextSpan.FromBounds(AtStart(LongSpan), RightAfterStart(LongSpan))));
        }

        [Fact]
        public void Starting_at_start_and_ending_before_end()
        {
            Assert.Equal(
                new[] { TextSpan.FromBounds(RightBeforeEnd(LongSpan), AtEnd(LongSpan)) },
                LongSpan.Subtract(TextSpan.FromBounds(AtStart(LongSpan), RightBeforeEnd(LongSpan))));
        }

        [Fact]
        public void Starting_at_start_and_ending_at_end()
        {
            Assert.Empty(
                LongSpan.Subtract(TextSpan.FromBounds(AtStart(LongSpan), AtEnd(LongSpan))));
        }

        [Fact]
        public void Starting_at_start_and_ending_after_end()
        {
            Assert.Empty(
                LongSpan.Subtract(TextSpan.FromBounds(AtStart(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void Starting_after_start_and_ending_after_start()
        {
            Assert.Equal(
                new[] { LongSpan },
                LongSpan.Subtract(TextSpan.FromBounds(RightAfterStart(LongSpan), RightAfterStart(LongSpan))));
        }

        [Fact]
        public void Starting_after_start_and_ending_before_end()
        {
            Assert.Equal(
                new[]
                {
                    TextSpan.FromBounds(AtStart(LongSpan), RightAfterStart(LongSpan)),
                    TextSpan.FromBounds(RightBeforeEnd(LongSpan), AtEnd(LongSpan))
                },
                LongSpan.Subtract(TextSpan.FromBounds(RightAfterStart(LongSpan), RightBeforeEnd(LongSpan))));
        }

        [Fact]
        public void Starting_after_start_and_ending_at_end()
        {
            Assert.Equal(
                new[] { TextSpan.FromBounds(AtStart(LongSpan), RightAfterStart(LongSpan)) },
                LongSpan.Subtract(TextSpan.FromBounds(RightAfterStart(LongSpan), AtEnd(LongSpan))));
        }

        [Fact]
        public void Starting_after_start_and_ending_after_end()
        {
            Assert.Equal(
                new[] { TextSpan.FromBounds(AtStart(LongSpan), RightAfterStart(LongSpan)) },
                LongSpan.Subtract(TextSpan.FromBounds(RightAfterStart(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void Starting_before_end_and_ending_before_end()
        {
            Assert.Equal(
                new[] { LongSpan },
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeEnd(LongSpan), RightBeforeEnd(LongSpan))));
        }

        [Fact]
        public void Starting_before_end_and_ending_at_end()
        {
            Assert.Equal(
                new[] { TextSpan.FromBounds(AtStart(LongSpan), RightBeforeEnd(LongSpan)) },
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeEnd(LongSpan), AtEnd(LongSpan))));
        }

        [Fact]
        public void Starting_before_end_and_ending_after_end()
        {
            Assert.Equal(
                new[] { TextSpan.FromBounds(AtStart(LongSpan), RightBeforeEnd(LongSpan)) },
                LongSpan.Subtract(TextSpan.FromBounds(RightBeforeEnd(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void Starting_at_end_and_ending_at_end()
        {
            Assert.Equal(
                new[] { LongSpan },
                LongSpan.Subtract(TextSpan.FromBounds(AtEnd(LongSpan), AtEnd(LongSpan))));
        }

        [Fact]
        public void Starting_at_end_and_ending_after_end()
        {
            Assert.Equal(
                new[] { LongSpan },
                LongSpan.Subtract(TextSpan.FromBounds(AtEnd(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void Starting_after_end_and_ending_after_end()
        {
            Assert.Equal(
                new[] { LongSpan },
                LongSpan.Subtract(TextSpan.FromBounds(RightAfterEnd(LongSpan), RightAfterEnd(LongSpan))));
        }

        [Fact]
        public void Unit_span_starting_before_start_and_ending_before_start()
        {
            Assert.Equal(
                new[] { UnitSpan },
                UnitSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(UnitSpan), RightBeforeStart(UnitSpan))));
        }

        [Fact]
        public void Unit_span_starting_before_start_and_ending_at_start()
        {
            Assert.Equal(
                new[] { UnitSpan },
                UnitSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(UnitSpan), AtStart(UnitSpan))));
        }

        [Fact]
        public void Unit_span_starting_before_start_and_ending_at_end()
        {
            Assert.Empty(
                UnitSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(UnitSpan), AtEnd(UnitSpan))));
        }

        [Fact]
        public void Unit_span_starting_before_start_and_ending_after_end()
        {
            Assert.Empty(
                UnitSpan.Subtract(TextSpan.FromBounds(RightBeforeStart(UnitSpan), RightAfterEnd(UnitSpan))));
        }

        [Fact]
        public void Unit_span_starting_at_start_and_ending_at_start()
        {
            Assert.Equal(
                new[] { UnitSpan },
                UnitSpan.Subtract(TextSpan.FromBounds(AtStart(UnitSpan), AtStart(UnitSpan))));
        }

        [Fact]
        public void Unit_span_starting_at_start_and_ending_at_end()
        {
            Assert.Empty(
                UnitSpan.Subtract(TextSpan.FromBounds(AtStart(UnitSpan), AtEnd(UnitSpan))));
        }

        [Fact]
        public void Unit_span_starting_at_start_and_ending_after_end()
        {
            Assert.Empty(
                UnitSpan.Subtract(TextSpan.FromBounds(AtStart(UnitSpan), RightAfterEnd(UnitSpan))));
        }

        [Fact]
        public void Unit_span_starting_at_end_and_ending_at_end()
        {
            Assert.Equal(
                new[] { UnitSpan },
                UnitSpan.Subtract(TextSpan.FromBounds(AtEnd(UnitSpan), AtEnd(UnitSpan))));
        }

        [Fact]
        public void Unit_span_starting_at_end_and_ending_after_end()
        {
            Assert.Equal(
                new[] { UnitSpan },
                UnitSpan.Subtract(TextSpan.FromBounds(AtEnd(UnitSpan), RightAfterEnd(UnitSpan))));
        }

        [Fact]
        public void Unit_span_starting_after_end_and_ending_after_end()
        {
            Assert.Equal(
                new[] { UnitSpan },
                UnitSpan.Subtract(TextSpan.FromBounds(RightAfterEnd(UnitSpan), RightAfterEnd(UnitSpan))));
        }

        [Fact]
        public void Empty_span_starting_before_start_and_ending_before_start()
        {
            Assert.Equal(
                new[] { EmptySpan },
                EmptySpan.Subtract(TextSpan.FromBounds(RightBeforeStart(EmptySpan), RightBeforeStart(EmptySpan))));
        }

        [Fact]
        public void Empty_span_starting_before_start_and_ending_at_span()
        {
            Assert.Empty(
                EmptySpan.Subtract(TextSpan.FromBounds(RightBeforeStart(EmptySpan), EmptySpan.Start)));
        }

        [Fact]
        public void Empty_span_starting_before_start_and_ending_after_end()
        {
            Assert.Empty(
                EmptySpan.Subtract(TextSpan.FromBounds(RightBeforeStart(EmptySpan), RightAfterEnd(EmptySpan))));
        }

        [Fact]
        public void Empty_span_starting_at_span_and_ending_at_span()
        {
            Assert.Empty(
                EmptySpan.Subtract(TextSpan.FromBounds(EmptySpan.Start, EmptySpan.Start)));
        }

        [Fact]
        public void Empty_span_starting_at_span_and_ending_after_end()
        {
            Assert.Empty(
                EmptySpan.Subtract(TextSpan.FromBounds(EmptySpan.Start, RightAfterEnd(EmptySpan))));
        }

        [Fact]
        public void Empty_span_starting_after_end_and_ending_after_end()
        {
            Assert.Equal(
                new[] { EmptySpan },
                EmptySpan.Subtract(TextSpan.FromBounds(RightAfterEnd(EmptySpan), RightAfterEnd(EmptySpan))));
        }
    }
}
