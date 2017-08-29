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
        public async Task TestEndOfParameter1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C(string s[||])
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
        public async Task TestEndOfParameter2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C(string s[||], string t)
    {
    }
}",
@"
class C
{
    private string s;

    public C(string s, string t)
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
        S = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithDifferentName()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    private string t;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string t;

    public C(string s)
    {
        S = s;
    }

    public string S { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeNonWritableProperty()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string S => null;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string S => null;

    public string S1 { get; }

    public C(string s)
    {
        S1 = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeDoesNotUsePropertyWithUnrelatedName()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    private string T { get; }

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private string T { get; }
    public string S { get; }

    public C(string s)
    {
        S = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithWrongType1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private int s;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private int s;

    public C(string s)
    {
        S = s;
    }

    public string S { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInitializeFieldWithWrongType2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private int s;

    public C([||]string s)
    {
    }
}",
@"
class C
{
    private readonly string s1;
    private int s;

    public C(string s)
    {
        s1 = s;
    }
}", index: 1);
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestWhenAlreadyInitialized1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private int s;
    private int x;

    public C([||]string s)
    {
        x = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestWhenAlreadyInitialized2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private int s;
    private int x;

    public C([||]string s)
    {
        x = s ?? throw new Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestWhenAlreadyInitialized3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private int s;

    public C([||]string s)
    {
        s = 0;
    }
}",

@"
class C
{
    private int s;

    public C([||]string s)
    {
        s = 0;
        S = s;
    }

    public string S { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;
    private string t;

    public C([||]string s, string t)
    {
        this.t = t;   
    }
}",
@"
class C
{
    private string s;
    private string t;

    public C(string s, string t)
    {
        this.s = s;
        this.t = t;   
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;
    private string t;

    public C(string s, [||]string t)
    {
        this.s = s;   
    }
}",
@"
class C
{
    private string s;
    private string t;

    public C(string s, string t)
    {
        this.s = s;
        this.t = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C([||]string s)
    {
        if (true) { } 
    }
}",
@"
class C
{
    private string s;

    public C(string s)
    {
        if (true) { }

        this.s = s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNotInMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    private string s;

    public M([||]string s)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation4()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;
    private string t;

    public C(string s, [||]string t)
        => this.s = s;   
}",
@"
class C
{
    private string s;
    private string t;

    public C(string s, string t)
    {
        this.s = s;
        this.t = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation5()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;
    private string t;

    public C([||]string s, string t)
        => this.t = t;   
}",
@"
class C
{
    private string s;
    private string t;

    public C(string s, string t)
    {
        this.s = s;
        this.t = t;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation6()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    public C(string s, [||]string t)
    {
        S = s;   
    }

    public string S { get; }
}",
@"
class C
{
    public C(string s, string t)
    {
        S = s;
        T = t;
    }

    public string S { get; }
    public string T { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestInsertionLocation7()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    public C([||]string s, string t)
    {
        T = t;   
    }

    public string T { get; }
}",
@"
class C
{
    public C(string s, string t)
    {
        S = s;
        T = t;   
    }

    public string S { get; }
    public string T { get; }
}");
        }

        [WorkItem(19956, "https://github.com/dotnet/roslyn/issues/19956")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)]
        public async Task TestNoBlock()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    private string s;

    public C(string s[||])
}",
@"
class C
{
    private string s;

    public C(string s)
    {
        this.s = s;
    }
}", ignoreTrivia: false);
        }
    }
}
