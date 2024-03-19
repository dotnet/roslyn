// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.NameTupleElement;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NameTupleElement
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsNameTupleElement)]
    public class NameTupleElementTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
            => new CSharpNameTupleElementCodeRefactoringProvider();

        [Fact]
        public async Task TestInCall_FirstElement()
        {
            await TestInRegularAndScript1Async(
@"class C { void M((int arg1, int arg2) x) => M(([||]1, 2)); }",
@"class C { void M((int arg1, int arg2) x) => M((arg1: 1, 2)); }");
        }

        [Fact]
        public async Task TestInCall_Deep()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M((int arg1, int arg2) x) => M((Method([||]1), 2));
    int Method(int x) => throw null;
}",
@"class C
{
    void M((int arg1, int arg2) x) => M((arg1: Method(1), 2));
    int Method(int x) => throw null;
}");
        }

        [Fact]
        public async Task TestInCall_Deep2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M((int arg1, int arg2) x) => M((1, Method(1[||], 2)));
    int Method((int arg3, int arg4) x) => throw null;
}",
@"class C
{
    void M((int arg1, int arg2) x) => M((1, arg2: Method(1, 2)));
    int Method((int arg3, int arg4) x) => throw null;
}");
        }

        [Fact]
        public async Task TestInCall_Deep3()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M((int arg1, int arg2) x) => M((1, Method[||](1, 2)));
    int Method((int arg3, int arg4) x) => throw null;
}",
@"class C
{
    void M((int arg1, int arg2) x) => M((1, arg2: Method(1, 2)));
    int Method((int arg3, int arg4) x) => throw null;
}");
        }

        [Fact]
        public async Task TestInCall_FirstElement_EscapedNamed()
        {
            await TestInRegularAndScript1Async(
@"class C { void M((int @int, int arg2) x) => M(([||]1, 2)); }",
@"class C { void M((int @int, int arg2) x) => M((@int: 1, 2)); }");
        }

        [Fact]
        public async Task TestInCall_FirstElement_AlreadyNamed()
            => await TestMissingAsync(@"class C { void M((int arg1, int arg2) x) => M(([||]arg1: 1, 2)); }");

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35157")]
        public async Task TestUntypedTuple()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        _ = ([||]null, 2);
    }
}");
        }

        [Fact]
        public async Task TestInvocationArgument()
        {
            await TestMissingAsync(
@"class C
{
    void M(string arg1, int arg2)
    {
        M([||]null, 2);
    }
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestWithSelection()
        {
            await TestInRegularAndScript1Async(
@"class C { void M((int arg1, int arg2) x) => M(([|1|], 2)); }",
@"class C { void M((int arg1, int arg2) x) => M((arg1: 1, 2)); }");
        }

        [Fact]
        public async Task TestWithConversion()
        {
            await TestMissingAsync(
@"class C
{
    void M(C x) => M(([|1|], 2));
    public static implicit operator C((int arg1, int arg2) x) => throw null;
}");
        }

        [Fact]
        public async Task TestInCall_FirstElement_WithTrivia()
        {
            await TestInRegularAndScript1Async(
@"class C { void M((int arg1, int arg2) x) => M((/*before*/ [||]1 /*after*/, 2)); }",
@"class C { void M((int arg1, int arg2) x) => M((/*before*/ arg1: 1 /*after*/, 2)); }");
        }

        [Fact]
        public async Task TestInCall_FirstElement_Nested()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    int M((int arg1, int arg2) x)
        => M((M(([||]1, 2)), 3));
}",
@"class C
{
    int M((int arg1, int arg2) x)
        => M((M((arg1: 1, 2)), 3));
}");
        }

        [Fact]
        public async Task TestInCall_FirstComma()
        {
            await TestInRegularAndScript1Async(
@"class C { void M((int arg1, int arg2) x) => M((1[||], 2)); }",
@"class C { void M((int arg1, int arg2) x) => M((arg1: 1, 2)); }");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
        public async Task TestInCall_FirstComma2()
        {
            await TestInRegularAndScript1Async(
@"class C { void M((int arg1, int arg2) x) => M((1,[||] 2)); }",
@"class C { void M((int arg1, int arg2) x) => M((1, arg2: 2)); }");
        }

        [Fact]
        public async Task TestInCall_SecondElement()
        {
            await TestInRegularAndScript1Async(
@"class C { void M((int arg1, int arg2) x) => M((1, [||]2)); }",
@"class C { void M((int arg1, int arg2) x) => M((1, arg2: 2)); }");
        }

        [Fact]
        public async Task TestInCall_CloseParen()
        {
            await TestInRegularAndScript1Async(
@"class C { void M((int arg1, int arg2) x) => M((1, 2[||])); }",
@"class C { void M((int arg1, int arg2) x) => M((1, arg2: 2)); }");
        }

        [Fact]
        public async Task TestUnnamedTuple()
            => await TestMissingAsync(@"class C { void M((int, int) x) => M(([||]1, 2)); }");

        [Fact]
        public async Task TestArrowReturnedTuple()
        {
            await TestInRegularAndScript1Async(
@"class C { (int arg1, int arg2, int arg3) M() => ([||]1, 2); }",
@"class C { (int arg1, int arg2, int arg3) M() => (arg1: 1, 2); }");
        }

        [Fact]
        public async Task TestArrowReturnedTuple_LocalFunction()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        (int arg1, int arg2, int arg3) local() => ([||]1, 2);
    }
}",
@"class C
{
    void M()
    {
        (int arg1, int arg2, int arg3) local() => (arg1: 1, 2);
    }
}");
        }

        [Fact]
        public async Task TestReturnedTuple()
        {
            await TestInRegularAndScript1Async(
@"class C { (int arg1, int arg2, int arg3) M() { return ([||]1, 2); } }",
@"class C { (int arg1, int arg2, int arg3) M() { return (arg1: 1, 2); } }");
        }

        [Fact]
        public async Task TestReturnedTuple_LongerTuple()
        {
            await TestMissingAsync(
@"class C { (int arg1, int arg2) M() => (1, 2, [||]3); }");
        }
    }
}
