// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class IsPossibleDeconstructionDesignationTests : AbstractContextTests
    {
        protected override void CheckResult(bool expected, int position, SyntaxTree syntaxTree)
        {
            var actual = syntaxTree.IsPossibleDeconstructionDesignation(position, CancellationToken.None);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Test1()
            => VerifyTrue(AddInsideMethod(@"(var $$, var y)"));

        [Fact]
        public void WellFormed1()
            => VerifyTrue(AddInsideMethod(@"(var $$, var y) = e;"));

        [Fact]
        public void Test2()
            => VerifyTrue(AddInsideMethod(@"(var x, var $$)"));

        [Fact]
        public void Test3()
            => VerifyTrue(AddInsideMethod(@"var ($$, y)"));

        [Fact]
        public void Test4()
            => VerifyTrue(AddInsideMethod(@"var ($$, y)"));

        [Fact]
        public void Test5()
            => VerifyTrue(AddInsideMethod(@"var ($$)"));

        [Fact]
        public void Test6()
            => VerifyTrue(AddInsideMethod(@"(var $$)"));

        [Fact]
        public void Test7()
            => VerifyTrue(AddInsideMethod(@"(var a, var $$)"));

        [Fact]
        public void Test8()
            => VerifyFalse(AddInsideMethod(@"var str = (($$)items) as string;"));

        [Fact]
        public void Test9()
            => VerifyTrue(AddInsideMethod(@"Func<int, int, int> f = (x, i $$"));

        [Fact]
        public void TestNestedVar()
            => VerifyTrue(AddInsideMethod(@"var (($$), y)"));

        [Fact]
        public void TestNestedVar2()
            => VerifyTrue(AddInsideMethod(@"var ((x, $$), y)"));

        [Fact]
        public void TestNestedVar3()
            => VerifyTrue(AddInsideMethod(@"var ((x, $$), y) = e;"));

        [Fact]
        public void TestForeachVar1()
            => VerifyTrue(AddInsideMethod(@"foreach(var ($$"));

        [Fact]
        public void TestForeachVar2()
            => VerifyTrue(AddInsideMethod(@"foreach(var ($$)"));

        [Fact]
        public void TestForeachVar3()
            => VerifyTrue(AddInsideMethod(@"foreach(var ($$))"));

        [Fact]
        public void TestForeachVar4()
            => VerifyTrue(AddInsideMethod(@"foreach(var (x, $$"));

        [Fact]
        public void TestForeachVar5()
            => VerifyTrue(AddInsideMethod(@"foreach(var ($$) in "));

        [Fact]
        public void TestForeachVar6()
            => VerifyTrue(AddInsideMethod(@"foreach(var (($$), y)"));

        [Fact]
        public void TestForeachVar7()
            => VerifyTrue(AddInsideMethod(@"foreach(var (($$), y) in "));

        [Fact]
        public void TestForeachVar8()
            => VerifyTrue(AddInsideMethod(@"foreach(var ((x, $$), y)"));

        [Fact]
        public void TestForeachVar9()
            => VerifyTrue(AddInsideMethod(@"foreach(var ((x, $$), y) in "));

        [Fact]
        public void False1()
            => VerifyFalse(AddInsideMethod(@"var $$"));

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25084#issuecomment-369075537")]
        public void FalseAfterPattern1()
            => VerifyFalse(AddInsideMethod(@"if (1 is int i $$"));

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25084#issuecomment-369075537")]
        public void FalseAfterPattern2()
            => VerifyFalse(AddInsideMethod(@"if (1 is int i $$);"));

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25084#issuecomment-369075537")]
        public void FalseAfterPattern3()
            => VerifyFalse(AddInsideMethod(@"switch (1) { case int i $$ }"));
    }
}
