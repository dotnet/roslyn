// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Extensions
{
    public class RangeExtensionsTests
    {
        [Fact]
        public void CompareTo_StartAndEndAreSame_ReturnsZero()
        {
            // Arrange
            var range1 = new Range() { Start = new Position(1, 2), End = new Position(3, 4) };
            var range2 = new Range() { Start = new Position(1, 2), End = new Position(3, 4) };

            // Act
            var result = range1.CompareTo(range2);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CompareTo_StartOfThisRangeIsBeforeOther_ReturnsNegative()
        {
            // Arrange
            var range1 = new Range() { Start = new Position(1, 2), End = new Position(3, 4) };
            var range2 = new Range() { Start = new Position(2, 2), End = new Position(3, 4) };

            // Act
            var result = range1.CompareTo(range2);

            // Assert
            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_EndOfThisRangeIsBeforeOther_ReturnsNegative()
        {
            // Arrange
            var range1 = new Range() { Start = new Position(1, 2), End = new Position(3, 4) };
            var range2 = new Range() { Start = new Position(1, 2), End = new Position(4, 4) };

            // Act
            var result = range1.CompareTo(range2);

            // Assert
            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_StartOfThisRangeIsAfterOther_ReturnsPositive()
        {
            // Arrange
            var range1 = new Range() { Start = new Position(2, 2), End = new Position(3, 4) };
            var range2 = new Range() { Start = new Position(1, 2), End = new Position(3, 4) };

            // Act
            var result = range1.CompareTo(range2);

            // Assert
            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_EndOfThisRangeIsAfterOther_ReturnsPositive()
        {
            // Arrange
            var range1 = new Range() { Start = new Position(1, 2), End = new Position(4, 4) };
            var range2 = new Range() { Start = new Position(1, 2), End = new Position(3, 4) };

            // Act
            var result = range1.CompareTo(range2);

            // Assert
            Assert.True(result > 0);
        }
    }
}
