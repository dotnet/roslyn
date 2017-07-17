// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseNamedArguments;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.UseNamedArguments
{
    public class UseNamedArgumentsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpUseNamedArgumentsCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestFirstArgument()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNonFirstArgument()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(int arg1, int arg2) => M(1, [||]2); }",
@"class C { void M(int arg1, int arg2) => M(1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestDelegate()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(System.Action<int> f) => f([||]1); }",
@"class C { void M(System.Action<int> f) => f(obj: 1); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestConditionalMethod()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(int arg1, int arg2) => this?.M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => this?.M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestConditionalIndexer()
        {
            await TestInRegularAndScriptAsync(
@"class C { int? this[int arg1, int arg2] => this?[[||]1, 2]; }",
@"class C { int? this[int arg1, int arg2] => this?[arg1: 1, arg2: 2]; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestThisConstructorInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class C { C(int arg1, int arg2) {} C() : this([||]1, 2) {} }",
@"class C { C(int arg1, int arg2) {} C() : this(arg1: 1, arg2: 2) {} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestBaseConstructorInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class C { public C(int arg1, int arg2) {} } class D : C { D() : base([||]1, 2) {} }",
@"class C { public C(int arg1, int arg2) {} } class D : C { D() : base(arg1: 1, arg2: 2) {} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestConstructor()
        {
            await TestInRegularAndScriptAsync(
@"class C { C(int arg1, int arg2) { new C([||]1, 2); } }",
@"class C { C(int arg1, int arg2) { new C(arg1: 1, arg2: 2); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestIndexer()
        {
            await TestInRegularAndScriptAsync(
@"class C { char M(string arg1) => arg1[[||]0]; }",
@"class C { char M(string arg1) => arg1[index: 0]; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingOnArrayIndexer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C { int M(int[] arg1) => arg1[[||]0]; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingOnConditionalArrayIndexer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C { int? M(int[] arg1) => arg1?[[||]0]; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingOnEmptyArgumentList()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C { void M() => M([||]); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingOnExistingArgumentName()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C { void M(int arg) => M([||]arg: 1); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestEmptyParams()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(int arg1, params int[] arg2) => M([||]1); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestSingleParams()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(int arg1, params int[] arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNamedParams()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(int arg1, params int[] arg2) => M([||]1, arg2: new int[0]); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1, arg2: new int[0]); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestExistingArgumentNames()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(int arg1, int arg2) => M([||]1, arg2: 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestExistingUnorderedArgumentNames()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(int arg1, int arg2, int arg3) => M([||]1, arg3: 3, arg2: 2); }",
@"class C { void M(int arg1, int arg2, int arg3) => M(arg1: 1, arg3: 3, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestPreserveTrivia()
        {
            await TestInRegularAndScriptAsync(
@"class C { void M(int arg1, ref int arg2) => M(

    [||]1,

    ref arg1

    ); }",
@"class C { void M(int arg1, ref int arg2) => M(

    arg1: 1,

    arg2: ref arg1

    ); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingOnNameOf()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C { string M() => nameof([||]M); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestAttribute()
        {
            await TestInRegularAndScriptAsync(
@"[C([||]1, 2)]
class C : System.Attribute { public C(int arg1, int arg2) {} }",
@"[C(arg1: 1, arg2: 2)]
class C : System.Attribute { public C(int arg1, int arg2) {} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestAttributeWithNamedProperties()
        {
            await TestInRegularAndScriptAsync(
@"[C([||]1, P = 2)]
class C : System.Attribute { public C(int arg1) {} public int P { get; set; } }",
@"[C(arg1: 1, P = 2)]
class C : System.Attribute { public C(int arg1) {} public int P { get; set; } }");
        }

        [WorkItem(18848, "https://github.com/dotnet/roslyn/issues/18848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestAvailableOnFirstTokenOfArgument1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int arg1, int arg2) 
        => M([||]1 + 2, 2);
}",
@"class C
{
    void M(int arg1, int arg2)
        => M(arg1: 1 + 2, arg2: 2);
}");
        }

        [WorkItem(18848, "https://github.com/dotnet/roslyn/issues/18848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestAvailableOnFirstTokenOfArgument2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int arg1, int arg2) 
        => M(1[||] + 2, 2);
}",
@"class C
{
    void M(int arg1, int arg2)
        => M(arg1: 1 + 2, arg2: 2);
}");
        }

        [WorkItem(18848, "https://github.com/dotnet/roslyn/issues/18848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNotMissingWhenInsideSingleLineArgument1()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M(Action arg1, int arg2) 
        => M([||]() => { }, 2);
}",
@"
using System;

class C
{
    void M(Action arg1, int arg2) 
        => M(arg1: () => { }, arg2: 2);
}");
        }

        [WorkItem(18848, "https://github.com/dotnet/roslyn/issues/18848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNotMissingWhenInsideSingleLineArgument2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(int arg1, int arg2) 
        => M(1 [||]+ 2, 2);
}",
@"class C
{
    void M(int arg1, int arg2) 
        => M(arg1: 1 + 2, arg2: 2);
}");
        }

        [WorkItem(18848, "https://github.com/dotnet/roslyn/issues/18848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNotMissingWhenInsideSingleLineArgument3()
        {
            await TestInRegularAndScriptAsync(
@"
using System;

class C
{
    void M(Action arg1, int arg2) 
        => M(() => { [||] }, 2);
}",
@"
using System;

class C
{
    void M(Action arg1, int arg2) 
        => M(arg1: () => { }, arg2: 2);
}");
        }

        [WorkItem(18848, "https://github.com/dotnet/roslyn/issues/18848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingNotOnStartingLineOfArgument1()
        {
            await TestMissingAsync(
@"
using System;

class C
{
    void M(Action arg1, int arg2) 
        => M(() => {
             [||]
           }, 2);
}");
        }

        [WorkItem(18848, "https://github.com/dotnet/roslyn/issues/18848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingWithSelection()
        {
            await TestMissingAsync(
@"
using System;

class C
{
    void M(Action arg1, int arg2) 
        => M([|1 + 2|], 3);
}");
        }

        [WorkItem(19175, "https://github.com/dotnet/roslyn/issues/19175")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestCaretPositionAtTheEnd1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int arg1) => M(arg1[||]);
}",
@"class C
{
    void M(int arg1) => M(arg1: arg1);
}");
        }

        [WorkItem(19175, "https://github.com/dotnet/roslyn/issues/19175")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestCaretPositionAtTheEnd2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int arg1, int arg2) => M(arg1[||], arg2);
}",
@"class C
{
    void M(int arg1, int arg2) => M(arg1: arg1, arg2: arg2);
}");
        }

        [WorkItem(19758, "https://github.com/dotnet/roslyn/issues/19758")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingOnTuple()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Linq;
class C
{
    void M(int[] arr) => arr.Zip(arr, (p1, p2) =>  ([||]p1, p2));
}
");
        } 
    }
}
