// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.ReplaceMethodWithProperty;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ReplaceMethodWithProperty;

[Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
public sealed class ReplaceMethodWithPropertyTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new ReplaceMethodWithPropertyCodeRefactoringProvider();

    private Task TestWithAllCodeStyleOff(
        string initialMarkup, string expectedMarkup,
        ParseOptions? parseOptions = null, int index = 0)
        => TestAsync(
            initialMarkup, expectedMarkup, new(parseOptions, index: index, options: AllCodeStyleOff));

    private OptionsCollection AllCodeStyleOff
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    private OptionsCollection PreferExpressionBodiedAccessors
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    private OptionsCollection PreferExpressionBodiedProperties
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement },
        };

    private OptionsCollection PreferExpressionBodiedAccessorsAndProperties
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement },
        };

    [Fact]
    public Task TestMethodWithGetName()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]GetGoo()
                {
                }
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMethodWithoutGetName()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]Goo()
                {
                }
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6034")]
    public Task TestMethodWithArrowBody()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]GetGoo() => 0;
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        return 0;
                    }
                }
            }
            """);

    [Fact]
    public Task TestMethodWithoutBody()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]GetGoo();
            }
            """,
            """
            class C
            {
                int Goo { get; }
            }
            """);

    [Fact]
    public Task TestMethodWithModifiers()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                public static int [||]GetGoo()
                {
                }
            }
            """,
            """
            class C
            {
                public static int Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMethodWithAttributes()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                [A]
                int [||]GetGoo()
                {
                }
            }
            """,
            """
            class C
            {
                [A]
                int Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMethodWithTrivia_1()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                // Goo
                int [||]GetGoo()
                {
                }
            }
            """,
            """
            class C
            {
                // Goo
                int Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMethodWithTrailingTrivia()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]GetP();
                bool M()
                {
                    return GetP() == 0;
                }
            }
            """,
            """
            class C
            {
                int P { get; }

                bool M()
                {
                    return P == 0;
                }
            }
            """);

    [Fact]
    public Task TestDelegateWithTrailingTrivia()
        => TestWithAllCodeStyleOff(
            """
            delegate int Mdelegate();
            class C
            {
                int [||]GetP() => 0;

                void M()
                {
                    Mdelegate del = new Mdelegate(GetP );
                }
            }
            """,
            """
            delegate int Mdelegate();
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
            }
            """);

    [Fact]
    public Task TestIndentation()
        => TestWithAllCodeStyleOff(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21460")]
    public Task TestIfDefMethod1()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
            #if true
                int [||]GetGoo()
                {
                }
            #endif
            }
            """,
            """
            class C
            {
            #if true
                int Goo
                {
                    get
                    {
                    }
                }
            #endif
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21460")]
    public Task TestIfDefMethod2()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
            #if true
                int [||]GetGoo()
                {
                }

                void SetGoo(int val)
                {
                }
            #endif
            }
            """,
            """
            class C
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21460")]
    public Task TestIfDefMethod3()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
            #if true
                int [||]GetGoo()
                {
                }

                void SetGoo(int val)
                {
                }
            #endif
            }
            """,
            """
            class C
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
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21460")]
    public Task TestIfDefMethod4()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
            #if true
                void SetGoo(int val)
                {
                }

                int [||]GetGoo()
                {
                }
            #endif
            }
            """,
            """
            class C
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21460")]
    public Task TestIfDefMethod5()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
            #if true
                void SetGoo(int val)
                {
                }

                int [||]GetGoo()
                {
                }
            #endif
            }
            """,
            """
            class C
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
            }
            """, index: 1);

    [Fact]
    public Task TestMethodWithTrivia_2()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                // Goo
                int [||]GetGoo()
                {
                }
                // SetGoo
                void SetGoo(int i)
                {
                }
            }
            """,
            """
            class C
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
            }
            """,
            index: 1);

    [Fact]
    public Task TestExplicitInterfaceMethod_1()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]I.GetGoo()
                {
                }
            }
            """,
            """
            class C
            {
                int I.Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestExplicitInterfaceMethod_2()
        => TestWithAllCodeStyleOff(
            """
            interface I
            {
                int GetGoo();
            }

            class C : I
            {
                int [||]I.GetGoo()
                {
                }
            }
            """,
            """
            interface I
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
            }
            """);

    [Fact]
    public Task TestExplicitInterfaceMethod_3()
        => TestWithAllCodeStyleOff(
            """
            interface I
            {
                int [||]GetGoo();
            }

            class C : I
            {
                int I.GetGoo()
                {
                }
            }
            """,
            """
            interface I
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
            }
            """);

    [Fact]
    public Task TestInAttribute()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [At[||]tr]
                int GetGoo()
                {
                }
            }
            """);

    [Fact]
    public Task TestInMethod()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int GetGoo()
                {
            [||]
                }
            }
            """);

    [Fact]
    public Task TestVoidMethod()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void [||]GetGoo()
                {
                }
            }
            """);

    [Fact]
    public Task TestAsyncMethod()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                async Task [||]GetGoo()
                {
                }
            }
            """);

    [Fact]
    public Task TestGenericMethod()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo<T>()
                {
                }
            }
            """);

    [Fact]
    public Task TestExtensionMethod()
        => TestMissingInRegularAndScriptAsync(
            """
            static class C
            {
                int [||]GetGoo(this int i)
                {
                }
            }
            """);

    [Fact]
    public Task TestMethodWithParameters_1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo(int i)
                {
                }
            }
            """);

    [Fact]
    public Task TestMethodWithParameters_2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo(int i = 0)
                {
                }
            }
            """);

    [Fact]
    public Task TestNotInSignature_1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [At[||]tr]
                int GetGoo()
                {
                }
            }
            """);

    [Fact]
    public Task TestNotInSignature_2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int GetGoo()
                {
            [||]
                }
            }
            """);

    [Fact]
    public Task TestUpdateGetReferenceNotInMethod()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]GetGoo()
                {
                }

                void Bar()
                {
                    var x = GetGoo();
                }
            }
            """,
            """
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
                    var x = Goo;
                }
            }
            """);

    [Fact]
    public Task TestUpdateGetReferenceSimpleInvocation()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]GetGoo()
                {
                }

                void Bar()
                {
                    var x = GetGoo();
                }
            }
            """,
            """
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
                    var x = Goo;
                }
            }
            """);

    [Fact]
    public Task TestUpdateGetReferenceMemberAccessInvocation()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]GetGoo()
                {
                }

                void Bar()
                {
                    var x = this.GetGoo();
                }
            }
            """,
            """
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
                    var x = this.Goo;
                }
            }
            """);

    [Fact]
    public Task TestUpdateGetReferenceBindingMemberInvocation()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]GetGoo()
                {
                }

                void Bar()
                {
                    C x;
                    var v = x?.GetGoo();
                }
            }
            """,
            """
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
                    C x;
                    var v = x?.Goo;
                }
            }
            """);

    [Fact]
    public Task TestUpdateGetReferenceInMethod()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]GetGoo()
                {
                    return GetGoo();
                }
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        return Goo;
                    }
                }
            }
            """);

    [Fact]
    public Task TestOverride()
        => TestWithAllCodeStyleOff(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    public Task TestUpdateGetReference_NonInvoked()
        => TestWithAllCodeStyleOff(
            """
            using System;

            class C
            {
                int [||]GetGoo()
                {
                }

                void Bar()
                {
                    Action<int> i = GetGoo;
                }
            }
            """,
            """
            using System;

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
            }
            """);

    [Fact]
    public Task TestUpdateGetReference_ImplicitReference()
        => TestWithAllCodeStyleOff(
            """
            using System.Collections;

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
            }
            """,
            """
            using System.Collections;

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
            }
            """);

    [Fact]
    public Task TestUpdateGetSet()
        => TestWithAllCodeStyleOff(
            """
            using System;

            class C
            {
                int [||]GetGoo()
                {
                }

                void SetGoo(int i)
                {
                }
            }
            """,
            """
            using System;

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
            }
            """,
            index: 1);

    [Fact]
    public Task TestUpdateGetSetReference_NonInvoked()
        => TestWithAllCodeStyleOff(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """,
            index: 1);

    [Fact]
    public Task TestUpdateGetSet_SetterAccessibility()
        => TestWithAllCodeStyleOff(
            """
            using System;

            class C
            {
                public int [||]GetGoo()
                {
                }

                private void SetGoo(int i)
                {
                }
            }
            """,
            """
            using System;

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
            }
            """,
            index: 1);

    [Fact]
    public Task TestUpdateGetSet_ExpressionBodies()
        => TestWithAllCodeStyleOff(
            """
            using System;

            class C
            {
                int [||]GetGoo() => 0;
                void SetGoo(int i) => Bar();
            }
            """,
            """
            using System;

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
            }
            """,
            index: 1);

    [Fact]
    public Task TestUpdateGetSet_GetInSetReference()
        => TestWithAllCodeStyleOff(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """,
            index: 1);

    [Fact]
    public Task TestUpdateGetSet_UpdateSetParameterName_1()
        => TestWithAllCodeStyleOff(
            """
            using System;

            class C
            {
                int [||]GetGoo()
                {
                }

                void SetGoo(int i)
                {
                    v = i;
                }
            }
            """,
            """
            using System;

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
            }
            """,
            index: 1);

    [Fact]
    public Task TestUpdateGetSet_UpdateSetParameterName_2()
        => TestWithAllCodeStyleOff(
            """
            using System;

            class C
            {
                int [||]GetGoo()
                {
                }

                void SetGoo(int value)
                {
                    v = value;
                }
            }
            """,
            """
            using System;

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
            }
            """,
            index: 1);

    [Fact]
    public Task TestUpdateGetSet_SetReferenceInSetter()
        => TestWithAllCodeStyleOff(
            """
            using System;

            class C
            {
                int [||]GetGoo()
                {
                }

                void SetGoo(int i)
                {
                    SetGoo(i - 1);
                }
            }
            """,
            """
            using System;

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
            }
            """,
            index: 1);

    [Fact]
    public Task TestVirtualGetWithOverride_1()
        => TestWithAllCodeStyleOff(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """,
            index: 0);

    [Fact]
    public Task TestVirtualGetWithOverride_2()
        => TestWithAllCodeStyleOff(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """,
            index: 0);

    [Fact]
    public Task TestGetWithInterface()
        => TestWithAllCodeStyleOff(
            """
            interface I
            {
                int [||]GetGoo();
            }

            class C : I
            {
                public int GetGoo()
                {
                }
            }
            """,
            """
            interface I
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
            }
            """,
            index: 0);

    [Fact]
    public Task TestWithPartialClasses()
        => TestWithAllCodeStyleOff(
            """
            partial class C
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
            }
            """,
            """
            partial class C
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
            }
            """,
            index: 1);

    [Fact]
    public Task TestUpdateGetSetCaseInsensitive()
        => TestWithAllCodeStyleOff(
            """
            using System;

            class C
            {
                int [||]getGoo()
                {
                }

                void setGoo(int i)
                {
                }
            }
            """,
            """
            using System;

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
            }
            """,
            index: 1);

    [Fact]
    public Task Tuple()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                (int, string) [||]GetGoo()
                {
                }
            }
            """,
            """
            class C
            {
                (int, string) Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task Tuple_GetAndSet()
        => TestWithAllCodeStyleOff(
            """
            using System;

            class C
            {
                (int, string) [||]getGoo()
                {
                }

                void setGoo((int, string) i)
                {
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            using System;

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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
index: 1);

    [Fact]
    public Task TupleWithNames_GetAndSet()
        => TestWithAllCodeStyleOff(
            """
            using System;

            class C
            {
                (int a, string b) [||]getGoo()
                {
                }

                void setGoo((int a, string b) i)
                {
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            using System;

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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
index: 1);

    [Fact]
    public Task TupleWithDifferentNames_GetAndSet()
        => TestActionCountAsync(
            """
            using System;

            class C
            {
                (int a, string b) [||]getGoo()
                {
                }

                void setGoo((int c, string d) i)
                {
                }
            }
            """,
            count: 1, new TestParameters(options: AllCodeStyleOff));

    [Fact]
    public Task TestOutVarDeclaration_1()
        => TestWithAllCodeStyleOff(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """,
            index: 0);

    [Fact]
    public Task TestOutVarDeclaration_2()
        => TestWithAllCodeStyleOff(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """,
            index: 1);

    [Fact]
    public Task TestOutVarDeclaration_3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact]
    public Task TestOutVarDeclaration_4()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14327")]
    public Task TestUpdateChainedGet1()
        => TestWithAllCodeStyleOff(
            """
            public class Goo
            {
                public Goo()
                {
                    Goo value = GetValue().GetValue();
                }

                public Goo [||]GetValue()
                {
                    return this;
                }
            }
            """,
            """
            public class Goo
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo()
                {
                    return 1;
                }
            }
            """,
            """
            class C
            {
                int Goo { get => 1; }
            }
            """, new(options: PreferExpressionBodiedAccessors));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo()
                {
                    return 1;
                }
            }
            """,
            """
            class C
            {
                int Goo => 1;
            }
            """, new(options: PreferExpressionBodiedProperties));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo()
                {
                    return 1;
                }
            }
            """,
            """
            class C
            {
                int Goo => 1;
            }
            """, new(options: PreferExpressionBodiedAccessorsAndProperties));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo()
                {
                    return 1;
                }

                void SetGoo(int i)
                {
                    _i = i;
                }
            }
            """,
            """
            class C
            {
                int Goo { get => 1; set => _i = value; }
            }
            """,
            index: 1,
            new(options: PreferExpressionBodiedAccessors));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo()
                {
                    return 1;
                }

                void SetGoo(int i)
                {
                    _i = i;
                }
            }
            """,
            """
            class C
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
            }
            """,
            index: 1,
            new(options: PreferExpressionBodiedProperties));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle6()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo()
                {
                    return 1;
                }

                void SetGoo(int i)
                {
                    _i = i;
                }
            }
            """,
            """
            class C
            {
                int Goo { get => 1; set => _i = value; }
            }
            """,
            index: 1,
            new(options: PreferExpressionBodiedAccessorsAndProperties));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle7()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo() => 0;
            }
            """,
            """
            class C
            {
                int Goo => 0;
            }
            """, new(options: PreferExpressionBodiedProperties));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo() => 0;
            }
            """,
            """
            class C
            {
                int Goo { get => 0; }
            }
            """, new(options: PreferExpressionBodiedAccessors));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo() => throw e;
            }
            """,
            """
            class C
            {
                int Goo { get => throw e; }
            }
            """, new(options: PreferExpressionBodiedAccessors));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle10()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo() { throw e; }
            }
            """,
            """
            class C
            {
                int Goo => throw e;
            }
            """, new(options: PreferExpressionBodiedProperties));

    [Fact]
    public Task TestUseExpressionBodyWhenOnSingleLine_AndIsSingleLine()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo() { throw e; }
            }
            """,
            """
            class C
            {
                int Goo => throw e;
            }
            """, new(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact]
    public Task TestUseExpressionBodyWhenOnSingleLine_AndIsNotSingleLine()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]GetGoo() { throw e +
                    e; }
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        throw e +
                    e;
                    }
                }
            }
            """, new(options: new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement },
            }));

    [Fact]
    public Task TestExplicitInterfaceImplementation()
        => TestWithAllCodeStyleOff(
            """
            interface IGoo
            {
                int [||]GetGoo();
            }

            class C : IGoo
            {
                int IGoo.GetGoo()
                {
                    throw new System.NotImplementedException();
                }
            }
            """,
            """
            interface IGoo
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
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=443523")]
    public Task TestSystemObjectMetadataOverride()
        => TestMissingAsync(
            """
            class C
            {
                public override string [||]ToString()
                {
                }
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=443523")]
    public Task TestMetadataOverride()
        => TestWithAllCodeStyleOff(
            """
            class C : System.Type
            {
                public override int [||]GetArrayRank()
                {
                }
            }
            """,
            """
            class C : System.Type
            {
                public override int {|Warning:ArrayRank|}
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task IgnoreIfTopLevelNullableIsDifferent_GetterNullable()
        => TestInRegularAndScriptAsync(
            """
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
            }
            """,
            """
            #nullable enable

            class C
            {
                private string? name;

                public void SetName(string name)
                {
                    this.name = name;
                }

                public string? Name => this.name;
            }
            """);

    [Fact]
    public Task IgnoreIfTopLevelNullableIsDifferent_SetterNullable()
        => TestInRegularAndScriptAsync(
            """
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
                    return this.name ?? "";
                }
            }
            """,
            """
            #nullable enable

            class C
            {
                private string? name;

                public void SetName(string? name)
                {
                    this.name = name;
                }

                public string Name => this.name ?? "";
            }
            """);

    [Fact]
    public Task IgnoreIfNestedNullableIsDifferent_GetterNullable()
        => TestInRegularAndScriptAsync(
            """
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
            }
            """,
            """
            #nullable enable

            class C
            {
                private IEnumerable<string?> names;

                public void SetNames(IEnumerable<string> names)
                {
                    this.names = names;
                }

                public IEnumerable<string?> Names => this.names;
            }
            """);

    [Fact]
    public Task IgnoreIfNestedNullableIsDifferent_SetterNullable()
        => TestInRegularAndScriptAsync(
            """
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
            }
            """,
            """
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
            }
            """);

    [Fact]
    public Task NullabilityOfFieldDifferentThanProperty()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                private string name;

                public string? [||]GetName()
                {
                    return name;
                }
            }
            """,
            """
            #nullable enable

            class C
            {
                private string name;

                public string? Name => name;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38379")]
    public Task TestUnsafeGetter()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public unsafe int [||]GetP()
                {
                    return 0;
                }

                public void SetP(int value)
                { }
            }
            """,
            """
            class C
            {
                public unsafe int P
                {
                    get => 0;
                    set
                    { }
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38379")]
    public Task TestUnsafeSetter()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]GetP()
                {
                    return 0;
                }

                public unsafe void SetP(int value)
                { }
            }
            """,
            """
            class C
            {
                public unsafe int P
                {
                    get => 0;
                    set
                    { }
                }
            }
            """, index: 1);

    [Fact]
    public Task TestAtStartOfMethod()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                [||]int GetGoo()
                {
                }
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestBeforeStartOfMethod_OnSameLine()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
            [||]    int GetGoo()
                {
                }
            }
            """,
            """
            class C
            {
                int Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestBeforeStartOfMethod_OnPreviousLine()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                [||]
                int GetGoo()
                {
                }
            }
            """,
            """
            class C
            {

                int Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestBeforeStartOfMethod_NotMultipleLinesPrior()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [||]

                int GetGoo()
                {
                }
            }
            """);

    [Fact]
    public Task TestBeforeStartOfMethod_NotBeforeAttributes()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                [||][A]
                int GetGoo()
                {
                }
            }
            """,
            """
            class C
            {
                [A]
                int Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestBeforeStartOfMethod_NotBeforeComments()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                [||] /// <summary/>
                int GetGoo()
                {
                }
            }
            """);

    [Fact]
    public Task TestBeforeStartOfMethod_NotInComment()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                /// [||]<summary/>
                int GetGoo()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42699")]
    public Task TestSameNameMemberAsProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int Goo;
                [||]int GetGoo()
                {
                }
            }
            """,
            """
            class C
            {
                int Goo;
                int Goo1
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42698")]
    public Task TestMethodWithTrivia_3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                [||]int Goo() //Vital Comment
                {
                  return 1;
                }
            }
            """,
            """
            class C
            {
                //Vital Comment
                int Goo => 1;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42698")]
    public Task TestMethodWithTrivia_4()
        => TestWithAllCodeStyleOff(
            """
            class C
            {
                int [||]GetGoo()    // Goo
                {
                }
                void SetGoo(int i)    // SetGoo
                {
                }
            }
            """,
            """
            class C
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
            }
            """,
            index: 1);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/57769")]
    public Task TestInLinkedFile()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.1'>
                    <Document FilePath='C.cs'>
            class C
            {
                int [||]GetP();
                bool M()
                {
                    return GetP() == 0;
                }
            }
                    </Document>
                </Project>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.2'>
                    <Document IsLinkFile='true' LinkProjectName='CSProj.1' LinkFilePath='C.cs'/>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.1'>
                    <Document FilePath='C.cs'>
            class C
            {
                int P { get; }

                bool M()
                {
                    return P == 0;
                }
            }
                    </Document>
                </Project>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.2'>
                    <Document IsLinkFile='true' LinkProjectName='CSProj.1' LinkFilePath='C.cs'/>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37991")]
    public Task AllowIfNestedNullableIsSame()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            using System.Linq;

            class C
            {
                private IEnumerable<string?> names;

                public void SetNames(IEnumerable<string?> names)
                {
                    this.names = names;
                }

                public IEnumerable<string?> [||]GetNames()
                {
                    return this.names.Where(n => n is object);
                }
            }
            """,
            """
            #nullable enable

            using System.Linq;

            class C
            {
                private IEnumerable<string?> names;

                public IEnumerable<string?> Names { get => this.names.Where(n => n is object); set => this.names = value; }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37991")]
    public Task TestGetSetWithGeneric()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                private Task<string> someTask;

                public void SetSomeTask(Task<string> t)
                {
                    this.someTask = t;
                }

                public Task<string> [||]GetSomeTask()
                {
                    return this.someTask;
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                private Task<string> someTask;

                public Task<string> SomeTask { get => this.someTask; set => this.someTask = value; }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40758")]
    public Task TestReferenceTrivia1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                static bool [||]Value() => default;

                static void Main()
                {
                    if (/*test*/Value())
                    {
                    }
                }
            }
            """,
            """
            class Class
            {
                static bool Value => default;

                static void Main()
                {
                    if (/*test*/Value)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40758")]
    public Task TestReferenceTrivia2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                static bool [||]Value() => default;

                static void Main()
                {
                    if (Value()/*test*/)
                    {
                    }
                }
            }
            """,
            """
            class Class
            {
                static bool Value => default;

                static void Main()
                {
                    if (Value/*test*/)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40758")]
    public Task TestReferenceTrivia3()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                static bool [||]Value() => default;

                static void Main()
                {
                    var valueAsDelegate = /*test*/Value;
                }
            }
            """,
            """
            class Class
            {
                static bool Value => default;

                static void Main()
                {
                    var valueAsDelegate = /*test*/{|Conflict:Value|};
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40758")]
    public Task TestReferenceTrivia4()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                static bool [||]Value() => default;

                static void Main()
                {
                    var valueAsDelegate = Value/*test*/;
                }
            }
            """,
            """
            class Class
            {
                static bool Value => default;

                static void Main()
                {
                    var valueAsDelegate = {|Conflict:Value|}/*test*/;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72035")]
    public Task TestUpdateGetReferenceGeneratedPart()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>partial class C
            {
                int [||]GetGoo()
                {
                }
            }</Document>
                    <DocumentFromSourceGenerator>
            partial class C
            {
                void Bar()
                {
                    var x = GetGoo();
                }
            }
                    </DocumentFromSourceGenerator>
                </Project>
            </Workspace>
            """,
            """
            partial class C
            {
                int Goo
                {
                    get
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61161")]
    public Task TestEndOfLineTrivia1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Test1() { return 1; }
                public void Test2() { }
            }
            """,
            """
            class C
            {
                public int Test1 => 1;
                public void Test2() { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61161")]
    public Task TestEndOfLineTrivia2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Test1() { return 1; }

                public void Test2() { }
            }
            """,
            """
            class C
            {
                public int Test1 => 1;

                public void Test2() { }
            }
            """);
}
