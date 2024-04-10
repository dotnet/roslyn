// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Symbols;
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

        [Theory]
        [InlineData("DisplayClass10_20", 10, 0, 20, 0)]
        [InlineData("1#2_3#4", 1, 2, 3, 4)]
        [InlineData("123_4#5", 123, 0, 4, 5)]
        [InlineData("1#2_345", 1, 2, 345, 0)]
        [InlineData("1#0_2#0", 1, 0, 2, 0)]
        [InlineData("0_1", 0, 0, 1, 0)]
        [InlineData("L1|6_0", 6, 0, 0, 0)]
        [InlineData("L1|1", 1, 0, 0, 0, false)]
        [InlineData("L1|1", 0, 0, 1, 0, true)]
        [InlineData("L1|1#2", 1, 2, 0, 0, false)]
        [InlineData("L1|1#2", 0, 0, 1, 2, true)]
        public void TryParseDebugIds(string metadataName, int methodIdOrdinal, int methodIdGeneration, int entityIdOrdinal, int entityIdGeneration, bool isMethodIdOptional = false)
        {
            Assert.True(CommonGeneratedNames.TryParseDebugIds(metadataName.AsSpan(), GeneratedNameConstants.IdSeparator, isMethodIdOptional, out var actualMethodId, out var actualEntityId));
            Assert.Equal(new DebugId(methodIdOrdinal, methodIdGeneration), actualMethodId);
            Assert.Equal(new DebugId(entityIdOrdinal, entityIdGeneration), actualEntityId);
        }

        [Theory]
        [InlineData("L1|0_99999999999999999999999")]
        [InlineData("L1|99999999999999999999999_0")]
        [InlineData("L1|6_")]
        [InlineData("L1|_6")]
        [InlineData("1#2#3#4")]
        [InlineData("1#2#3_4")]
        [InlineData("1_2_3")]
        [InlineData("_2_3")]
        [InlineData("##")]
        [InlineData("#")]
        [InlineData("#1")]
        [InlineData("1#")]
        [InlineData("1#_2")]
        [InlineData("1_#2")]
        [InlineData("_")]
        [InlineData("__")]
        [InlineData("")]
        public void TryParseDebugIds_Errors(string metadataName)
        {
            Assert.False(CommonGeneratedNames.TryParseDebugIds(metadataName.AsSpan(), GeneratedNameConstants.IdSeparator, isMethodIdOptional: false, out _, out _));
        }
    }
}
