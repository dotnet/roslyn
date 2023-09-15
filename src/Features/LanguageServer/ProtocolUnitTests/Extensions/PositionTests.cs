// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Extensions
{
    public class PositionTests
    {
        [Fact]
        public void CompareTo_EqualPositions_ReturnsZero()
        {
            // Arrange
            var position = new Position(1, 2);
            var other = new Position(1, 2);

            // Act
            var result = position.CompareTo(other);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CompareTo_GreaterLineNumber_ReturnsLessThanZero()
        {
            // Arrange
            var position = new Position(2, 2);
            var other = new Position(1, 2);

            // Act
            var result = position.CompareTo(other);

            // Assert
            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_GreaterCharacterIndex_ReturnsLessThanZero()
        {
            // Arrange
            var position = new Position(1, 3);
            var other = new Position(1, 2);

            // Act
            var result = position.CompareTo(other);

            // Assert
            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_LowerLineNumber_ReturnsGreaterThanZero()
        {
            // Arrange
            var position = new Position(1, 2);
            var other = new Position(2, 2);

            // Act
            var result = position.CompareTo(other);

            // Assert
            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_LowerCharacterIndex_ReturnsGreaterThanZero()
        {
            // Arrange
            var position = new Position(1, 2);
            var other = new Position(1, 3);

            // Act
            var result = position.CompareTo(other);

            // Assert
            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_NullParameter_ThrowArgumentNullException()
        {
            // Arrange
            var position = new Position(1, 2);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => position.CompareTo(null!));
        }
    }
}
