// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.InvertConditional;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
        public async Task TestTrivia2()
        {
            // We currently do not move trivia along with the true/false parts.  We could consider
            // trying to intelligently do that in the future.  It would require moving the comments,
            // but preserving the whitespace/newlines.
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = [||]x
            ? a /*trivia1*/
            : b /*trivia2*/;
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !x
            ? b /*trivia1*/
            : a /*trivia2*/;
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

        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertConditional)]
        public async Task TestAfterCondition()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = x ? a [||]: b;
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
    }
}
