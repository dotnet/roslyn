// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UtilityTest;

public sealed class FormattingRangeHelperTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33560")]
    public void TestAreTwoTokensOnSameLineTrue()
    {
        var root = SyntaxFactory.ParseSyntaxTree("{Foo();}").GetRoot();
        var token1 = root.GetFirstToken();
        var token2 = root.GetLastToken();

        Assert.True(FormattingRangeHelper.AreTwoTokensOnSameLine(token1, token2));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33560")]
    public void TestAreTwoTokensOnSameLineFalse()
    {
        var root = SyntaxFactory.ParseSyntaxTree("{Fizz();\nBuzz();}").GetRoot();
        var token1 = root.GetFirstToken();
        var token2 = root.GetLastToken();

        Assert.False(FormattingRangeHelper.AreTwoTokensOnSameLine(token1, token2));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33560")]
    public void TestAreTwoTokensOnSameLineWithEqualTokens()
    {
        var token = SyntaxFactory.ParseSyntaxTree("else\nFoo();").GetRoot().GetFirstToken();

        Assert.True(FormattingRangeHelper.AreTwoTokensOnSameLine(token, token));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33560")]
    public void TestAreTwoTokensOnSameLineWithEqualTokensWithoutSyntaxTree()
    {
        var token = SyntaxFactory.ParseToken("else");

        Assert.True(FormattingRangeHelper.AreTwoTokensOnSameLine(token, token));
    }
}
