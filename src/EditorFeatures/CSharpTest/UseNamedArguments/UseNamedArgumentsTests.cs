// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UseNamedArguments;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNamedArguments
{
    public class UseNamedArgumentsTests : AbstractCSharpCodeActionTest
    {
        private static readonly ParseOptions CSharp72 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2);

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpUseNamedArgumentsCodeRefactoringProvider();

        private Task TestWithCSharp7(string initialMarkup, string expectedMarkup)
        {
            return TestAsync(
                initialMarkup, expectedMarkup, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7));
        }

        private Task TestWithCSharp7_2(string initialMarkup, string expectedMarkup, int index = 0)
        {
            return TestAsync(
                initialMarkup, expectedMarkup, index: index, parseOptions: CSharp72);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestFirstArgument()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestFirstArgument_CSharp7_2_FirstOption()
        {
            // First option only adds the named argument to the specific parameter you're on.
            await TestWithCSharp7_2(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestFirstArgument_CSharp7_2_SecondOption()
        {
            // Second option only adds the named argument to parameter you're on and all trailing parameters.
            await TestWithCSharp7_2(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNonFirstArgument()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => M(1, [||]2); }",
@"class C { void M(int arg1, int arg2) => M(1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNonFirstArgument_CSharp_7_2()
        {
            // Because we're on the last argument, we should only offer one refactoring to the user.
            var initialMarkup = @"class C { void M(int arg1, int arg2) => M(1, [||]2); }";
            await TestActionCountAsync(initialMarkup, count: 1, parameters: new TestParameters(parseOptions: CSharp72));

            await TestWithCSharp7_2(
initialMarkup,
@"class C { void M(int arg1, int arg2) => M(1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestDelegate()
        {
            await TestWithCSharp7(
@"class C { void M(System.Action<int> f) => f([||]1); }",
@"class C { void M(System.Action<int> f) => f(obj: 1); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestConditionalMethod()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => this?.M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => this?.M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestConditionalIndexer()
        {
            await TestWithCSharp7(
@"class C { int? this[int arg1, int arg2] => this?[[||]1, 2]; }",
@"class C { int? this[int arg1, int arg2] => this?[arg1: 1, arg2: 2]; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestThisConstructorInitializer()
        {
            await TestWithCSharp7(
@"class C { C(int arg1, int arg2) {} C() : this([||]1, 2) {} }",
@"class C { C(int arg1, int arg2) {} C() : this(arg1: 1, arg2: 2) {} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestBaseConstructorInitializer()
        {
            await TestWithCSharp7(
@"class C { public C(int arg1, int arg2) {} } class D : C { D() : base([||]1, 2) {} }",
@"class C { public C(int arg1, int arg2) {} } class D : C { D() : base(arg1: 1, arg2: 2) {} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestConstructor()
        {
            await TestWithCSharp7(
@"class C { C(int arg1, int arg2) { new C([||]1, 2); } }",
@"class C { C(int arg1, int arg2) { new C(arg1: 1, arg2: 2); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestIndexer()
        {
            await TestWithCSharp7(
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
            await TestWithCSharp7(
@"class C { void M(int arg1, params int[] arg2) => M([||]1); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestSingleParams()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, params int[] arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNamedParams()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, params int[] arg2) => M([||]1, arg2: new int[0]); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1, arg2: new int[0]); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestExistingArgumentNames()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, int arg2) => M([||]1, arg2: 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestExistingUnorderedArgumentNames()
        {
            await TestWithCSharp7(
@"class C { void M(int arg1, int arg2, int arg3) => M([||]1, arg3: 3, arg2: 2); }",
@"class C { void M(int arg1, int arg2, int arg3) => M(arg1: 1, arg3: 3, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestPreserveTrivia()
        {
            await TestWithCSharp7(
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
            await TestWithCSharp7(
@"[C([||]1, 2)]
class C : System.Attribute { public C(int arg1, int arg2) {} }",
@"[C(arg1: 1, arg2: 2)]
class C : System.Attribute { public C(int arg1, int arg2) {} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestAttributeWithNamedProperties()
        {
            await TestWithCSharp7(
@"[C([||]1, P = 2)]
class C : System.Attribute { public C(int arg1) {} public int P { get; set; } }",
@"[C(arg1: 1, P = 2)]
class C : System.Attribute { public C(int arg1) {} public int P { get; set; } }");
        }

        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestAvailableOnSelectionOfArgument1()
        {
            await TestWithCSharp7(
@"class C
{
    void M(int arg1, int arg2) 
        => M([|1 + 2|], 2);
}",
@"class C
{
    void M(int arg1, int arg2) 
        => M(arg1: 1 + 2, arg2: 2);
}");
        }

        [WorkItem(18848, "https://github.com/dotnet/roslyn/issues/18848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestAvailableOnFirstTokenOfArgument1()
        {
            await TestWithCSharp7(
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
            await TestWithCSharp7(
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
            await TestWithCSharp7(
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
        public async Task TestNotMissingWhenInsideSingleLineArgument2_CSharp7()
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
}",
                parameters: new TestParameters(parseOptions: TestOptions.Regular7));
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
        => M(arg1: 1 + 2, 2);
}", parameters: new TestParameters(parseOptions: TestOptions.Regular7_3));
        }

        [WorkItem(18848, "https://github.com/dotnet/roslyn/issues/18848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNotMissingWhenInsideSingleLineArgument3()
        {
            await TestWithCSharp7(
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
        => M(arg1: () => {  }, arg2: 2);
}");
        }

        [WorkItem(18848, "https://github.com/dotnet/roslyn/issues/18848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNotMissingWhenInsideSingleLineArgument4()
        {
            await TestWithCSharp7(
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
            await TestWithCSharp7(
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
            await TestWithCSharp7(
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

        [WorkItem(23269, "https://github.com/dotnet/roslyn/issues/23269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestCharacterEscape1()
        {
            await TestWithCSharp7(
@"class C
{
    void M(int @default, int @params) => M([||]1, 2);
}",
@"class C
{
    void M(int @default, int @params) => M(@default: 1, @params: 2);
}");
        }

        [WorkItem(23269, "https://github.com/dotnet/roslyn/issues/23269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestCharacterEscape2()
        {
            await TestWithCSharp7(
@"[C([||]1, 2)]
class C : System.Attribute
{
    public C(int @default, int @params) {}
}",
@"[C(@default: 1, @params: 2)]
class C : System.Attribute
{
    public C(int @default, int @params) {}
}");
        }
    }
}
