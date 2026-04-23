// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.ReplacePropertyWithMethods;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ReplacePropertyWithMethods;

[Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
public sealed class ReplacePropertyWithMethodsTests : AbstractCSharpCodeActionTest_NoEditor
{
    private OptionsCollection PreferExpressionBodiedMethods
        => new(GetLanguage()) { { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement } };

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new ReplacePropertyWithMethodsCodeRefactoringProvider();

    [Fact]
    public Task TestGetWithBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return 0;
                    }
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return 0;
                }
            }
            """);

    [Fact]
    public Task TestPublicProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop
                {
                    get
                    {
                        return 0;
                    }
                }
            }
            """,
            """
            class C
            {
                public int GetProp()
                {
                    return 0;
                }
            }
            """);

    [Fact]
    public Task TestAnonyousType1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop
                {
                    get
                    {
                        return 0;
                    }
                }

                public void M()
                {
                    var v = new { P = this.Prop } }
            }
            """,
            """
            class C
            {
                public int GetProp()
                {
                    return 0;
                }

                public void M()
                {
                    var v = new { P = this.GetProp() } }
            }
            """);

    [Fact]
    public Task TestAnonyousType2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop
                {
                    get
                    {
                        return 0;
                    }
                }

                public void M()
                {
                    var v = new { this.Prop } }
            }
            """,
            """
            class C
            {
                public int GetProp()
                {
                    return 0;
                }

                public void M()
                {
                    var v = new { Prop = this.GetProp() } }
            }
            """);

    [Fact]
    public Task TestPassedToRef1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop
                {
                    get
                    {
                        return 0;
                    }
                }

                public void RefM(ref int i)
                {
                }

                public void M()
                {
                    RefM(ref this.Prop);
                }
            }
            """,
            """
            class C
            {
                public int GetProp()
                {
                    return 0;
                }

                public void RefM(ref int i)
                {
                }

                public void M()
                {
                    RefM(ref this.{|Conflict:GetProp|}());
                }
            }
            """);

    [Fact]
    public Task TestPassedToOut1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop
                {
                    get
                    {
                        return 0;
                    }
                }

                public void OutM(out int i)
                {
                }

                public void M()
                {
                    OutM(out this.Prop);
                }
            }
            """,
            """
            class C
            {
                public int GetProp()
                {
                    return 0;
                }

                public void OutM(out int i)
                {
                }

                public void M()
                {
                    OutM(out this.{|Conflict:GetProp|}());
                }
            }
            """);

    [Fact]
    public Task TestUsedInAttribute1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class CAttribute : Attribute
            {
                public int [||]Prop
                {
                    get
                    {
                        return 0;
                    }
                }
            }

            [C(Prop = 1)]
            class D
            {
            }
            """,
            """
            using System;

            class CAttribute : Attribute
            {
                public int GetProp()
                {
                    return 0;
                }
            }

            [C({|Conflict:Prop|} = 1)]
            class D
            {
            }
            """);

    [Fact]
    public Task TestSetWithBody1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    set
                    {
                        var v = value;
                    }
                }
            }
            """,
            """
            class C
            {
                private void SetProp(int value)
                {
                    var v = value;
                }
            }
            """);

    [Fact]
    public Task TestSetReference1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    set
                    {
                        var v = value;
                    }
                }

                void M()
                {
                    this.Prop = 1;
                }
            }
            """,
            """
            class C
            {
                private void SetProp(int value)
                {
                    var v = value;
                }

                void M()
                {
                    this.SetProp(1);
                }
            }
            """);

    [Fact]
    public Task TestGetterAndSetter()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return 0;
                    }

                    set
                    {
                        var v = value;
                    }
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return 0;
                }

                private void SetProp(int value)
                {
                    var v = value;
                }
            }
            """);

    [Fact]
    public Task TestGetterAndSetterAccessibilityChange()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop
                {
                    get
                    {
                        return 0;
                    }

                    private set
                    {
                        var v = value;
                    }
                }
            }
            """,
            """
            class C
            {
                public int GetProp()
                {
                    return 0;
                }

                private void SetProp(int value)
                {
                    var v = value;
                }
            }
            """);

    [Fact]
    public Task TestIncrement1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return 0;
                    }

                    set
                    {
                        var v = value;
                    }
                }

                void M()
                {
                    this.Prop++;
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return 0;
                }

                private void SetProp(int value)
                {
                    var v = value;
                }

                void M()
                {
                    this.SetProp(this.GetProp() + 1);
                }
            }
            """);

    [Fact]
    public Task TestDecrement2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return 0;
                    }

                    set
                    {
                        var v = value;
                    }
                }

                void M()
                {
                    this.Prop--;
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return 0;
                }

                private void SetProp(int value)
                {
                    var v = value;
                }

                void M()
                {
                    this.SetProp(this.GetProp() - 1);
                }
            }
            """);

    [Fact]
    public Task TestRecursiveGet()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return this.Prop + 1;
                    }
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return this.GetProp() + 1;
                }
            }
            """);

    [Fact]
    public Task TestRecursiveSet()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    set
                    {
                        this.Prop = value + 1;
                    }
                }
            }
            """,
            """
            class C
            {
                private void SetProp(int value)
                {
                    this.SetProp(value + 1);
                }
            }
            """);

    [Fact]
    public Task TestCompoundAssign1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return 0;
                    }

                    set
                    {
                        var v = value;
                    }
                }

                void M()
                {
                    this.Prop *= x;
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return 0;
                }

                private void SetProp(int value)
                {
                    var v = value;
                }

                void M()
                {
                    this.SetProp(this.GetProp() * x);
                }
            }
            """);

    [Fact]
    public Task TestCompoundAssign2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return 0;
                    }

                    set
                    {
                        var v = value;
                    }
                }

                void M()
                {
                    this.Prop *= x + y;
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return 0;
                }

                private void SetProp(int value)
                {
                    var v = value;
                }

                void M()
                {
                    this.SetProp(this.GetProp() * (x + y));
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41159")]
    public Task TestCompoundAssign3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                object [||]Prop
                {
                    get
                    {
                        return null;
                    }

                    set
                    {
                        var v = value;
                    }
                }

                void M()
                {
                    this.Prop ??= x;
                }
            }
            """,
            """
            class C
            {
                private object GetProp()
                {
                    return null;
                }

                private void SetProp(object value)
                {
                    var v = value;
                }

                void M()
                {
                    this.SetProp(this.GetProp() ?? x);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41159")]
    public Task TestCompoundAssign4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return 0;
                    }

                    set
                    {
                        var v = value;
                    }
                }

                void M()
                {
                    this.Prop >>= x;
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return 0;
                }

                private void SetProp(int value)
                {
                    var v = value;
                }

                void M()
                {
                    this.SetProp(this.GetProp() >> x);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41159")]
    public Task TestCompoundAssign5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return 0;
                    }

                    set
                    {
                        var v = value;
                    }
                }

                void M()
                {
                    this.Prop >>>= x;
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return 0;
                }

                private void SetProp(int value)
                {
                    var v = value;
                }

                void M()
                {
                    this.SetProp(this.GetProp() >>> x);
                }
            }
            """);

    [Fact]
    public Task TestMissingAccessors()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop { }

                void M()
                {
                    var v = this.Prop;
                }
            }
            """,
            """
            class C
            {

                void M()
                {
                    var v = this.GetProp();
                }
            }
            """);

    [Fact]
    public Task TestComputedProp()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop => 1;
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return 1;
                }
            }
            """);

    [Fact]
    public Task TestComputedPropWithTrailingTrivia()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop => 1; // Comment
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    return 1; // Comment
                }
            }
            """);

    [Fact]
    public Task TestIndentation()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Goo
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
            """,
            """
            class C
            {
                private int GetGoo()
                {
                    int count;
                    foreach (var x in y)
                    {
                        count += bar;
                    }
                    return count;
                }
            }
            """);

    [Fact]
    public Task TestComputedPropWithTrailingTriviaAfterArrow()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop => /* return 42 */ 42;
            }
            """,
            """
            class C
            {
                public int GetProp()
                {
                    /* return 42 */
                    return 42;
                }
            }
            """);

    [Fact]
    public Task TestAbstractProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public abstract int [||]Prop { get; }

                public void M()
                {
                    var v = new { P = this.Prop } }
            }
            """,
            """
            class C
            {
                public abstract int GetProp();

                public void M()
                {
                    var v = new { P = this.GetProp() } }
            }
            """);

    [Fact]
    public Task TestVirtualProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public virtual int [||]Prop
                {
                    get
                    {
                        return 1;
                    }
                }

                public void M()
                {
                    var v = new { P = this.Prop } }
            }
            """,
            """
            class C
            {
                public virtual int GetProp()
                {
                    return 1;
                }

                public void M()
                {
                    var v = new { P = this.GetProp() } }
            }
            """);

    [Fact]
    public Task TestInterfaceProperty()
        => TestInRegularAndScriptAsync(
            """
            interface I
            {
                int [||]Prop { get; }
            }
            """,
            """
            interface I
            {
                int GetProp();
            }
            """);

    [Fact]
    public Task TestAutoProperty1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop { get; }
            }
            """,
            """
            class C
            {
                private readonly int prop;

                public int GetProp()
                {
                    return prop;
                }
            }
            """);

    [Fact]
    public Task TestAutoProperty2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop { get; }

                public C()
                {
                    this.Prop++;
                }
            }
            """,
            """
            class C
            {
                private readonly int prop;

                public int GetProp()
                {
                    return prop;
                }

                public C()
                {
                    this.prop = this.GetProp() + 1;
                }
            }
            """);

    [Fact]
    public Task TestAutoProperty3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop { get; }

                public C()
                {
                    this.Prop *= x + y;
                }
            }
            """,
            """
            class C
            {
                private readonly int prop;

                public int GetProp()
                {
                    return prop;
                }

                public C()
                {
                    this.prop = this.GetProp() * (x + y);
                }
            }
            """);

    [Fact]
    public Task TestAutoProperty4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop { get; } = 1;
            }
            """,
            """
            class C
            {
                private readonly int prop = 1;

                public int GetProp()
                {
                    return prop;
                }
            }
            """);

    [Fact]
    public Task TestAutoProperty5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int prop;

                public int [||]Prop { get; } = 1;
            }
            """,
            """
            class C
            {
                private int prop;
                private readonly int prop1 = 1;

                public int GetProp()
                {
                    return prop1;
                }
            }
            """);

    [Fact]
    public Task TestAutoProperty6()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]PascalCase { get; }
            }
            """,
            """
            class C
            {
                private readonly int pascalCase;

                public int GetPascalCase()
                {
                    return pascalCase;
                }
            }
            """);

    [Fact]
    public Task TestUniqueName1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop
                {
                    get
                    {
                        return 0;
                    }
                }

                public abstract int GetProp();
            }
            """,
            """
            class C
            {
                public int GetProp1()
                {
                    return 0;
                }

                public abstract int GetProp();
            }
            """);

    [Fact]
    public Task TestUniqueName2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Prop
                {
                    set
                    {
                    }
                }

                public abstract void SetProp(int i);
            }
            """,
            """
            class C
            {
                public void SetProp1(int value)
                {
                }

                public abstract void SetProp(int i);
            }
            """);

    [Fact]
    public Task TestUniqueName3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public object [||]Prop
                {
                    set
                    {
                    }
                }

                public abstract void SetProp(dynamic i);
            }
            """,
            """
            class C
            {
                public void SetProp1(object value)
                {
                }

                public abstract void SetProp(dynamic i);
            }
            """);

    [Fact]
    public Task TestTrivia1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop { get; set; }

                void M()
                {

                    Prop++;
                }
            }
            """,
            """
            class C
            {
                private int prop;

                private int GetProp()
                {
                    return prop;
                }

                private void SetProp(int value)
                {
                    prop = value;
                }

                void M()
                {

                    SetProp(GetProp() + 1);
                }
            }
            """);

    [Fact]
    public Task TestTrivia2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop { get; set; }

                void M()
                {
                    /* Leading */
                    Prop++; /* Trailing */
                }
            }
            """,
            """
            class C
            {
                private int prop;

                private int GetProp()
                {
                    return prop;
                }

                private void SetProp(int value)
                {
                    prop = value;
                }

                void M()
                {
                    /* Leading */
                    SetProp(GetProp() + 1); /* Trailing */
                }
            }
            """);

    [Fact]
    public Task TestTrivia3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop { get; set; }

                void M()
                {
                    /* Leading */
                    Prop += 1 /* Trailing */ ;
                }
            }
            """,
            """
            class C
            {
                private int prop;

                private int GetProp()
                {
                    return prop;
                }

                private void SetProp(int value)
                {
                    prop = value;
                }

                void M()
                {
                    /* Leading */
                    SetProp(GetProp() + 1 /* Trailing */ );
                }
            }
            """);

    [Fact]
    public Task ReplaceReadInsideWrite1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop { get; set; }

                void M()
                {
                    Prop = Prop + 1;
                }
            }
            """,
            """
            class C
            {
                private int prop;

                private int GetProp()
                {
                    return prop;
                }

                private void SetProp(int value)
                {
                    prop = value;
                }

                void M()
                {
                    SetProp(GetProp() + 1);
                }
            }
            """);

    [Fact]
    public Task ReplaceReadInsideWrite2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop { get; set; }

                void M()
                {
                    Prop *= Prop + 1;
                }
            }
            """,
            """
            class C
            {
                private int prop;

                private int GetProp()
                {
                    return prop;
                }

                private void SetProp(int value)
                {
                    prop = value;
                }

                void M()
                {
                    SetProp(GetProp() * (GetProp() + 1));
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16157")]
    public Task TestWithConditionalBinding1()
        => TestInRegularAndScriptAsync(
            """
            public class Goo
            {
                public bool [||]Any { get; } // Replace 'Any' with method

                public static void Bar()
                {
                    var goo = new Goo();
                    bool f = goo?.Any == true;
                }
            }
            """,
            """
            public class Goo
            {
                private readonly bool any;

                public bool GetAny()
                {
                    return any;
                }

                public static void Bar()
                {
                    var goo = new Goo();
                    bool f = goo?.GetAny() == true;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return 0;
                    }
                }
            }
            """,
            """
            class C
            {
                private int GetProp() => 0;
            }
            """, new(options: PreferExpressionBodiedMethods));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        return 0;
                    }

                    set
                    {
                        throw e;
                    }
                }
            }
            """,
            """
            class C
            {
                private int GetProp() => 0;
                private void SetProp(int value) => throw e;
            }
            """, new(options: PreferExpressionBodiedMethods));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get => 0;

                    set => throw e;
                }
            }
            """,
            """
            class C
            {
                private int GetProp() => 0;
                private void SetProp(int value) => throw e;
            }
            """, new(options: PreferExpressionBodiedMethods));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop => 0;
            }
            """,
            """
            class C
            {
                private int GetProp() => 0;
            }
            """, new(options: PreferExpressionBodiedMethods));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop { get; }
            }
            """,
            """
            class C
            {
                private readonly int prop;

                private int GetProp() => prop;
            }
            """, new(options: PreferExpressionBodiedMethods));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle6()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop { get; set; }
            }
            """,
            """
            class C
            {
                private int prop;

                private int GetProp() => prop;
                private void SetProp(int value) => prop = value;
            }
            """, new(options: PreferExpressionBodiedMethods));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16980")]
    public Task TestCodeStyle7()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
                        A();
                        return B();
                    }
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
                    A();
                    return B();
                }
            }
            """, new(options: PreferExpressionBodiedMethods));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18234")]
    public Task TestDocumentationComment1()
        => TestInRegularAndScriptAsync(
            """
            internal interface ILanguageServiceHost
            {
                /// <summary>
                ///     Gets the active workspace project context that provides access to the language service for the active configured project.
                /// </summary>
                /// <value>
                ///     An value that provides access to the language service for the active configured project.
                /// </value>
                object [||]ActiveProjectContext
                {
                    get;
                }
            }
            """,
            """
            internal interface ILanguageServiceHost
            {
                /// <summary>
                ///     Gets the active workspace project context that provides access to the language service for the active configured project.
                /// </summary>
                /// <returns>
                ///     An value that provides access to the language service for the active configured project.
                /// </returns>
                object GetActiveProjectContext();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18234")]
    public Task TestDocumentationComment2()
        => TestInRegularAndScriptAsync(
            """
            internal interface ILanguageServiceHost
            {
                /// <summary>
                ///     Sets the active workspace project context that provides access to the language service for the active configured project.
                /// </summary>
                /// <value>
                ///     An value that provides access to the language service for the active configured project.
                /// </value>
                object [||]ActiveProjectContext
                {
                    set;
                }
            }
            """,
            """
            internal interface ILanguageServiceHost
            {
                /// <summary>
                ///     Sets the active workspace project context that provides access to the language service for the active configured project.
                /// </summary>
                /// <param name="value">
                ///     An value that provides access to the language service for the active configured project.
                /// </param>
                void SetActiveProjectContext(object value);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18234")]
    public Task TestDocumentationComment3()
        => TestInRegularAndScriptAsync(
            """
            internal interface ILanguageServiceHost
            {
                /// <summary>
                ///     Gets or sets the active workspace project context that provides access to the language service for the active configured project.
                /// </summary>
                /// <value>
                ///     An value that provides access to the language service for the active configured project.
                /// </value>
                object [||]ActiveProjectContext
                {
                    get; set;
                }
            }
            """,
            """
            internal interface ILanguageServiceHost
            {
                /// <summary>
                ///     Gets or sets the active workspace project context that provides access to the language service for the active configured project.
                /// </summary>
                /// <returns>
                ///     An value that provides access to the language service for the active configured project.
                /// </returns>
                object GetActiveProjectContext();
                void SetActiveProjectContext(object value);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18234")]
    public Task TestDocumentationComment4()
        => TestInRegularAndScriptAsync(
            """
            internal interface ILanguageServiceHost
            {
                /// <summary>
                ///     Sets <see cref="ActiveProjectContext"/>.
                /// </summary>
                /// <seealso cref="ActiveProjectContext"/>
                object [||]ActiveProjectContext
                {
                    set;
                }
            }
            internal struct AStruct
            {
                /// <seealso cref="ILanguageServiceHost.ActiveProjectContext"/>
                private int x;
            }
            """,
            """
            internal interface ILanguageServiceHost
            {
                /// <summary>
                ///     Sets <see cref="SetActiveProjectContext(object)"/>.
                /// </summary>
                /// <seealso cref="SetActiveProjectContext(object)"/>
                void SetActiveProjectContext(object value);
            }
            internal struct AStruct
            {
                /// <seealso cref="ILanguageServiceHost.SetActiveProjectContext(object)"/>
                private int x;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18234")]
    public Task TestDocumentationComment5()
        => TestInRegularAndScriptAsync(
            """
            internal interface ILanguageServiceHost
            {
                /// <summary>
                ///     Gets or sets <see cref="ActiveProjectContext"/>.
                /// </summary>
                /// <seealso cref="ActiveProjectContext"/>
                object [||]ActiveProjectContext
                {
                    get; set;
                }
            }
            internal struct AStruct
            {
                /// <seealso cref="ILanguageServiceHost.ActiveProjectContext"/>
                private int x;
            }
            """,
            """
            internal interface ILanguageServiceHost
            {
                /// <summary>
                ///     Gets or sets <see cref="GetActiveProjectContext()"/>.
                /// </summary>
                /// <seealso cref="GetActiveProjectContext()"/>
                object GetActiveProjectContext();
                void SetActiveProjectContext(object value);
            }
            internal struct AStruct
            {
                /// <seealso cref="ILanguageServiceHost.GetActiveProjectContext()"/>
                private int x;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18234")]
    public Task TestDocumentationComment6()
        => TestInRegularAndScriptAsync(
            """
            internal interface ISomeInterface<T>
            {
                /// <seealso cref="Context"/>
                ISomeInterface<T> [||]Context
                {
                    set;
                }
            }
            internal struct AStruct
            {
                /// <seealso cref="ISomeInterface{T}.Context"/>
                private int x;
            }
            """,
            """
            internal interface ISomeInterface<T>
            {
                /// <seealso cref="SetContext(ISomeInterface{T})"/>
                void SetContext(ISomeInterface<T> value);
            }
            internal struct AStruct
            {
                /// <seealso cref="ISomeInterface{T}.SetContext(ISomeInterface{T})"/>
                private int x;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19235")]
    public Task TestWithDirectives1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
            #if true
                        return 0;
            #else
                        return 1;
            #endif
                    }
                }
            }
            """,
            """
            class C
            {
                private int GetProp()
                {
            #if true
                    return 0;
            #else
                        return 1;
            #endif
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19235")]
    public Task TestWithDirectives2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop
                {
                    get
                    {
            #if true
                        return 0;
            #else
                        return 1;
            #endif
                    }
                }
            }
            """,
            """
            class C
            {
                private int GetProp() =>
            #if true
                        0;
            #else
                        return 1;
            #endif
            }
            """,
            new(options: PreferExpressionBodiedMethods));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19235")]
    public Task TestWithDirectives3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop =>
            #if true
                    0;
            #else
                    1;
            #endif
            }
            """,
            """
            class C
            {
                private int GetProp() =>
            #if true
                    0;
            #else
                    1;
            #endif
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19235")]
    public Task TestWithDirectives4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int [||]Prop =>
            #if true
                    0;
            #else
                    1;
            #endif
            }
            """,
            """
            class C
            {
                private int GetProp() =>
            #if true
                    0;
            #else
                    1;
            #endif
            }
            """,
            new(options: PreferExpressionBodiedMethods));

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/440371")]
    public Task TestExplicitInterfaceImplementation()
        => TestInRegularAndScriptAsync(
            """
            interface IGoo
            {
                int [||]Goo { get; set; }
            }

            class C : IGoo
            {
                int IGoo.Goo
                {
                    get
                    {
                        throw new System.NotImplementedException();
                    }

                    set
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }
            """,
            """
            interface IGoo
            {
                int GetGoo();
                void SetGoo(int value);
            }

            class C : IGoo
            {
                int IGoo.GetGoo()
                {
                    throw new System.NotImplementedException();
                }

                void IGoo.SetGoo(int value)
                {
                    throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38379")]
    public Task TestUnsafeExpressionBody()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public unsafe void* [||]Pointer => default;
            }
            """,
            """
            class C
            {
                public unsafe void* GetPointer()
                {
                    return default;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38379")]
    public Task TestUnsafeAutoProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public unsafe void* [||]Pointer { get; set; }
            }
            """,
            """
            class C
            {
                private unsafe void* pointer;

                public unsafe void* GetPointer()
                {
                    return pointer;
                }

                public unsafe void SetPointer(void* value)
                {
                    pointer = value;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38379")]
    public Task TestUnsafeSafeType()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public unsafe int [||]P
                {
                    get => 0;
                    set {}
                }
            }
            """,
            """
            class C
            {
                public unsafe int GetP()
                {
                    return 0;
                }

                public unsafe void SetP(int value)
                { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22760")]
    public Task QualifyFieldAccessWhenNecessary1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Value { get; }

                public C(int value)
                {
                    Value = value;
                }
            }
            """,
            """
            class C
            {
                private readonly int value;

                public int GetValue()
                {
                    return value;
                }

                public C(int value)
                {
                    this.value = value;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22760")]
    public Task QualifyFieldAccessWhenNecessary2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public int [||]Value { get; }

                public C(int value)
                {
                    this.Value = value;
                }
            }
            """,
            """
            class C
            {
                private readonly int value;

                public int GetValue()
                {
                    return value;
                }

                public C(int value)
                {
                    this.value = value;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22760")]
    public Task QualifyFieldAccessWhenNecessary3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static int [||]Value { get; }

                public static void Set(int value)
                {
                    Value = value;
                }
            }
            """,
            """
            class C
            {
                private static readonly int value;

                public static int GetValue()
                {
                    return value;
                }

                public static void Set(int value)
                {
                    C.value = value;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45171")]
    public Task TestReferenceInObjectInitializer()
        => TestInRegularAndScriptAsync(
            """
            public class Tweet
            {
                public string [||]Tweet { get; }
            }

            class C
            {
                void Main()
                {
                    var t = new Tweet();
                    var t1 = new Tweet
                    {
                        Tweet = t.Tweet
                    };
                }
            }
            """,
            """
            public class Tweet
            {
                private readonly string tweet;

                public string GetTweet()
                {
                    return tweet;
                }
            }

            class C
            {
                void Main()
                {
                    var t = new Tweet();
                    var t1 = new Tweet
                    {
                        {|Conflict:Tweet|} = t.GetTweet()
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45171")]
    public Task TestReferenceInImplicitObjectInitializer()
        => TestInRegularAndScriptAsync(
            """
            public class Tweet
            {
                public string [||]Tweet { get; }
            }

            class C
            {
                void Main()
                {
                    var t = new Tweet();
                    Tweet t1 = new()
                    {
                        Tweet = t.Tweet
                    };
                }
            }
            """,
            """
            public class Tweet
            {
                private readonly string tweet;

                public string GetTweet()
                {
                    return tweet;
                }
            }

            class C
            {
                void Main()
                {
                    var t = new Tweet();
                    Tweet t1 = new()
                    {
                        {|Conflict:Tweet|} = t.GetTweet()
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45171")]
    public Task TestReferenceInWithInitializer()
        => TestInRegularAndScriptAsync(
            """
            public class Tweet
            {
                public string [||]Tweet { get; }
            }

            class C
            {
                void Main()
                {
                    var t = new Tweet();
                    var t1 = t with
                    {
                        Tweet = t.Tweet
                    };
                }
            }
            """,
            """
            public class Tweet
            {
                private readonly string tweet;

                public string GetTweet()
                {
                    return tweet;
                }
            }

            class C
            {
                void Main()
                {
                    var t = new Tweet();
                    var t1 = t with
                    {
                        {|Conflict:Tweet|} = t.GetTweet()
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57376")]
    public Task TestInLinkedFile()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.1'>
                    <Document FilePath='C.cs'>
            class C
            {
                int [||]Prop
                {
                    set
                    {
                        var v = value;
                    }
                }

                void M()
                {
                    this.Prop = 1;
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
                private void SetProp(int value)
                {
                    var v = value;
                }

                void M()
                {
                    this.SetProp(1);
                }
            }
                    </Document>
                </Project>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.2'>
                    <Document IsLinkFile='true' LinkProjectName='CSProj.1' LinkFilePath='C.cs'/>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25367")]
    public Task TestAccessorAttributes1()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Runtime.CompilerServices;

            class Program
            {
                static void Main() { }

                private static int [||]SomeValue
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => 42;
                }
            }
            """,
            """
            using System;
            using System.Runtime.CompilerServices;

            class Program
            {
                static void Main() { }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static int GetSomeValue()
                {
                    return 42;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25367")]
    public Task TestAccessorAttributes2()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Runtime.CompilerServices;

            class Program
            {
                static void Main() { }

                private static int [||]SomeValue
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => 42;

                    [OtherAttribute]
                    set { }
                }
            }
            """,
            """
            using System;
            using System.Runtime.CompilerServices;

            class Program
            {
                static void Main() { }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static int GetSomeValue()
                {
                    return 42;
                }

                [OtherAttribute]
                private static void SetSomeValue(int value)
                { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75135")]
    public Task TestMatchInPropertyPattern()
        => TestInRegularAndScriptAsync("""
            class C
            {
                public int [||]Property { get { return 0; } }
                public bool M()
                {
                    return this is { Property: 1 };
                }
            }
            """, """
            class C
            {
                public int GetProperty()
                { return 0; }
                public bool M()
                {
                    return this is { {|Conflict:Property|}: 1 };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75999")]
    public Task TestInterfacePropertyWithImplementation()
        => TestInRegularAndScriptAsync(
            """
            interface I
            {
                public virtual string [||]Name
                {
                    get
                    {
                        return string.Empty;
                    }
                }
            }
            """,
            """
            interface I
            {
                public virtual string GetName()
                {
                    return string.Empty;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75186")]
    public Task DoNotDuplicatePreprocessorDirective1()
        => TestInRegularAndScriptAsync("""
            class A
            {
                #region
                static bool a;
                #endregion
                static bool [||]B => true;
            }
            """, """
            class A
            {
                #region
                static bool a;
                #endregion
                private static bool GetB()
                {
                    return true;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75186")]
    public Task DoNotDuplicatePreprocessorDirective2()
        => TestInRegularAndScriptAsync("""
            class A
            {
                #region
                static bool a;
                #endregion
                static bool [||]B { get { return 0; } set { } }
            }
            """, """
            class A
            {
                #region
                static bool a;
                #endregion
                private static bool GetB()
                { return 0; }

                private static void SetB(bool value)
                { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75186")]
    public Task DoNotDuplicatePreprocessorDirective3()
        => TestInRegularAndScriptAsync("""
            class A
            {
                #region
                static bool a;
                #endregion
                static bool [||]B { get; }
            }
            """, """
            class A
            {
                #region
                static bool a;
                private static readonly bool b;
                #endregion
                private static bool GetB()
                {
                    return b;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75186")]
    public Task DoNotDuplicatePreprocessorDirective4()
        => TestInRegularAndScriptAsync("""
            class A
            {
                #region
                static bool a;
                #endregion
                static bool [||]B { get; set; }
            }
            """, """
            class A
            {
                #region
                static bool a;
                private static bool b;
                #endregion
                private static bool GetB()
                {
                    return b;
                }

                private static void SetB(bool value)
                {
                    b = value;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/XXXXX")]
    public Task TestPartialPropertyWithImplementation()
        => TestInRegularAndScriptAsync("""
            partial class C
            {
                public partial int [||]P { get; }
            }

            partial class C
            {
                public partial int P => 0;
            }
            """, """
            partial class C
            {
                public partial int GetP();
            }

            partial class C
            {
                public partial int GetP()
                {
                    return 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/XXXXX")]
    public Task TestPartialPropertyWithBody()
        => TestInRegularAndScriptAsync("""
            partial class C
            {
                public partial int [||]P { get; set; }
            }

            partial class C
            {
                public partial int P
                {
                    get => 42;
                    set { }
                }
            }
            """, """
            partial class C
            {
                public partial int GetP();
                public partial void SetP(int value);
            }

            partial class C
            {
                public partial int GetP()
                {
                    return 42;
                }

                public partial void SetP(int value)
                { }
            }
            """);
}
