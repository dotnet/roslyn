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
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new CSharpUseNamedArgumentsCodeRefactoringProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestFirstArgument()
        {
            await TestAsync(
@"class C { void M(int arg1, int arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNonFirstArgument()
        {
            await TestAsync(
@"class C { void M(int arg1, int arg2) => M(1, [||]2); }",
@"class C { void M(int arg1, int arg2) => M(1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestDelegate()
        {
            await TestAsync(
@"class C { void M(System.Action<int> f) => f([||]1); }",
@"class C { void M(System.Action<int> f) => f(obj: 1); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestConditionalMethod()
        {
            await TestAsync(
@"class C { void M(int arg1, int arg2) => this?.M([||]1, 2); }",
@"class C { void M(int arg1, int arg2) => this?.M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestConditionalIndexer()
        {
            await TestAsync(
@"class C { int? this[int arg1, int arg2] => this?[[||]1, 2]; }",
@"class C { int? this[int arg1, int arg2] => this?[arg1: 1, arg2: 2]; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestThisConstructorInitializer()
        {
            await TestAsync(
@"class C { C(int arg1, int arg2) {} C() : this([||]1, 2) {} }",
@"class C { C(int arg1, int arg2) {} C() : this(arg1: 1, arg2: 2) {} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestBaseConstructorInitializer()
        {
            await TestAsync(
@"class C { public C(int arg1, int arg2) {} } class D : C { D() : base([||]1, 2) {} }",
@"class C { public C(int arg1, int arg2) {} } class D : C { D() : base(arg1: 1, arg2: 2) {} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestConstructor()
        {
            await TestAsync(
@"class C { C(int arg1, int arg2) { new C([||]1, 2); } }",
@"class C { C(int arg1, int arg2) { new C(arg1: 1, arg2: 2); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestIndexer()
        {
            await TestAsync(
@"class C { char M(string arg1) => arg1[[||]0]; }",
@"class C { char M(string arg1) => arg1[index: 0]; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingOnArrayIndexer()
        {
            await TestMissingAsync(
@"class C { int M(int[] arg1) => arg1[[||]0]; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingOnConditionalArrayIndexer()
        {
            await TestMissingAsync(
@"class C { int? M(int[] arg1) => arg1?[[||]0]; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingOnEmptyArgumentList()
        {
            await TestMissingAsync(
@"class C { void M() => M([||]); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestMissingOnExistingArgumentName()
        {
            await TestMissingAsync(
@"class C { void M(int arg) => M([||]arg: 1); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestEmptyParams()
        {
            await TestAsync(
@"class C { void M(int arg1, params int[] arg2) => M([||]1); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestSingleParams()
        {
            await TestAsync(
@"class C { void M(int arg1, params int[] arg2) => M([||]1, 2); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestNamedParams()
        {
            await TestAsync(
@"class C { void M(int arg1, params int[] arg2) => M([||]1, arg2: new int[0]); }",
@"class C { void M(int arg1, params int[] arg2) => M(arg1: 1, arg2: new int[0]); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestExistingArgumentNames()
        {
            await TestAsync(
@"class C { void M(int arg1, int arg2) => M([||]1, arg2: 2); }",
@"class C { void M(int arg1, int arg2) => M(arg1: 1, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestExistingUnorderedArgumentNames()
        {
            await TestAsync(
@"class C { void M(int arg1, int arg2, int arg3) => M([||]1, arg3: 3, arg2: 2); }",
@"class C { void M(int arg1, int arg2, int arg3) => M(arg1: 1, arg3: 3, arg2: 2); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestPreserveTrivia()
        {
            await TestAsync(
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
            await TestMissingAsync(
@"class C { string M() => nameof([||]M); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestAttirbute()
        {
            await TestAsync(
@"[C([||]1, 2)]
class C : System.Attribute { public C(int arg1, int arg2) {} }",
@"[C(arg1: 1, arg2: 2)]
class C : System.Attribute { public C(int arg1, int arg2) {} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseNamedArguments)]
        public async Task TestAttirbuteWithNamedProperties()
        {
            await TestAsync(
@"[C([||]1, P = 2)]
class C : System.Attribute { public C(int arg1) {} public int P { get; set; } }",
@"[C(arg1: 1, P = 2)]
class C : System.Attribute { public C(int arg1) {} public int P { get; set; } }");
        }
    }
}