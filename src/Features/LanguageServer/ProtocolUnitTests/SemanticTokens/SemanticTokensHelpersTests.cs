// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SemanticTokens
{
    public class SemanticTokensHelpersTests
    {
        [Fact]
        public void StitchSemanticTokenResponsesTogether_OnEmptyInput_ReturnsEmptyResponseData()
        {
            // Arrange
            var responseData = Array.Empty<int[]>();

            // Act
            var result = SemanticTokensHelpers.StitchSemanticTokenResponsesTogether(responseData);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void StitchSemanticTokenResponsesTogether_ReturnsCombinedResponseData()
        {
            // Arrange
            var responseData = new int[][] {
                new int[] { 0, 0, 0, 0, 0,
                            1, 0, 0, 0, 0,
                            1, 0, 0, 0, 0,
                            0, 5, 0, 0, 0,
                            0, 3, 0, 0, 0,
                            2, 2, 0, 0, 0,
                            0, 3, 0, 0, 0 },
                new int[] { 10, 0, 0, 0, 0,
                            1, 0, 0, 0, 0,
                            1, 0, 0, 0, 0,
                            0, 5, 0, 0, 0,
                            0, 3, 0, 0, 0,
                            2, 2, 0, 0, 0,
                            0, 3, 0, 0, 0 },
                new int[] { 14, 7, 0, 0, 0,
                            1, 0, 0, 0, 0,
                            1, 0, 0, 0, 0,
                            0, 5, 0, 0, 0,
                            0, 3, 0, 0, 0,
                            2, 2, 0, 0, 0,
                            0, 3, 0, 0, 0 },
                };

            var expectedResponseData = new int[] {
                0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 3, 0, 0, 0, 2, 2, 0, 0, 0, 0, 3, 0, 0, 0,
                6, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 3, 0, 0, 0, 2, 2, 0, 0, 0, 0, 3, 0, 0, 0,
                0, 2, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 3, 0, 0, 0, 2, 2, 0, 0, 0, 0, 3, 0, 0, 0
            };

            // Act
            var result = SemanticTokensHelpers.StitchSemanticTokenResponsesTogether(responseData);

            // Assert
            Assert.Equal(expectedResponseData, result);
        }
    }
}
