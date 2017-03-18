// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.InitializeParameter;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InitializeParameter
{
    public partial class InitializeMemberFromParameterTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpInitializeMemberFromParameterCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithSameName()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string s;

    public C(string s)
    {
        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithUnderscoreName()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string _s;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string _s;

    public C(string s)
    {
        _s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeWritableProperty()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string S { get; }

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string S { get; }

    public C(string s)
    {
        this.S = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithDifferentName()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private string t;

    public C([||]string s)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeNonWritableProperty()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private string S => null;

    public C([||]string s)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializePropertyWithBadName()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private string T { get; }

    public C([||]string s)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithWrongType()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private int s;

    public C([||]string s)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithConvertibleType()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    private object s;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private object s;

    public C(string s)
    {
        this.s = s;
    }
}");
        }
    }
}