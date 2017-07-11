// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ReplaceMethodWithProperty;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ReplaceMethodWithProperty
{
    public class ReplaceMethodWithPropertyTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new ReplaceMethodWithPropertyCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithGetName()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetFoo()
    {
    }
}",
@"class C
{
    int Foo
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithoutGetName()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]Foo()
    {
    }
}",
@"class C
{
    int Foo
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        [WorkItem(6034, "https://github.com/dotnet/roslyn/issues/6034")]
        public async Task TestMethodWithArrowBody()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetFoo() => 0;
}",
@"class C
{
    int Foo { get { return 0; } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithoutBody()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetFoo();
}",
@"class C
{
    int Foo { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithModifiers()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    public static int [||]GetFoo()
    {
    }
}",
@"class C
{
    public static int Foo
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithAttributes()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    [A]
    int [||]GetFoo()
    {
    }
}",
@"class C
{
    [A]
    int Foo
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithTrivia_1()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    // Foo
    int [||]GetFoo()
    {
    }
}",
@"class C
{
    // Foo
    int Foo
    {
        get
        {
        }
    }
}",
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestIndentation()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetFoo()
    {
        int count;
        foreach (var x in y)
        {
            count += bar;
        }
        return count;
    }
}",
@"class C
{
    int Foo
    {
        get
        {
            int count;
            foreach (var x in y)
            {
                count += bar;
            }
            return count;
        }
    }
}",
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestIfDefMethod()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
#if true
    int [||]GetFoo()
    {
    }
#endif
}",
@"class C
{
#if true
    int Foo
    {
        get
        {
        }
    }
#endif
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithTrivia_2()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    // Foo
    int [||]GetFoo()
    {
    }
    // SetFoo
    void SetFoo(int i)
    {
    }
}",
@"class C
{
    // Foo
    // SetFoo
    int Foo
    {
        get
        {
        }

        set
        {
        }
    }
}",
index: 1,
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestExplicitInterfaceMethod_1()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]I.GetFoo()
    {
    }
}",
@"class C
{
    int I.Foo
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestExplicitInterfaceMethod_2()
        {
            await TestWithAllCodeStyleOff(
@"interface I
{
    int GetFoo();
}

class C : I
{
    int [||]I.GetFoo()
    {
    }
}",
@"interface I
{
    int Foo { get; }
}

class C : I
{
    int I.Foo
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestExplicitInterfaceMethod_3()
        {
            await TestWithAllCodeStyleOff(
@"interface I
{
    int [||]GetFoo();
}

class C : I
{
    int I.GetFoo()
    {
    }
}",
@"interface I
{
    int Foo { get; }
}

class C : I
{
    int I.Foo
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestInAttribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    [At[||]tr]
    int GetFoo()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestInMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int GetFoo()
    {
[||]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestVoidMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void [||]GetFoo()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestAsyncMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    async Task [||]GetFoo()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestGenericMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo<T>()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestExtensionMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"static class C
{
    int [||]GetFoo(this int i)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithParameters_1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo(int i)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithParameters_2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo(int i = 0)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestNotInSignature_1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    [At[||]tr]
    int GetFoo()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestNotInSignature_2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    int GetFoo()
    {
[||]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceNotInMethod()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetFoo()
    {
    }

    void Bar()
    {
        var x = GetFoo();
    }
}",
@"class C
{
    int Foo
    {
        get
        {
        }
    }

    void Bar()
    {
        var x = Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceSimpleInvocation()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetFoo()
    {
    }

    void Bar()
    {
        var x = GetFoo();
    }
}",
@"class C
{
    int Foo
    {
        get
        {
        }
    }

    void Bar()
    {
        var x = Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceMemberAccessInvocation()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetFoo()
    {
    }

    void Bar()
    {
        var x = this.GetFoo();
    }
}",
@"class C
{
    int Foo
    {
        get
        {
        }
    }

    void Bar()
    {
        var x = this.Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceBindingMemberInvocation()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetFoo()
    {
    }

    void Bar()
    {
        C x;
        var v = x?.GetFoo();
    }
}",
@"class C
{
    int Foo
    {
        get
        {
        }
    }

    void Bar()
    {
        C x;
        var v = x?.Foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceInMethod()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetFoo()
    {
        return GetFoo();
    }
}",
@"class C
{
    int Foo
    {
        get
        {
            return Foo;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestOverride()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    public virtual int [||]GetFoo()
    {
    }
}

class D : C
{
    public override int GetFoo()
    {
    }
}",
@"class C
{
    public virtual int Foo
    {
        get
        {
        }
    }
}

class D : C
{
    public override int Foo
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReference_NonInvoked()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    int [||]GetFoo()
    {
    }

    void Bar()
    {
        Action<int> i = GetFoo;
    }
}",
@"using System;

class C
{
    int Foo
    {
        get
        {
        }
    }

    void Bar()
    {
        Action<int> i = {|Conflict:Foo|};
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReference_ImplicitReference()
        {
            await TestWithAllCodeStyleOff(
@"using System.Collections;

class C
{
    public IEnumerator [||]GetEnumerator()
    {
    }

    void Bar()
    {
        foreach (var x in this)
        {
        }
    }
}",
@"using System.Collections;

class C
{
    public IEnumerator Enumerator
    {
        get
        {
        }
    }

    void Bar()
    {
        {|Conflict:foreach (var x in this)
        {
        }|}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    int [||]GetFoo()
    {
    }

    void SetFoo(int i)
    {
    }
}",
@"using System;

class C
{
    int Foo
    {
        get
        {
        }

        set
        {
        }
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSetReference_NonInvoked()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    int [||]GetFoo()
    {
    }

    void SetFoo(int i)
    {
    }

    void Bar()
    {
        Action<int> i = SetFoo;
    }
}",
@"using System;

class C
{
    int Foo
    {
        get
        {
        }

        set
        {
        }
    }

    void Bar()
    {
        Action<int> i = {|Conflict:Foo|};
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_SetterAccessibility()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    public int [||]GetFoo()
    {
    }

    private void SetFoo(int i)
    {
    }
}",
@"using System;

class C
{
    public int Foo
    {
        get
        {
        }

        private set
        {
        }
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_ExpressionBodies()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    int [||]GetFoo() => 0;
    void SetFoo(int i) => Bar();
}",
@"using System;

class C
{
    int Foo
    {
        get
        {
            return 0;
        }

        set
        {
            Bar();
        }
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_GetInSetReference()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    int [||]GetFoo()
    {
    }

    void SetFoo(int i)
    {
    }

    void Bar()
    {
        SetFoo(GetFoo() + 1);
    }
}",
@"using System;

class C
{
    int Foo
    {
        get
        {
        }

        set
        {
        }
    }

    void Bar()
    {
        Foo = Foo + 1;
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_UpdateSetParameterName_1()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    int [||]GetFoo()
    {
    }

    void SetFoo(int i)
    {
        v = i;
    }
}",
@"using System;

class C
{
    int Foo
    {
        get
        {
        }

        set
        {
            v = value;
        }
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_UpdateSetParameterName_2()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    int [||]GetFoo()
    {
    }

    void SetFoo(int value)
    {
        v = value;
    }
}",
@"using System;

class C
{
    int Foo
    {
        get
        {
        }

        set
        {
            v = value;
        }
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_SetReferenceInSetter()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    int [||]GetFoo()
    {
    }

    void SetFoo(int i)
    {
        SetFoo(i - 1);
    }
}",
@"using System;

class C
{
    int Foo
    {
        get
        {
        }

        set
        {
            Foo = value - 1;
        }
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestVirtualGetWithOverride_1()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    protected virtual int [||]GetFoo()
    {
    }
}

class D : C
{
    protected override int GetFoo()
    {
    }
}",
@"class C
{
    protected virtual int Foo
    {
        get
        {
        }
    }
}

class D : C
{
    protected override int Foo
    {
        get
        {
        }
    }
}",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestVirtualGetWithOverride_2()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    protected virtual int [||]GetFoo()
    {
    }
}

class D : C
{
    protected override int GetFoo()
    {
        base.GetFoo();
    }
}",
@"class C
{
    protected virtual int Foo
    {
        get
        {
        }
    }
}

class D : C
{
    protected override int Foo
    {
        get
        {
            base.Foo;
        }
    }
}",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestGetWithInterface()
        {
            await TestWithAllCodeStyleOff(
@"interface I
{
    int [||]GetFoo();
}

class C : I
{
    public int GetFoo()
    {
    }
}",
@"interface I
{
    int Foo { get; }
}

class C : I
{
    public int Foo
    {
        get
        {
        }
    }
}",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestWithPartialClasses()
        {
            await TestWithAllCodeStyleOff(
@"partial class C
{
    int [||]GetFoo()
    {
    }
}

partial class C
{
    void SetFoo(int i)
    {
    }
}",
@"partial class C
{
    int Foo
    {
        get
        {
        }

        set
        {
        }
    }
}

partial class C
{
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSetCaseInsensitive()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    int [||]getFoo()
    {
    }

    void setFoo(int i)
    {
    }
}",
@"using System;

class C
{
    int Foo
    {
        get
        {
        }

        set
        {
        }
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task Tuple()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    (int, string) [||]GetFoo()
    {
    }
}",
@"class C
{
    (int, string) Foo
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task Tuple_GetAndSet()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    (int, string) [||]getFoo()
    {
    }

    void setFoo((int, string) i)
    {
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"using System;

class C
{
    (int, string) Foo
    {
        get
        {
        }

        set
        {
        }
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TupleWithNames_GetAndSet()
        {
            await TestWithAllCodeStyleOff(
@"using System;

class C
{
    (int a, string b) [||]getFoo()
    {
    }

    void setFoo((int a, string b) i)
    {
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"using System;

class C
{
    (int a, string b) Foo
    {
        get
        {
        }

        set
        {
        }
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TupleWithDifferentNames_GetAndSet()
        {
            // Cannot refactor tuples with different names together
            await Assert.ThrowsAsync<Xunit.Sdk.InRangeException>(() =>
                TestWithAllCodeStyleOff(
@"using System;

class C
{
    (int a, string b) [||]getFoo()
    {
    }

    void setFoo((int c, string d) i)
    {
    }
}",
@"",
index: 1));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestOutVarDeclaration_1()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    // Foo
    int [||]GetFoo()
    {
    }
    // SetFoo
    void SetFoo(out int i)
    {
    }

    void Test()
    {
        SetFoo(out int i);
    }
}",
@"class C
{
    // Foo
    int Foo
    {
        get
        {
        }
    }

    // SetFoo
    void SetFoo(out int i)
    {
    }

    void Test()
    {
        SetFoo(out int i);
    }
}",
index: 0,
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestOutVarDeclaration_2()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    // Foo
    int [||]GetFoo()
    {
    }
    // SetFoo
    void SetFoo(int i)
    {
    }

    void Test()
    {
        SetFoo(out int i);
    }
}",
@"class C
{
    // Foo
    // SetFoo
    int Foo
    {
        get
        {
        }

        set
        {
        }
    }

    void Test()
    {
        {|Conflict:Foo|}(out int i);
    }
}",
index: 1,
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestOutVarDeclaration_3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    // Foo
    int GetFoo()
    {
    }

    // SetFoo
    void [||]SetFoo(out int i)
    {
    }

    void Test()
    {
        SetFoo(out int i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestOutVarDeclaration_4()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    // Foo
    int [||]GetFoo(out int i)
    {
    }

    // SetFoo
    void SetFoo(out int i, int j)
    {
    }

    void Test()
    {
        var y = GetFoo(out int i);
    }
}");
        }

        [WorkItem(14327, "https://github.com/dotnet/roslyn/issues/14327")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateChainedGet1()
        {
            await TestWithAllCodeStyleOff(
@"public class Foo
{
    public Foo()
    {
        Foo value = GetValue().GetValue();
    }

    public Foo [||]GetValue()
    {
        return this;
    }
}",
@"public class Foo
{
    public Foo()
    {
        Foo value = Value.Value;
    }

    public Foo Value
    {
        get
        {
            return this;
        }
    }
}");
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo()
    {
        return 1;
    }
}",
@"class C
{
    int Foo
    {
        get => 1;
    }
}", options: PreferExpressionBodiedAccessors);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo()
    {
        return 1;
    }
}",
@"class C
{
    int Foo => 1;
}", options: PreferExpressionBodiedProperties);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo()
    {
        return 1;
    }
}",
@"class C
{
    int Foo => 1;
}", options: PreferExpressionBodiedAccessorsAndProperties);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo()
    {
        return 1;
    }

    void SetFoo(int i)
    {
        _i = i;
    }
}",
@"class C
{
    int Foo
    {
        get => 1;
        set => _i = value;
    }
}", 
index: 1,
options: PreferExpressionBodiedAccessors);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo()
    {
        return 1;
    }

    void SetFoo(int i)
    {
        _i = i;
    }
}",
@"class C
{
    int Foo
    {
        get { return 1; }
        set { _i = value; }
    }
}", 
index: 1,
options: PreferExpressionBodiedProperties);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo()
    {
        return 1;
    }

    void SetFoo(int i)
    {
        _i = i;
    }
}",
@"class C
{
    int Foo
    {
        get => 1;
        set => _i = value;
    }
}",
index: 1,
options: PreferExpressionBodiedAccessorsAndProperties);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle7()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo() => 0;
}",
@"class C
{
    int Foo => 0;
}", options: PreferExpressionBodiedProperties);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle8()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo() => 0;
}",
@"class C
{
    int Foo { get => 0; }
}", options: PreferExpressionBodiedAccessors);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle9()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo() => throw e;
}",
@"class C
{
    int Foo { get => throw e; }
}", options: PreferExpressionBodiedAccessors);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle10()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo() { throw e; }
}",
@"class C
{
    int Foo => throw e;
}", options: PreferExpressionBodiedProperties);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUseExpressionBodyWhenOnSingleLine_AndIsSingleLine()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo() { throw e; }
}",
@"class C
{
    int Foo => throw e;
}", options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenOnSingleLineWithNoneEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUseExpressionBodyWhenOnSingleLine_AndIsNotSingleLine()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetFoo() { throw e +
        e; }
}",
@"class C
{
    int Foo
    {
        get
        {
            throw e + 
                e;
        }
    }
}", options: OptionsSet(
    SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenOnSingleLineWithNoneEnforcement),
    SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenOnSingleLineWithNoneEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestExplicitInterfaceImplementation()
        {
            await TestWithAllCodeStyleOff(
@"interface IFoo
{
    int [||]GetFoo();
}

class C : IFoo
{
    int IFoo.GetFoo()
    {
        throw new System.NotImplementedException();
    }
}",
@"interface IFoo
{
    int Foo { get; }
}

class C : IFoo
{
    int IFoo.Foo
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        private async Task TestWithAllCodeStyleOff(
            string initialMarkup, string expectedMarkup, 
            ParseOptions parseOptions = null, int index = 0, 
            bool ignoreTrivia = true)
        {
            await TestAsync(
                initialMarkup, expectedMarkup, parseOptions,
                index: index,
                ignoreTrivia: ignoreTrivia,
                options: AllCodeStyleOff);
        }

        private IDictionary<OptionKey, object> AllCodeStyleOff =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                       SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        private IDictionary<OptionKey, object> PreferExpressionBodiedAccessors =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement),
                       SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        private IDictionary<OptionKey, object> PreferExpressionBodiedProperties =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                       SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement));

        private IDictionary<OptionKey, object> PreferExpressionBodiedAccessorsAndProperties =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement),
                       SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement));
    }
}
