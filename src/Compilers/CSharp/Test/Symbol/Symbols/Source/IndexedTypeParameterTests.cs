// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IndexedTypeParameterTests
    {
        [Fact]
        public void TestTake()
        {
            var zero = IndexedTypeParameterSymbol.TakeSymbols(0);
            Assert.Equal(0, zero.Length);

            var five = IndexedTypeParameterSymbol.TakeSymbols(5);
            Assert.Equal(5, five.Length);
            Assert.Equal(five[0], IndexedTypeParameterSymbol.GetTypeParameter(0));
            Assert.Equal(five[1], IndexedTypeParameterSymbol.GetTypeParameter(1));
            Assert.Equal(five[2], IndexedTypeParameterSymbol.GetTypeParameter(2));
            Assert.Equal(five[3], IndexedTypeParameterSymbol.GetTypeParameter(3));
            Assert.Equal(five[4], IndexedTypeParameterSymbol.GetTypeParameter(4));

            var fifty = IndexedTypeParameterSymbol.TakeSymbols(50);
            Assert.Equal(50, fifty.Length);

            // prove they are all unique
            var set = new HashSet<TypeParameterSymbol>(fifty);
            Assert.Equal(50, set.Count);

            var fiveHundred = IndexedTypeParameterSymbol.TakeSymbols(500);
            Assert.Equal(500, fiveHundred.Length);
        }
    }
}
