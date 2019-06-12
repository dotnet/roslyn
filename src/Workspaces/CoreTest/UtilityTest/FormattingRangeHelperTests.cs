// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UtilityTest
{
    public class FormattingRangeHelperTests
    {
        [Fact]
        public void TestAreTwoTokensOnSameLineTrue()
        {
            var root = SyntaxFactory.ParseSyntaxTree("{Foo();}").GetRoot();
            var token1 = root.GetFirstToken();
            var token2 = root.GetLastToken();

            Assert.True(FormattingRangeHelper.AreTwoTokensOnSameLine(token1, token2));
        }

        [Fact]
        public void TestAreTwoTokensOnSameLineFalse()
        {
            var root = SyntaxFactory.ParseSyntaxTree("{Fizz();\nBuzz();}").GetRoot();
            var token1 = root.GetFirstToken();
            var token2 = root.GetLastToken();

            Assert.False(FormattingRangeHelper.AreTwoTokensOnSameLine(token1, token2));
        }

        [Fact]
        public void TestAreTwoTokensOnSameLineWithEqualTokens()
        {
            var token = SyntaxFactory.ParseSyntaxTree("else\nFoo();").GetRoot().GetFirstToken();

            Assert.True(FormattingRangeHelper.AreTwoTokensOnSameLine(token, token));
        }

        [Fact]
        public void TestAreTwoTokensOnSameLineWithEqualTokensWithoutSyntaxTree()
        {
            var token = SyntaxFactory.ParseToken("else");

            Assert.True(FormattingRangeHelper.AreTwoTokensOnSameLine(token, token));
        }
    }
}
