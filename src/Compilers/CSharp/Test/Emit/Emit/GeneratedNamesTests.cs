// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class GeneratedNamesTests
    {
        [Theory]
        [InlineData("<>A", true, 0, 0)]
        [InlineData("<>A{00010000}`8", true, 0, 8)]
        [InlineData("<>A{00010000}#2`8", true, 2, 8)]
        [InlineData("<>A{00010000,00000000}`16", true, 0, 16)]
        [InlineData("<>A{00010000,00000000}#4`16", true, 4, 16)]
        [InlineData("<>A`5", true, 0, 5)]
        [InlineData("<>A`18", true, 0, 18)]
        [InlineData("<>F`1", false, 0, 0)]
        [InlineData("<>F`6", false, 0, 5)]
        [InlineData("<>F`19", false, 0, 18)]
        [InlineData("<>F#3`19", false, 3, 18)]
        public void TryParseSynthesizedDelegateName_Success(string name, bool returnsVoid, int generation, int parameterCount)
        {
            Assert.True(GeneratedNames.TryParseSynthesizedDelegateName(name, out var actualByRefs, out var actualReturnsVoid, out var actualGeneration, out var actualParameterCount));

            Assert.Equal(returnsVoid, actualReturnsVoid);
            Assert.Equal(generation, actualGeneration);
            Assert.Equal(parameterCount, actualParameterCount);


            // We need to strip arity in order to validate round-tripping
            name = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(name, out _);
            Assert.Equal(name, GeneratedNames.MakeSynthesizedDelegateName(actualByRefs, actualReturnsVoid, actualGeneration));
        }

        [Theory]
        [InlineData("<>D")]
        [InlineData("<>A{")]
        [InlineData("<>A00}")]
        [InlineData("<>ABCDEF")]
        [InlineData("<>A{Z}")]
        [InlineData("<>A#F")]
        [InlineData("<>A{}")]
        [InlineData("<>A{,}")]
        [InlineData("<>A#")]
        [InlineData("<>A{0,}")]
        [InlineData("<>A{,1}")]
        public void TryParseSynthesizedDelegateName_Failure(string name)
        {
            Assert.False(GeneratedNames.TryParseSynthesizedDelegateName(name, out _, out _, out _, out _));
        }
    }
}
