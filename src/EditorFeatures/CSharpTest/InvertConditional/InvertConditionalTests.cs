// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.InvertConditional;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertConditional
{
    public partial class InvertConditionalTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpInvertConditionalCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)]
        public async Task InvertConditional1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = x [||]? a : b;
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)]
        public async Task InvertConditional2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !x [||]? a : b;
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = x ? b : a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)]
        public async Task TestTrivia()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = [||]x
            ? a
            : b;
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !x
            ? b
            : a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = [||]x ?
            a :
            b;
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !x ?
            b :
            a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)]
        public async Task TestStartOfConditional()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = [||]x ? a : b;
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !x ? b : a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)]
        public async Task TestMissingAfterCondition()
        {
            await TestMissingAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = x ? a [||]: b;
    }
}");
        }
    }
}
