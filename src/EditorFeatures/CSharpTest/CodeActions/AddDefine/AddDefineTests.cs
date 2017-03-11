// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddDefine;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.AddDefine
{
    public class AddDefineTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new AddDefineCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDefine)]
        public async Task OnlySimplePragma()
        {
            await TestMissingInRegularAndScriptAsync(@"
class C
{
#if [||]TEST == 1
#endif
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDefine)]
        public async Task OnlyPragma()
        {
            await TestMissingInRegularAndScriptAsync(@"
class C
{
    int x[||];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDefine)]
        public async Task OnlyEmptySelection()
        {
            await TestMissingInRegularAndScriptAsync(@"
class C
{
#if [|TEST|]
#endif
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDefine)]
        public async Task OnlyInactiveCondition()
        {
            await TestMissingInRegularAndScriptAsync(@"
#define TEST
class C
{
#if [|TEST|]
#endif
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDefine)]
        public async Task OnlyIfPragma()
        {
            await TestMissingInRegularAndScriptAsync(@"
#define [||]TEST
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDefine)]
        public async Task CursorBeforeIf()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    void M()
    {
[||]#if TEST
        System.Console.WriteLine();
#endif
    }
}", @"
#define TEST
class C
{
    void M()
    {
#if [||]TEST
        System.Console.WriteLine();
#endif
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDefine)]
        public async Task CursorMiddleIf()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    void M()
    {
#if [||]TEST
        System.Console.WriteLine();
#endif
    }
}", @"
#define TEST
class C
{
    void M()
    {
#if [||]TEST
        System.Console.WriteLine();
#endif
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDefine)]
        public async Task CursorAfterIf()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    void M()
    {
#if TEST[||]
        System.Console.WriteLine();
#endif
    }
}", @"
#define TEST
class C
{
    void M()
    {
#if [||]TEST
        System.Console.WriteLine();
#endif
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDefine)]
        public async Task InitialCommentRemains()
        {
            await TestInRegularAndScriptAsync(@"
// copyright
class C
{
    void M()
    {
#if [||]TEST
        System.Console.WriteLine();
#endif
    }
}", @"
// copyright
#define TEST
class C
{
    void M()
    {
#if [||]TEST
        System.Console.WriteLine();
#endif
    }
}", ignoreTrivia: false);
        }
    }
}
