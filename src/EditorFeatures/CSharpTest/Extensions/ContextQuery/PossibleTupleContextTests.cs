// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class PossibleTupleContextTests : AbstractContextTests
{
    protected override void CheckResult(bool validLocation, int position, SyntaxTree syntaxTree)
    {
        var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, CancellationToken.None);
        var isPossibleTupleContext = syntaxTree.IsPossibleTupleContext(leftToken, position);

        Assert.Equal(validLocation, isPossibleTupleContext);
    }

    private void VerifyMultipleContexts(string x)
    {
        VerifyTrue(x);
        VerifyTrue(AddInsideClass(x));
        VerifyTrue(AddInsideMethod(x));
    }

    [Fact]
    public void Test1()
        => VerifyMultipleContexts(@"((a, b) $$");

    [Fact]
    public void Test2()
        => VerifyMultipleContexts(@"(xyz, (a, b) $$");

    [Fact]
    public void Test3()
        => VerifyMultipleContexts(@"(a $$");

    [Fact]
    public void Test4()
        => VerifyMultipleContexts(@"(a, b $$");

    [Fact]
    public void Test5()
        => VerifyMultipleContexts(@"($$");

    [Fact]
    public void Test6()
        => VerifyMultipleContexts(@"(a, $$");

    [Fact]
    public void Test7()
        => VerifyMultipleContexts(@"(a.b $$");

    [Fact]
    public void Test8()
        => VerifyMultipleContexts(@"(a, a.b $$");

    [Fact]
    public void Test9()
        => VerifyTrue(@"class C : I<($$");

    [Fact]
    public void Test10()
        => VerifyTrue(@"class C : I<(a, $$");

    [Fact]
    public void Test11()
        => VerifyTrue(AddInsideMethod(@"(var $$)"));

    [Fact]
    public void Test12()
        => VerifyTrue(AddInsideMethod(@"(var a, var $$)"));

    [Fact]
    public void Test13()
        => VerifyTrue(AddInsideMethod(@"var str = (($$)items) as string;"));

    [Fact]
    public void False1()
        => VerifyFalse(@"$$");

    [Fact]
    public void False2()
        => VerifyFalse(AddInsideMethod(@"(int) $$"));

    [Fact]
    public void False3()
        => VerifyFalse(AddInsideMethod(@"(Goo()) $$"));
}
