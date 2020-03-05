﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.InvertLogical;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InvertLogical
{
    public partial class InvertLogicalTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpInvertLogicalCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task InvertLogical1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = a > 10 [||]|| b < 20;
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !(a <= 10 && b >= 20);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task InvertLogical2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !(a <= 10 [||]&& b >= 20);
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = a > 10 || b < 20;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !(a <= 10 [||]&&
                  b >= 20);
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = a > 10 ||
                  b < 20;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task TestTrivia2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b, int c)
    {
        var c = !(a <= 10 [||]&&
                  b >= 20 &&
                  c == 30);
    }
}",
@"class C
{
    void M(bool x, int a, int b, int c)
    {
        var c = a > 10 ||
                  b < 20 ||
                  c != 30;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task InvertMultiConditional1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int a, int b, int c)
    {
        var x = a > 10 [||]|| b < 20 || c == 30;
    }
}",
@"class C
{
    void M(int a, int b, int c)
    {
        var x = !(a <= 10 && b >= 20 && c != 30);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task InvertMultiConditional2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int a, int b, int c)
    {
        var x = a > 10 || b < 20 [||]|| c == 30;
    }
}",
@"class C
{
    void M(int a, int b, int c)
    {
        var x = !(a <= 10 && b >= 20 && c != 30);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task InvertMultiConditional3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int a, int b, int c)
    {
        var x = !(a <= 10 [||]&& b >= 20 && c != 30);
    }
}",
@"class C
{
    void M(int a, int b, int c)
    {
        var x = a > 10 || b < 20 || c == 30;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task InverSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int a, int b, int c)
    {
        var x = !([|a <= 10 && b >= 20 && c != 30|]);
    }
}",
@"class C
{
    void M(int a, int b, int c)
    {
        var x = a > 10 || b < 20 || c == 30;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task MissingInverSelection1()
        {
            // Can't convert selected partial subtrees 
            // -> see comment at AbstractInvertLogicalCodeRefactoringProvider::ComputeRefactoringsAsync
            // -> "expected" result commented out & TestMissingXXX method used in the meantime
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(int a, int b, int c)
    {
        var x = !([|a <= 10 && b >= 20|] && c != 30);
    }
}"/*
@"class C
{
    void M(int a, int b, int c)
    {
        var x = !(!(a > 10 || b < 20) && c != 30);
    }
}"*/);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task InvertMultiConditional4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int a, int b, int c)
    {
        var x = !(a <= 10 && b >= 20 [||]&& c != 30);
    }
}",
@"class C
{
    void M(int a, int b, int c)
    {
        var x = a > 10 || b < 20 || c == 30;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task TestMissingOnShortCircuitAnd()
        {
            await TestMissingAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = a > 10 [||]& b < 20;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task TestMissingOnShortCircuitOr()
        {
            await TestMissingAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = a > 10 [||]| b < 20;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        public async Task TestSelectedOperator()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = a > 10 [||||] b < 20;
    }
}",
@"class C
{
    void M(bool x, int a, int b)
    {
        var c = !(a <= 10 && b >= 20);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertLogical)]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task MissingSelectedSubtree()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(int a, int b, int c)
    {
        var x = !(a <= 10 && [|b >= 20 && c != 30|]);
    }
}");
        }
    }
}
