// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveNewModifier;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.RemoveNewModifier
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveNewModifier)]
    public class RemoveNewModifierCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace) =>
            (null, new RemoveNewModifierCodeFixProvider());

        [Fact]
        public async Task TestRemoveNewFromProperty()
        {
            await TestInRegularAndScriptAsync(
                @"class App
{
    public static new App [|Current|] { get; set; }
}",
                @"class App
{
    public static App Current { get; set; }
}");
        }

        [Fact]
        public async Task TestRemoveNewFromMethod()
        {
            await TestInRegularAndScriptAsync(
                @"class App
{
    public static new void [|Method()|]
    {
    }
}",
                @"class App
{
    public static void Method()
    {
    }
}");
        }

        [Fact]
        public async Task TestRemoveNewFromField()
        {
            await TestInRegularAndScriptAsync(
                @"class App
{
    public new int [|Test|];
}",
                @"class App
{
    public int Test;
}");
        }

        [Fact]
        public async Task TestRemoveNewFromConstant()
        {
            await TestInRegularAndScriptAsync(
                @"class App
{
    public const new int [|Test|] = 1;
}",
                @"class App
{
    public const int Test = 1;
}");
        }

        [Fact]
        public async Task TestRemoveNewFirstModifier()
        {
            await TestInRegularAndScriptAsync(
                @"class App
{
    new App [|Current|] { get; set; }
}",
                @"class App
{
    App Current { get; set; }
}");
        }

        [Fact]
        public async Task TestRemoveNewFromConstantInternalFields()
        {
            await TestInRegularAndScriptAsync(
                @"class A { internal const new int [|i|] = 1; }",
                @"class A { internal const int [|i|] = 1; }");
        }

        [Fact]
        public async Task TestRemoveNewWithNoTrivia()
        {
            await TestInRegularAndScriptAsync(
                @"class C
{
new(int a, int b) [|x|];
}",
                @"class C
{
(int a, int b) x;
}");
        }

        [Theory]
        [InlineData(
            "public new event Action [|E|];",
            "public event Action E;")]
        [InlineData(
            "public new int [|this[int p]|] => p;",
            "public int this[int p] => p;")]
        [InlineData(
            "new class [|Test|] { }",
            "class Test { }")]
        [InlineData(
            "new struct [|Test|] { }",
            "struct Test { }")]
        [InlineData(
            "new interface [|Test|] { }",
            "interface Test { }")]
        public async Task Test(string original, string expected)
        {
            await TestInRegularAndScriptAsync(
                $@"class App
{{
    {original}
}}",
                $@"class App
{{
    {expected}
}}");
        }

        [Theory]
        [InlineData(
            "/* start */ public /* middle */ new /* end */ int [|Test|];",
            "/* start */ public /* middle */ /* end */ int Test;")]
        [InlineData(
            "/* start */ public /* middle */ new    /* end */ int [|Test|];",
            "/* start */ public /* middle */ /* end */ int Test;")]
        [InlineData(
            "/* start */ public /* middle */new /* end */ int [|Test|];",
            "/* start */ public /* middle */ /* end */ int Test;")]
        [InlineData(
            "/* start */ public /* middle */ new/* end */ int [|Test|];",
            "/* start */ public /* middle */ /* end */ int Test;")]
        [InlineData(
            "/* start */ public /* middle */new/* end */ int [|Test|];",
            "/* start */ public /* middle *//* end */ int Test;")]
        [InlineData(
            "new /* end */ int [|Test|];",
            "/* end */ int Test;")]
        [InlineData(
            "new     int [|Test|];",
            "int Test;")]
        [InlineData(
            "/* start */ new /* end */ int [|Test|];",
            "/* start */ /* end */ int [|Test|];")]
        public async Task TestRemoveNewFromModifiersWithComplexTrivia(string original, string expected) =>
            await TestInRegularAndScript1Async(
                $@"class App
{{
    {original}
}}",
                $@"class App
{{
    {expected}
}}");
    }
}
