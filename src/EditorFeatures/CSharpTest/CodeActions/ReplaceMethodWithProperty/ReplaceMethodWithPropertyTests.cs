// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ReplaceMethodWithProperty;
using Microsoft.CodeAnalysis.Test.Utilities;
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
    int [||]GetGoo()
    {
    }
}",
@"class C
{
    int Goo
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
    int [||]Goo()
    {
    }
}",
@"class C
{
    int Goo
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
    int [||]GetGoo() => 0;
}",
@"class C
{
    int Goo
    {
        get
        {
            return 0;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithoutBody()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetGoo();
}",
@"class C
{
    int Goo { get; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithModifiers()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    public static int [||]GetGoo()
    {
    }
}",
@"class C
{
    public static int Goo
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
    int [||]GetGoo()
    {
    }
}",
@"class C
{
    [A]
    int Goo
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
    // Goo
    int [||]GetGoo()
    {
    }
}",
@"class C
{
    // Goo
    int Goo
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithTrailingTrivia()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetP();
    bool M()
    {
        return GetP() == 0;
    }
}",
@"class C
{
    int P { get; }

    bool M()
    {
        return P == 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestDelegateWithTrailingTrivia()
        {
            await TestWithAllCodeStyleOff(
@"delegate int Mdelegate();
class C
{
    int [||]GetP() => 0;

    void M()
    {
        Mdelegate del = new Mdelegate(GetP );
    }
}",
@"delegate int Mdelegate();
class C
{
    int P
    {
        get
        {
            return 0;
        }
    }

    void M()
    {
        Mdelegate del = new Mdelegate({|Conflict:P|} );
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestIndentation()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetGoo()
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
    int Goo
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
}");
        }

        [WorkItem(21460, "https://github.com/dotnet/roslyn/issues/21460")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestIfDefMethod1()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
#if true
    int [||]GetGoo()
    {
    }
#endif
}",
@"class C
{
#if true
    int Goo
    {
        get
        {
        }
    }
#endif
}");
        }

        [WorkItem(21460, "https://github.com/dotnet/roslyn/issues/21460")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestIfDefMethod2()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
#if true
    int [||]GetGoo()
    {
    }

    void SetGoo(int val)
    {
    }
#endif
}",
@"class C
{
#if true
    int Goo
    {
        get
        {
        }
    }

    void SetGoo(int val)
    {
    }
#endif
}");
        }

        [WorkItem(21460, "https://github.com/dotnet/roslyn/issues/21460")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestIfDefMethod3()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
#if true
    int [||]GetGoo()
    {
    }

    void SetGoo(int val)
    {
    }
#endif
}",
@"class C
{
#if true
    int Goo
    {
        get
        {
        }

        set
        {
        }
    }
#endif
}", index: 1);
        }

        [WorkItem(21460, "https://github.com/dotnet/roslyn/issues/21460")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestIfDefMethod4()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
#if true
    void SetGoo(int val)
    {
    }

    int [||]GetGoo()
    {
    }
#endif
}",
@"class C
{
#if true
    void SetGoo(int val)
    {
    }

    int Goo
    {
        get
        {
        }
    }
#endif
}");
        }

        [WorkItem(21460, "https://github.com/dotnet/roslyn/issues/21460")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestIfDefMethod5()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
#if true
    void SetGoo(int val)
    {
    }

    int [||]GetGoo()
    {
    }
#endif
}",
@"class C
{

#if true

    int Goo
    {
        get
        {
        }

        set
        {
        }
    }
#endif
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithTrivia_2()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    // Goo
    int [||]GetGoo()
    {
    }
    // SetGoo
    void SetGoo(int i)
    {
    }
}",
@"class C
{
    // Goo
    // SetGoo
    int Goo
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
        public async Task TestExplicitInterfaceMethod_1()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]I.GetGoo()
    {
    }
}",
@"class C
{
    int I.Goo
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
    int GetGoo();
}

class C : I
{
    int [||]I.GetGoo()
    {
    }
}",
@"interface I
{
    int Goo { get; }
}

class C : I
{
    int I.Goo
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
    int [||]GetGoo();
}

class C : I
{
    int I.GetGoo()
    {
    }
}",
@"interface I
{
    int Goo { get; }
}

class C : I
{
    int I.Goo
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
    int GetGoo()
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
    int GetGoo()
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
    void [||]GetGoo()
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
    async Task [||]GetGoo()
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
    int [||]GetGoo<T>()
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
    int [||]GetGoo(this int i)
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
    int [||]GetGoo(int i)
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
    int [||]GetGoo(int i = 0)
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
    int GetGoo()
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
    int GetGoo()
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
    int [||]GetGoo()
    {
    }

    void Bar()
    {
        var x = GetGoo();
    }
}",
@"class C
{
    int Goo
    {
        get
        {
        }
    }

    void Bar()
    {
        var x = Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceSimpleInvocation()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetGoo()
    {
    }

    void Bar()
    {
        var x = GetGoo();
    }
}",
@"class C
{
    int Goo
    {
        get
        {
        }
    }

    void Bar()
    {
        var x = Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceMemberAccessInvocation()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetGoo()
    {
    }

    void Bar()
    {
        var x = this.GetGoo();
    }
}",
@"class C
{
    int Goo
    {
        get
        {
        }
    }

    void Bar()
    {
        var x = this.Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceBindingMemberInvocation()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetGoo()
    {
    }

    void Bar()
    {
        C x;
        var v = x?.GetGoo();
    }
}",
@"class C
{
    int Goo
    {
        get
        {
        }
    }

    void Bar()
    {
        C x;
        var v = x?.Goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceInMethod()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    int [||]GetGoo()
    {
        return GetGoo();
    }
}",
@"class C
{
    int Goo
    {
        get
        {
            return Goo;
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
    public virtual int [||]GetGoo()
    {
    }
}

class D : C
{
    public override int GetGoo()
    {
    }
}",
@"class C
{
    public virtual int Goo
    {
        get
        {
        }
    }
}

class D : C
{
    public override int Goo
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
    int [||]GetGoo()
    {
    }

    void Bar()
    {
        Action<int> i = GetGoo;
    }
}",
@"using System;

class C
{
    int Goo
    {
        get
        {
        }
    }

    void Bar()
    {
        Action<int> i = {|Conflict:Goo|};
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
    int [||]GetGoo()
    {
    }

    void SetGoo(int i)
    {
    }
}",
@"using System;

class C
{
    int Goo
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
    int [||]GetGoo()
    {
    }

    void SetGoo(int i)
    {
    }

    void Bar()
    {
        Action<int> i = SetGoo;
    }
}",
@"using System;

class C
{
    int Goo
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
        Action<int> i = {|Conflict:Goo|};
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
    public int [||]GetGoo()
    {
    }

    private void SetGoo(int i)
    {
    }
}",
@"using System;

class C
{
    public int Goo
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
    int [||]GetGoo() => 0;
    void SetGoo(int i) => Bar();
}",
@"using System;

class C
{
    int Goo
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
    int [||]GetGoo()
    {
    }

    void SetGoo(int i)
    {
    }

    void Bar()
    {
        SetGoo(GetGoo() + 1);
    }
}",
@"using System;

class C
{
    int Goo
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
        Goo = Goo + 1;
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
    int [||]GetGoo()
    {
    }

    void SetGoo(int i)
    {
        v = i;
    }
}",
@"using System;

class C
{
    int Goo
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
    int [||]GetGoo()
    {
    }

    void SetGoo(int value)
    {
        v = value;
    }
}",
@"using System;

class C
{
    int Goo
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
    int [||]GetGoo()
    {
    }

    void SetGoo(int i)
    {
        SetGoo(i - 1);
    }
}",
@"using System;

class C
{
    int Goo
    {
        get
        {
        }

        set
        {
            Goo = value - 1;
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
    protected virtual int [||]GetGoo()
    {
    }
}

class D : C
{
    protected override int GetGoo()
    {
    }
}",
@"class C
{
    protected virtual int Goo
    {
        get
        {
        }
    }
}

class D : C
{
    protected override int Goo
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
    protected virtual int [||]GetGoo()
    {
    }
}

class D : C
{
    protected override int GetGoo()
    {
        base.GetGoo();
    }
}",
@"class C
{
    protected virtual int Goo
    {
        get
        {
        }
    }
}

class D : C
{
    protected override int Goo
    {
        get
        {
            base.Goo;
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
    int [||]GetGoo();
}

class C : I
{
    public int GetGoo()
    {
    }
}",
@"interface I
{
    int Goo { get; }
}

class C : I
{
    public int Goo
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
    int [||]GetGoo()
    {
    }
}

partial class C
{
    void SetGoo(int i)
    {
    }
}",
@"partial class C
{
    int Goo
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
    int [||]getGoo()
    {
    }

    void setGoo(int i)
    {
    }
}",
@"using System;

class C
{
    int Goo
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
    (int, string) [||]GetGoo()
    {
    }
}",
@"class C
{
    (int, string) Goo
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
    (int, string) [||]getGoo()
    {
    }

    void setGoo((int, string) i)
    {
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"using System;

class C
{
    (int, string) Goo
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
    (int a, string b) [||]getGoo()
    {
    }

    void setGoo((int a, string b) i)
    {
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"using System;

class C
{
    (int a, string b) Goo
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
            await TestActionCountAsync(
@"using System;

class C
{
    (int a, string b) [||]getGoo()
    {
    }

    void setGoo((int c, string d) i)
    {
    }
}",
count: 1, new TestParameters(options: AllCodeStyleOff));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestOutVarDeclaration_1()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    // Goo
    int [||]GetGoo()
    {
    }
    // SetGoo
    void SetGoo(out int i)
    {
    }

    void Test()
    {
        SetGoo(out int i);
    }
}",
@"class C
{
    // Goo
    int Goo
    {
        get
        {
        }
    }

    // SetGoo
    void SetGoo(out int i)
    {
    }

    void Test()
    {
        SetGoo(out int i);
    }
}",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestOutVarDeclaration_2()
        {
            await TestWithAllCodeStyleOff(
@"class C
{
    // Goo
    int [||]GetGoo()
    {
    }
    // SetGoo
    void SetGoo(int i)
    {
    }

    void Test()
    {
        SetGoo(out int i);
    }
}",
@"class C
{
    // Goo
    // SetGoo
    int Goo
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
        {|Conflict:Goo|}(out int i);
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestOutVarDeclaration_3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    // Goo
    int GetGoo()
    {
    }

    // SetGoo
    void [||]SetGoo(out int i)
    {
    }

    void Test()
    {
        SetGoo(out int i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestOutVarDeclaration_4()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    // Goo
    int [||]GetGoo(out int i)
    {
    }

    // SetGoo
    void SetGoo(out int i, int j)
    {
    }

    void Test()
    {
        var y = GetGoo(out int i);
    }
}");
        }

        [WorkItem(14327, "https://github.com/dotnet/roslyn/issues/14327")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateChainedGet1()
        {
            await TestWithAllCodeStyleOff(
@"public class Goo
{
    public Goo()
    {
        Goo value = GetValue().GetValue();
    }

    public Goo [||]GetValue()
    {
        return this;
    }
}",
@"public class Goo
{
    public Goo()
    {
        Goo value = Value.Value;
    }

    public Goo Value
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
    int [||]GetGoo()
    {
        return 1;
    }
}",
@"class C
{
    int Goo { get => 1; }
}", options: PreferExpressionBodiedAccessors);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetGoo()
    {
        return 1;
    }
}",
@"class C
{
    int Goo => 1;
}", options: PreferExpressionBodiedProperties);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetGoo()
    {
        return 1;
    }
}",
@"class C
{
    int Goo => 1;
}", options: PreferExpressionBodiedAccessorsAndProperties);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetGoo()
    {
        return 1;
    }

    void SetGoo(int i)
    {
        _i = i;
    }
}",
@"class C
{
    int Goo { get => 1; set => _i = value; }
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
    int [||]GetGoo()
    {
        return 1;
    }

    void SetGoo(int i)
    {
        _i = i;
    }
}",
@"class C
{
    int Goo
    {
        get
        {
            return 1;
        }

        set
        {
            _i = value;
        }
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
    int [||]GetGoo()
    {
        return 1;
    }

    void SetGoo(int i)
    {
        _i = i;
    }
}",
@"class C
{
    int Goo { get => 1; set => _i = value; }
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
    int [||]GetGoo() => 0;
}",
@"class C
{
    int Goo => 0;
}", options: PreferExpressionBodiedProperties);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle8()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetGoo() => 0;
}",
@"class C
{
    int Goo { get => 0; }
}", options: PreferExpressionBodiedAccessors);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle9()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetGoo() => throw e;
}",
@"class C
{
    int Goo { get => throw e; }
}", options: PreferExpressionBodiedAccessors);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestCodeStyle10()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetGoo() { throw e; }
}",
@"class C
{
    int Goo => throw e;
}", options: PreferExpressionBodiedProperties);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUseExpressionBodyWhenOnSingleLine_AndIsSingleLine()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetGoo() { throw e; }
}",
@"class C
{
    int Goo => throw e;
}", options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUseExpressionBodyWhenOnSingleLine_AndIsNotSingleLine()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]GetGoo() { throw e +
        e; }
}",
@"class C
{
    int Goo
    {
        get
        {
            throw e +
   e;
        }
    }
}", options: OptionsSet(
    SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement),
    SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestExplicitInterfaceImplementation()
        {
            await TestWithAllCodeStyleOff(
@"interface IGoo
{
    int [||]GetGoo();
}

class C : IGoo
{
    int IGoo.GetGoo()
    {
        throw new System.NotImplementedException();
    }
}",
@"interface IGoo
{
    int Goo { get; }
}

class C : IGoo
{
    int IGoo.Goo
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }
}");
        }

        [WorkItem(443523, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=443523")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestSystemObjectMetadataOverride()
        {
            await TestMissingAsync(
@"class C
{
    public override string [||]ToString()
    {
    }
}");
        }

        [WorkItem(443523, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=443523")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMetadataOverride()
        {
            await TestWithAllCodeStyleOff(
@"class C : System.Type
{
    public override int [||]GetArrayRank()
    {
    }
}",
@"class C : System.Type
{
    public override int {|Warning:ArrayRank|}
    {
        get
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task IgnoreIfTopLevelNullableIsDifferent_GetterNullable()
        {
            await TestInRegularAndScriptAsync(
@"
#nullable enable

class C
{
    private string? name;

    public void SetName(string name)
    {
        this.name = name;
    }

    public string? [||]GetName()
    {
        return this.name;
    }
}",
@"
#nullable enable

class C
{
    private string? name;

    public void SetName(string name)
    {
        this.name = name;
    }

    public string? Name => this.name;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task IgnoreIfTopLevelNullableIsDifferent_SetterNullable()
        {
            await TestInRegularAndScriptAsync(
@"
#nullable enable

class C
{
    private string? name;

    public void SetName(string? name)
    {
        this.name = name;
    }

    public string [||]GetName()
    {
        return this.name ?? """";
    }
}",
@"
#nullable enable

class C
{
    private string? name;

    public void SetName(string? name)
    {
        this.name = name;
    }

    public string Name => this.name ?? """";
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task IgnoreIfNestedNullableIsDifferent_GetterNullable()
        {
            await TestInRegularAndScriptAsync(
@"
#nullable enable

class C
{
    private IEnumerable<string?> names;

    public void SetNames(IEnumerable<string> names)
    {
        this.names = names;
    }

    public IEnumerable<string?> [||]GetNames()
    {
        return this.names;
    }
}",
@"
#nullable enable

class C
{
    private IEnumerable<string?> names;

    public void SetNames(IEnumerable<string> names)
    {
        this.names = names;
    }

    public IEnumerable<string?> Names => this.names;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task IgnoreIfNestedNullableIsDifferent_SetterNullable()
        {
            await TestInRegularAndScriptAsync(
@"
#nullable enable

using System.Linq;

class C
{
    private IEnumerable<string?> names;

    public void SetNames(IEnumerable<string?> names)
    {
        this.names = names;
    }

    public IEnumerable<string> [||]GetNames()
    {
        return this.names.Where(n => n is object);
    }
}",
@"
#nullable enable

using System.Linq;

class C
{
    private IEnumerable<string?> names;

    public void SetNames(IEnumerable<string?> names)
    {
        this.names = names;
    }

    public IEnumerable<string> Names => this.names.Where(n => n is object);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task NullabilityOfFieldDifferentThanProperty()
        {
            await TestInRegularAndScriptAsync(
@"
#nullable enable

class C
{
    private string name;
    
    public string? [||]GetName()
    {
        return name;
    }
}",
@"
#nullable enable

class C
{
    private string name;

    public string? Name => name;
}");
        }

        private async Task TestWithAllCodeStyleOff(
            string initialMarkup, string expectedMarkup,
            ParseOptions parseOptions = null, int index = 0)
        {
            await TestAsync(
                initialMarkup, expectedMarkup, parseOptions,
                index: index,
                options: AllCodeStyleOff);
        }

        private IDictionary<OptionKey, object> AllCodeStyleOff =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement),
                       SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement));

        private IDictionary<OptionKey, object> PreferExpressionBodiedAccessors =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement),
                       SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement));

        private IDictionary<OptionKey, object> PreferExpressionBodiedProperties =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement),
                       SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement));

        private IDictionary<OptionKey, object> PreferExpressionBodiedAccessorsAndProperties =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement),
                       SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement));
    }
}
