// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.GenerateVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateVariable;

[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
public sealed class GenerateVariableTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    private const int FieldIndex = 0;
    private const int ReadonlyFieldIndex = 1;
    private const int PropertyIndex = 2;
    private const int LocalIndex = 3;
    private const int Parameter = 4;
    private const int ParameterAndOverrides = 5;

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpGenerateVariableCodeFixProvider());

    private readonly CodeStyleOption2<bool> onWithInfo = new(true, NotificationOption2.Suggestion);

    // specify all options explicitly to override defaults.
    private OptionsCollection ImplicitTypingEverywhere()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
        };

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    [Fact]
    public Task TestSimpleLowercaseIdentifier1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|goo|];
                }
            }
            """,
            """
            class Class
            {
                private object goo;

                void Method()
                {
                    goo;
                }
            }
            """);

    [Fact]
    public Task TestSimpleLowercaseIdentifierAllOptionsOffered()
        => TestExactActionSetOfferedAsync(
            """
            class Class
            {
                void Method()
                {
                    [|goo|];
                }
            }
            """,
            [
                string.Format(CodeFixesResources.Generate_field_0, "goo"),
                string.Format(CodeFixesResources.Generate_read_only_field_0, "goo"),
                string.Format(CodeFixesResources.Generate_property_0, "goo"),
                string.Format(CodeFixesResources.Generate_local_0, "goo"),
                string.Format(CodeFixesResources.Generate_parameter_0, "goo"),
            ]);

    [Fact]
    public Task TestUnderscorePrefixAllOptionsOffered()
        => TestExactActionSetOfferedAsync(
            """
            class Class
            {
                void Method()
                {
                    [|_goo|];
                }
            }
            """,
            [
                string.Format(CodeFixesResources.Generate_field_0, "_goo"),
                string.Format(CodeFixesResources.Generate_read_only_field_0, "_goo"),
            ]);

    [Fact]
    public Task TestSimpleLowercaseIdentifier2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|goo|];
                }
            }
            """,
            """
            class Class
            {
                private readonly object goo;

                void Method()
                {
                    goo;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestTestSimpleLowercaseIdentifier3()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|goo|];
                }
            }
            """,
            """
            class Class
            {
                public object goo { get; private set; }

                void Method()
                {
                    goo;
                }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestSimpleUppercaseIdentifier1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|Goo|];
                }
            }
            """,
            """
            class Class
            {
                public object Goo { get; private set; }

                void Method()
                {
                    Goo;
                }
            }
            """);

    [Fact]
    public Task TestSimpleUppercaseIdentifier2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|Goo|];
                }
            }
            """,
            """
            class Class
            {
                private object Goo;

                void Method()
                {
                    Goo;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestSimpleUppercaseIdentifier3()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|Goo|];
                }
            }
            """,
            """
            class Class
            {
                private readonly object Goo;

                void Method()
                {
                    Goo;
                }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestSimpleRead1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(int i)
                {
                    Method([|goo|]);
                }
            }
            """,
            """
            class Class
            {
                private int goo;

                void Method(int i)
                {
                    Method(goo);
                }
            }
            """);

    [Fact]
    public Task TestSimpleReadWithTopLevelNullability()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class Class
            {
                void Method(string? s)
                {
                    Method([|goo|]);
                }
            }
            """,
            """
            #nullable enable

            class Class
            {
                private string? goo;

                void Method(string? s)
                {
                    Method(goo);
                }
            }
            """);

    [Fact]
    public Task TestSimpleReadWithNestedNullability()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            using System.Collections.Generic;

            class Class
            {
                void Method(IEnumerable<string?> s)
                {
                    Method([|goo|]);
                }
            }
            """,
            """
            #nullable enable

            using System.Collections.Generic;

            class Class
            {
                private IEnumerable<string?> goo;

                void Method(IEnumerable<string?> s)
                {
                    Method(goo);
                }
            }
            """);

    [Fact]
    public Task TestSimpleWriteCount()
        => TestExactActionSetOfferedAsync(
            """
            class Class
            {
                void Method(int i)
                {
                    [|goo|] = 1;
                }
            }
            """,
            [string.Format(CodeFixesResources.Generate_field_0, "goo"), string.Format(CodeFixesResources.Generate_property_0, "goo"), string.Format(CodeFixesResources.Generate_local_0, "goo"), string.Format(CodeFixesResources.Generate_parameter_0, "goo")]);

    [Fact]
    public Task TestSimpleWriteInOverrideCount()
        => TestExactActionSetOfferedAsync(
            """
            abstract class Base
            {
                public abstract void Method(int i);
            }

            class Class : Base
            {
                public override void Method(int i)
                {
                    [|goo|] = 1;
                }
            }
            """,
            [string.Format(CodeFixesResources.Generate_field_0, "goo"), string.Format(CodeFixesResources.Generate_property_0, "goo"), string.Format(CodeFixesResources.Generate_local_0, "goo"), string.Format(CodeFixesResources.Generate_parameter_0, "goo"), string.Format(CodeFixesResources.Generate_parameter_0_and_overrides_implementations, "goo")]);

    [Fact]
    public Task TestSimpleWrite1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(int i)
                {
                    [|goo|] = 1;
                }
            }
            """,
            """
            class Class
            {
                private int goo;

                void Method(int i)
                {
                    goo = 1;
                }
            }
            """);

    [Fact]
    public Task TestSimpleWrite2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(int i)
                {
                    [|goo|] = 1;
                }
            }
            """,
            """
            class Class
            {
                public int goo { get; private set; }

                void Method(int i)
                {
                    goo = 1;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateFieldInRef()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(ref int i)
                {
                    Method(ref this.[|goo|]);
                }
            }
            """,
            """
            class Class
            {
                private int goo;

                void Method(ref int i)
                {
                    Method(ref this.[|goo|]);
                }
            }
            """);

    [Fact]
    public Task TestGeneratePropertyInRef()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Class
            {
                void Method(ref int i)
                {
                    Method(ref this.[|goo|]);
                }
            }
            """,
            """
            using System;
            class Class
            {
                public ref int goo => throw new NotImplementedException();

                void Method(ref int i)
                {
                    Method(ref this.goo);
                }
            }
            """, index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGeneratePropertyInIn()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Class
            {
                void Method(in int i)
                {
                    Method(in this.[|goo|]);
                }
            }
            """,
            """
            using System;
            class Class
            {
                public ref readonly int goo => throw new NotImplementedException();

                void Method(in int i)
                {
                    Method(in this.goo);
                }
            }
            """, index: PropertyIndex);

    [Fact]
    public Task TestInRef1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(ref int i)
                {
                    Method(ref [|goo|]);
                }
            }
            """,
            """
            class Class
            {
                private int goo;

                void Method(ref int i)
                {
                    Method(ref goo);
                }
            }
            """);

    [Fact]
    public Task TestInOutCodeActionCount()
        => TestExactActionSetOfferedAsync(
            """
            class Class
            {
                void Method(out int i)
                {
                    Method(out [|goo|]);
                }
            }
            """,
            [string.Format(CodeFixesResources.Generate_field_0, "goo"), string.Format(CodeFixesResources.Generate_local_0, "goo"), string.Format(CodeFixesResources.Generate_parameter_0, "goo")]);

    [Fact]
    public Task TestInOut1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(out int i)
                {
                    Method(out [|goo|]);
                }
            }
            """,
            """
            class Class
            {
                private int goo;

                void Method(out int i)
                {
                    Method(out goo);
                }
            }
            """);

    [Fact]
    public Task TestGenerateInStaticMember1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                static void Method()
                {
                    [|goo|];
                }
            }
            """,
            """
            class Class
            {
                private static object goo;

                static void Method()
                {
                    goo;
                }
            }
            """);

    [Fact]
    public Task TestGenerateInStaticMember2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                static void Method()
                {
                    [|goo|];
                }
            }
            """,
            """
            class Class
            {
                private static readonly object goo;

                static void Method()
                {
                    goo;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateInStaticMember3()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                static void Method()
                {
                    [|goo|];
                }
            }
            """,
            """
            class Class
            {
                public static object goo { get; private set; }

                static void Method()
                {
                    goo;
                }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGenerateOffInstance1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    this.[|goo|];
                }
            }
            """,
            """
            class Class
            {
                private object goo;

                void Method()
                {
                    this.goo;
                }
            }
            """);

    [Fact]
    public Task TestGenerateOffInstance2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    this.[|goo|];
                }
            }
            """,
            """
            class Class
            {
                private readonly object goo;

                void Method()
                {
                    this.goo;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateOffInstance3()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    this.[|goo|];
                }
            }
            """,
            """
            class Class
            {
                public object goo { get; private set; }

                void Method()
                {
                    this.goo;
                }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGenerateOffWrittenInstance1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    this.[|goo|] = 1;
                }
            }
            """,
            """
            class Class
            {
                private int goo;

                void Method()
                {
                    this.goo = 1;
                }
            }
            """);

    [Fact]
    public Task TestGenerateOffWrittenInstance2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    this.[|goo|] = 1;
                }
            }
            """,
            """
            class Class
            {
                public int goo { get; private set; }

                void Method()
                {
                    this.goo = 1;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateOffStatic1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    Class.[|goo|];
                }
            }
            """,
            """
            class Class
            {
                private static object goo;

                void Method()
                {
                    Class.goo;
                }
            }
            """);

    [Fact]
    public Task TestGenerateOffStatic2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    Class.[|goo|];
                }
            }
            """,
            """
            class Class
            {
                private static readonly object goo;

                void Method()
                {
                    Class.goo;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateOffStatic3()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    Class.[|goo|];
                }
            }
            """,
            """
            class Class
            {
                public static object goo { get; private set; }

                void Method()
                {
                    Class.goo;
                }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGenerateOffWrittenStatic1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    Class.[|goo|] = 1;
                }
            }
            """,
            """
            class Class
            {
                private static int goo;

                void Method()
                {
                    Class.goo = 1;
                }
            }
            """);

    [Fact]
    public Task TestGenerateOffWrittenStatic2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    Class.[|goo|] = 1;
                }
            }
            """,
            """
            class Class
            {
                public static int goo { get; private set; }

                void Method()
                {
                    Class.goo = 1;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateInstanceIntoSibling1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    new D().[|goo|];
                }
            }

            class D
            {
            }
            """,
            """
            class Class
            {
                void Method()
                {
                    new D().goo;
                }
            }

            class D
            {
                internal object goo;
            }
            """);

    [Fact]
    public Task TestGenerateInstanceIntoOuter1()
        => TestInRegularAndScriptAsync(
            """
            class Outer
            {
                class Class
                {
                    void Method()
                    {
                        new Outer().[|goo|];
                    }
                }
            }
            """,
            """
            class Outer
            {
                private object goo;

                class Class
                {
                    void Method()
                    {
                        new Outer().goo;
                    }
                }
            }
            """);

    [Fact]
    public Task TestGenerateInstanceIntoDerived1()
        => TestInRegularAndScriptAsync(
            """
            class Class : Base
            {
                void Method(Base b)
                {
                    b.[|goo|];
                }
            }

            class Base
            {
            }
            """,
            """
            class Class : Base
            {
                void Method(Base b)
                {
                    b.goo;
                }
            }

            class Base
            {
                internal object goo;
            }
            """);

    [Fact]
    public Task TestGenerateStaticIntoDerived1()
        => TestInRegularAndScriptAsync(
            """
            class Class : Base
            {
                void Method(Base b)
                {
                    Base.[|goo|];
                }
            }

            class Base
            {
            }
            """,
            """
            class Class : Base
            {
                void Method(Base b)
                {
                    Base.goo;
                }
            }

            class Base
            {
                protected static object goo;
            }
            """);

    [Fact]
    public Task TestGenerateIntoInterfaceFixCount()
        => TestActionCountAsync(
            """
            class Class
            {
                void Method(I i)
                {
                    i.[|goo|];
                }
            }

            interface I
            {
            }
            """,
            count: 2);

    [Fact]
    public Task TestGenerateIntoInterface1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(I i)
                {
                    i.[|Goo|];
                }
            }

            interface I
            {
            }
            """,
            """
            class Class
            {
                void Method(I i)
                {
                    i.Goo;
                }
            }

            interface I
            {
                object Goo { get; set; }
            }
            """, index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateIntoInterface2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(I i)
                {
                    i.[|Goo|];
                }
            }

            interface I
            {
            }
            """,
            """
            class Class
            {
                void Method(I i)
                {
                    i.Goo;
                }
            }

            interface I
            {
                object Goo { get; }
            }
            """);

    [Fact]
    public Task TestGenerateStaticIntoInterfaceMissing()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(I i)
                {
                    I.[|Goo|];
                }
            }

            interface I
            {
            }
            """);

    [Fact]
    public Task TestGenerateWriteIntoInterfaceFixCount()
        => TestActionCountAsync(
            """
            class Class
            {
                void Method(I i)
                {
                    i.[|Goo|] = 1;
                }
            }

            interface I
            {
            }
            """,
            count: 1);

    [Fact]
    public Task TestGenerateWriteIntoInterface1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(I i)
                {
                    i.[|Goo|] = 1;
                }
            }

            interface I
            {
            }
            """,
            """
            class Class
            {
                void Method(I i)
                {
                    i.Goo = 1;
                }
            }

            interface I
            {
                int Goo { get; set; }
            }
            """);

    [Fact]
    public Task TestGenerateInGenericType()
        => TestInRegularAndScriptAsync(
            """
            class Class<T>
            {
                void Method(T t)
                {
                    [|goo|] = t;
                }
            }
            """,
            """
            class Class<T>
            {
                private T goo;

                void Method(T t)
                {
                    goo = t;
                }
            }
            """);

    [Fact]
    public Task TestGenerateInGenericMethod1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method<T>(T t)
                {
                    [|goo|] = t;
                }
            }
            """,
            """
            class Class
            {
                private object goo;

                void Method<T>(T t)
                {
                    goo = t;
                }
            }
            """);

    [Fact]
    public Task TestGenerateInGenericMethod2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method<T>(IList<T> t)
                {
                    [|goo|] = t;
                }
            }
            """,
            """
            class Class
            {
                private IList<object> goo;

                void Method<T>(IList<T> t)
                {
                    goo = t;
                }
            }
            """);

    [Fact]
    public Task TestGenerateFieldBeforeFirstField()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                int i;

                void Method()
                {
                    [|goo|];
                }
            }
            """,
            """
            class Class
            {
                int i;
                private object goo;

                void Method()
                {
                    goo;
                }
            }
            """);

    [Fact]
    public Task TestGenerateFieldAfterLastField()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|goo|];
                }

                int i;
            }
            """,
            """
            class Class
            {
                void Method()
                {
                    goo;
                }

                int i;
                private object goo;
            }
            """);

    [Fact]
    public Task TestGeneratePropertyAfterLastField1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                int Bar;

                void Method()
                {
                    [|Goo|];
                }
            }
            """,
            """
            class Class
            {
                int Bar;

                public object Goo { get; private set; }

                void Method()
                {
                    Goo;
                }
            }
            """);

    [Fact]
    public Task TestGeneratePropertyAfterLastField2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|Goo|];
                }

                int Bar;
            }
            """,
            """
            class Class
            {
                void Method()
                {
                    Goo;
                }

                int Bar;

                public object Goo { get; private set; }
            }
            """);

    [Fact]
    public Task TestGeneratePropertyBeforeFirstProperty()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                int Quux { get; }

                void Method()
                {
                    [|Goo|];
                }
            }
            """,
            """
            class Class
            {
                public object Goo { get; private set; }
                int Quux { get; }

                void Method()
                {
                    Goo;
                }
            }
            """);

    [Fact]
    public Task TestGeneratePropertyBeforeFirstPropertyEvenWithField1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                int Bar;

                int Quux { get; }

                void Method()
                {
                    [|Goo|];
                }
            }
            """,
            """
            class Class
            {
                int Bar;

                public object Goo { get; private set; }
                int Quux { get; }

                void Method()
                {
                    Goo;
                }
            }
            """);

    [Fact]
    public Task TestGeneratePropertyAfterLastPropertyEvenWithField2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                int Quux { get; }

                int Bar;

                void Method()
                {
                    [|Goo|];
                }
            }
            """,
            """
            class Class
            {
                int Quux { get; }
                public object Goo { get; private set; }

                int Bar;

                void Method()
                {
                    Goo;
                }
            }
            """);

    [Fact]
    public Task TestMissingInInvocation()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|Goo|]();
                }
            }
            """);

    [Fact]
    public Task TestMissingInObjectCreation()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    new [|Goo|]();
                }
            }
            """);

    [Fact]
    public async Task TestMissingInTypeDeclaration()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|A|] a;
                }
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|A.B|] a;
                }
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|A|].B a;
                }
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    A.[|B|] a;
                }
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|A.B.C|] a;
                }
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|A.B|].C a;
                }
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    A.B.[|C|] a;
                }
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|A|].B.C a;
                }
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    A.[|B|].C a;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539336")]
    public async Task TestMissingInAttribute()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            [[|A|]]
            class Class
            {
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            [[|A.B|]]
            class Class
            {
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            [[|A|].B]
            class Class
            {
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            [A.[|B|]]
            class Class
            {
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            [[|A.B.C|]]
            class Class
            {
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            [[|A.B|].C]
            class Class
            {
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            [A.B.[|C|]]
            class Class
            {
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            [[|A|].B.C]
            class Class
            {
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            [A.B.[|C|]]
            class Class
            {
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539340")]
    public async Task TestSpansField()
    {
        await TestSpansAsync(
            """
            class C
            {
                void M()
                {
                    this.[|Goo|] }
            """);

        await TestSpansAsync(
            """
            class C
            {
                void M()
                {
                    this.[|Goo|];
                }
            """);

        await TestSpansAsync(
            """
            class C
            {
                void M()
                {
                    this.[|Goo|] = 1 }
            """);

        await TestSpansAsync(
            """
            class C
            {
                void M()
                {
                    this.[|Goo|] = 1 + 2 }
            """);

        await TestSpansAsync(
            """
            class C
            {
                void M()
                {
                    this.[|Goo|] = 1 + 2;
                }
            """);

        await TestSpansAsync(
            """
            class C
            {
                void M()
                {
                    this.[|Goo|] += Bar() }
            """);

        await TestSpansAsync(
            """
            class C
            {
                void M()
                {
                    this.[|Goo|] += Bar();
                }
            """);
    }

    [Fact]
    public Task TestGenerateFieldInSimpleLambda()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<string, int> f = x => [|goo|];
                }
            }
            """,
            """
            using System;

            class Program
            {
                private static int goo;

                static void Main(string[] args)
                {
                    Func<string, int> f = x => goo;
                }
            }
            """, FieldIndex);

    [Fact]
    public Task TestGenerateFieldInParenthesizedLambda()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<int> f = () => [|goo|];
                }
            }
            """,
            """
            using System;

            class Program
            {
                private static int goo;

                static void Main(string[] args)
                {
                    Func<int> f = () => goo;
                }
            }
            """, FieldIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30232")]
    public Task TestGenerateFieldInAsyncTaskOfTSimpleLambda()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<string, Task<int>> f = async x => [|goo|];
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                private static int goo;

                static void Main(string[] args)
                {
                    Func<string, Task<int>> f = async x => goo;
                }
            }
            """, FieldIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30232")]
    public Task TestGenerateFieldInAsyncTaskOfTParenthesizedLambda()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<Task<int>> f = async () => [|goo|];
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                private static int goo;

                static void Main(string[] args)
                {
                    Func<Task<int>> f = async () => goo;
                }
            }
            """, FieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539427")]
    public Task TestGenerateFromLambda()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(int i)
                {
                    [|goo|] = () => {
                        return 2 };
                }
            }
            """,
            """
            using System;

            class Class
            {
                private Func<int> goo;

                void Method(int i)
                {
                    goo = () => {
                        return 2 };
                }
            }
            """);

    // TODO: Move to TypeInferrer.InferTypes, or something
    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539466")]
    public Task TestGenerateInMethodOverload1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(int i)
                {
                    System.Console.WriteLine([|goo|]);
                }
            }
            """,
            """
            class Class
            {
                private bool goo;

                void Method(int i)
                {
                    System.Console.WriteLine(goo);
                }
            }
            """);

    // TODO: Move to TypeInferrer.InferTypes, or something
    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539466")]
    public Task TestGenerateInMethodOverload2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(int i)
                {
                    System.Console.WriteLine(this.[|goo|]);
                }
            }
            """,
            """
            class Class
            {
                private bool goo;

                void Method(int i)
                {
                    System.Console.WriteLine(this.goo);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
    public Task TestExplicitProperty1()
        => TestInRegularAndScriptAsync(
            """
            class Class : ITest
            {
                bool ITest.[|SomeProp|] { get; set; }
            }

            interface ITest
            {
            }
            """,
            """
            class Class : ITest
            {
                bool ITest.SomeProp { get; set; }
            }

            interface ITest
            {
                bool SomeProp { get; set; }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
    public Task TestExplicitProperty2()
        => TestInRegularAndScriptAsync(
            """
            class Class : ITest
            {
                bool ITest.[|SomeProp|] { }
            }

            interface ITest
            {
            }
            """,
            """
            class Class : ITest
            {
                bool ITest.SomeProp { }
            }

            interface ITest
            {
                bool SomeProp { get; set; }
            }
            """, index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
    public Task TestExplicitProperty3()
        => TestInRegularAndScriptAsync(
            """
            class Class : ITest
            {
                bool ITest.[|SomeProp|] { }
            }

            interface ITest
            {
            }
            """,
            """
            class Class : ITest
            {
                bool ITest.SomeProp { }
            }

            interface ITest
            {
                bool SomeProp { get; }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
    public Task TestExplicitProperty4()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                bool ITest.[|SomeProp|] { }
            }

            interface ITest
            {
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539468")]
    public Task TestExplicitProperty5()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class : ITest
            {
                bool ITest.[|SomeProp|] { }
            }

            interface ITest
            {
                bool SomeProp { get; }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
    public Task TestEscapedName()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|@goo|];
                }
            }
            """,
            """
            class Class
            {
                private object goo;

                void Method()
                {
                    @goo;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539489")]
    public Task TestEscapedKeyword()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|@int|];
                }
            }
            """,
            """
            class Class
            {
                private object @int;

                void Method()
                {
                    @int;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539529")]
    public Task TestRefLambda()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|test|] = (ref int x) => x = 10;
                }
            }
            """,
            """
            class Class
            {
                private object test;

                void Method()
                {
                    test = (ref int x) => x = 10;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539595")]
    public Task TestNotOnError()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                void F<U, V>(U u1, V v1)
                {
                    Goo<string, int>([|u1|], u2);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539571")]
    public Task TestNameSimplification()
        => TestInRegularAndScriptAsync(
            """
            namespace TestNs
            {
                class Program
                {
                    class Test
                    {
                        void Meth()
                        {
                            Program.[|blah|] = new Test();
                        }
                    }
                }
            }
            """,
            """
            namespace TestNs
            {
                class Program
                {
                    private static Test blah;

                    class Test
                    {
                        void Meth()
                        {
                            Program.blah = new Test();
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539717")]
    public Task TestPostIncrement()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|i|]++;
                }
            }
            """,
            """
            class Program
            {
                private static int i;

                static void Main(string[] args)
                {
                    i++;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539717")]
    public Task TestPreDecrement()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    --[|i|];
                }
            }
            """,
            """
            class Program
            {
                private static int i;

                static void Main(string[] args)
                {
                    --i;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539738")]
    public Task TestGenerateIntoScript()
        => TestAsync(
            """
            using C;

            static class C
            {
            }

            C.[|i|] ++ ;
            """,
            """
            using C;

            static class C
            {
                internal static int i;
            }

            C.i ++ ;
            """,
            new(parseOptions: Options.Script));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539558")]
    public Task BugFix5565()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    [|Goo|]#();
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                public static object Goo { get; private set; }

                static void Main(string[] args)
                {
                    Goo#();
                }
            }
            """);

    [Fact(Skip = "Tuples")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539536")]
    public Task BugFix5538()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    new([|goo|])();
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                public static object goo { get; private set; }

                static void Main(string[] args)
                {
                    new(goo)();
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539665")]
    public Task BugFix5697()
        => TestInRegularAndScriptAsync(
            """
            class C { }
            class D
            {
                void M()
                {
                    C.[|P|] = 10;
                }
            }
            """,
            """
            class C
            {
                public static int P { get; internal set; }
            }
            class D
            {
                void M()
                {
                    C.P = 10;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539793")]
    public async Task TestIncrement()
    {
        await TestExactActionSetOfferedAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|p|]++;
                }
            }
            """,
            [string.Format(CodeFixesResources.Generate_field_0, "p"), string.Format(CodeFixesResources.Generate_property_0, "p"), string.Format(CodeFixesResources.Generate_local_0, "p"), string.Format(CodeFixesResources.Generate_parameter_0, "p")]);

        await TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|p|]++;
                }
            }
            """,
            """
            class Program
            {
                private static int p;

                static void Main()
                {
                    p++;
                }
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539834")]
    public Task TestNotInGoto()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    goto [|goo|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539826")]
    public Task TestOnLeftOfDot()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    [|goo|].ToString();
                }
            }
            """,
            """
            class Program
            {
                private static object goo;

                static void Main()
                {
                    goo.ToString();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539840")]
    public Task TestNotBeforeAlias()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    [|global|]::System.String s;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539871")]
    public Task TestMissingOnGenericName()
        => TestMissingInRegularAndScriptAsync(
            """
            class C<T>
            {
                public delegate void Goo<R>(R r);

                static void M()
                {
                    Goo<T> r = [|Goo<T>|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539934")]
    public Task TestOnDelegateAddition()
        => TestAsync(
            """
            class C
            {
                delegate void D();

                void M()
                {
                    D d = [|M1|] + M2;
                }
            }
            """,
            """
            class C
            {
                private D M1 { get; set; }

                delegate void D();

                void M()
                {
                    D d = M1 + M2;
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539986")]
    public Task TestReferenceTypeParameter1()
        => TestInRegularAndScriptAsync(
            """
            class C<T>
            {
                public void Test()
                {
                    C<T> c = A.[|M|];
                }
            }

            class A
            {
            }
            """,
            """
            class C<T>
            {
                public void Test()
                {
                    C<T> c = A.M;
                }
            }

            class A
            {
                public static C<object> M { get; internal set; }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539986")]
    public Task TestReferenceTypeParameter2()
        => TestInRegularAndScriptAsync(
            """
            class C<T>
            {
                public void Test()
                {
                    C<T> c = A.[|M|];
                }

                class A
                {
                }
            }
            """,
            """
            class C<T>
            {
                public void Test()
                {
                    C<T> c = A.M;
                }

                class A
                {
                    public static C<T> M { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540159")]
    public async Task TestEmptyIdentifierName()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static void M()
                {
                    int i = [|@|] }
            }
            """);
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static void M()
                {
                    int i = [|@|]}
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541194")]
    public Task TestForeachVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    foreach (var v in [|list|])
                    {
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                private IEnumerable<object> list;

                void M()
                {
                    foreach (var v in list)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541265")]
    public Task TestExtensionMethodUsedAsInstance()
        => TestAsync(
            """
            using System;

            class C
            {
                public static void Main()
                {
                    string s = "Hello";
                    [|f|] = s.ExtensionMethod;
                }
            }

            public static class MyExtension
            {
                public static int ExtensionMethod(this String s)
                {
                    return s.Length;
                }
            }
            """,
            """
            using System;

            class C
            {
                private static Func<int> f;

                public static void Main()
                {
                    string s = "Hello";
                    f = s.ExtensionMethod;
                }
            }

            public static class MyExtension
            {
                public static int ExtensionMethod(this String s)
                {
                    return s.Length;
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541549")]
    public Task TestDelegateInvoke()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<int, int> f = x => x + 1;
                    f([|x|]);
                }
            }
            """,
            """
            using System;

            class Program
            {
                private static int x;

                static void Main(string[] args)
                {
                    Func<int, int> f = x => x + 1;
                    f(x);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541597")]
    public Task TestComplexAssign1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|a|] = a + 10;
                }
            }
            """,
            """
            class Program
            {
                private static int a;

                static void Main(string[] args)
                {
                    a = a + 10;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541597")]
    public Task TestComplexAssign2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    a = [|a|] + 10;
                }
            }
            """,
            """
            class Program
            {
                private static int a;

                static void Main(string[] args)
                {
                    a = a + 10;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541659")]
    public Task TestTypeNamedVar()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                public static void Main()
                {
                    var v = [|p|];
                }
            }

            class var
            {
            }
            """,
            """
            using System;

            class Program
            {
                private static var p;

                public static void Main()
                {
                    var v = p;
                }
            }

            class var
            {
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541675")]
    public Task TestStaticExtensionMethodArgument()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    MyExtension.ExMethod([|ss|]);
                }
            }

            static class MyExtension
            {
                public static int ExMethod(this string s)
                {
                    return s.Length;
                }
            }
            """,
            """
            using System;

            class Program
            {
                private static string ss;

                static void Main(string[] args)
                {
                    MyExtension.ExMethod(ss);
                }
            }

            static class MyExtension
            {
                public static int ExMethod(this string s)
                {
                    return s.Length;
                }
            }
            """);

    [Fact]
    public Task SpeakableTopLevelStatementType()
        => TestMissingAsync("""
            [|P|] = 10;

            partial class Program
            {
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539675")]
    public Task AddBlankLineBeforeCommentBetweenMembers1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                //method
                static void Main(string[] args)
                {
                    [|P|] = 10;
                }
            }
            """,
            """
            class Program
            {
                public static int P { get; private set; }

                //method
                static void Main(string[] args)
                {
                    P = 10;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539675")]
    public Task AddBlankLineBeforeCommentBetweenMembers2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                //method
                static void Main(string[] args)
                {
                    [|P|] = 10;
                }
            }
            """,
            """
            class Program
            {
                private static int P;

                //method
                static void Main(string[] args)
                {
                    P = 10;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
    public Task AddBlankLineBetweenMembers1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|P|] = 10;
                }
            }
            """,
            """
            class Program
            {
                private static int P;

                static void Main(string[] args)
                {
                    P = 10;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
    public Task AddBlankLineBetweenMembers2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|P|] = 10;
                }
            }
            """,
            """
            class Program
            {
                public static int P { get; private set; }

                static void Main(string[] args)
                {
                    P = 10;
                }
            }
            """,
            index: 0);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
    public Task DoNotAddBlankLineBetweenFields()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                private static int P;

                static void Main(string[] args)
                {
                    P = 10;
                    [|A|] = 9;
                }
            }
            """,
            """
            class Program
            {
                private static int P;
                private static int A;

                static void Main(string[] args)
                {
                    P = 10;
                    A = 9;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543813")]
    public Task DoNotAddBlankLineBetweenAutoProperties()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public static int P { get; private set; }

                static void Main(string[] args)
                {
                    P = 10;
                    [|A|] = 9;
                }
            }
            """,
            """
            class Program
            {
                public static int P { get; private set; }
                public static int A { get; private set; }

                static void Main(string[] args)
                {
                    P = 10;
                    A = 9;
                }
            }
            """,
            index: 0);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539665")]
    public Task TestIntoEmptyClass()
        => TestInRegularAndScriptAsync(
            """
            class C { }
            class D
            {
                void M()
                {
                    C.[|P|] = 10;
                }
            }
            """,
            """
            class C
            {
                public static int P { get; internal set; }
            }
            class D
            {
                void M()
                {
                    C.P = 10;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540595")]
    public Task TestGeneratePropertyInScript()
        => TestAsync(
            @"[|Goo|]",
            """
            object Goo { get; private set; }

            Goo
            """,
            new(parseOptions: Options.Script));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542535")]
    public async Task TestConstantInParameterValue()
    {
        const string Initial =
            """
            class C
            {   
                const int y = 1 ; 
                public void Goo ( bool x = [|undeclared|] ) { }
            }
            """;

        await TestActionCountAsync(
Initial,
count: 1);

        await TestInRegularAndScriptAsync(
Initial,
"""
class C
{   
    const int y = 1 ;
    private const bool undeclared;

    public void Goo ( bool x = undeclared ) { }
}
""");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542900")]
    public Task TestGenerateFromAttributeNamedArgument1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class ProgramAttribute : Attribute
            {
                [Program([|Name|] = 0)]
                static void Main(string[] args)
                {
                }
            }
            """,
            """
            using System;

            class ProgramAttribute : Attribute
            {
                public int Name { get; set; }

                [Program(Name = 0)]
                static void Main(string[] args)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542900")]
    public Task TestGenerateFromAttributeNamedArgument2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class ProgramAttribute : Attribute
            {
                [Program([|Name|] = 0)]
                static void Main(string[] args)
                {
                }
            }
            """,
            """
            using System;

            class ProgramAttribute : Attribute
            {
                public int Name;

                [Program(Name = 0)]
                static void Main(string[] args)
                {
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility1_InternalPrivate()
        => TestAsync(
            """
            class Program
            {
                public static void Main()
                {
                    C c = [|P|];
                }

                private class C
                {
                }
            }
            """,
            """
            class Program
            {
                private static C P { get; set; }

                public static void Main()
                {
                    C c = P;
                }

                private class C
                {
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility2_InternalProtected()
        => TestAsync(
            """
            class Program
            {
                public static void Main()
                {
                    C c = [|P|];
                }

                protected class C
                {
                }
            }
            """,
            """
            class Program
            {
                protected static C P { get; private set; }

                public static void Main()
                {
                    C c = P;
                }

                protected class C
                {
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility3_InternalInternal()
        => TestAsync(
            """
            class Program
            {
                public static void Main()
                {
                    C c = [|P|];
                }

                internal class C
                {
                }
            }
            """,
            """
            class Program
            {
                public static C P { get; private set; }

                public static void Main()
                {
                    C c = P;
                }

                internal class C
                {
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility4_InternalProtectedInternal()
        => TestAsync(
            """
            class Program
            {
                public static void Main()
                {
                    C c = [|P|];
                }

                protected internal class C
                {
                }
            }
            """,
            """
            class Program
            {
                public static C P { get; private set; }

                public static void Main()
                {
                    C c = P;
                }

                protected internal class C
                {
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility5_InternalPublic()
        => TestAsync(
            """
            class Program
            {
                public static void Main()
                {
                    C c = [|P|];
                }

                public class C
                {
                }
            }
            """,
            """
            class Program
            {
                public static C P { get; private set; }

                public static void Main()
                {
                    C c = P;
                }

                public class C
                {
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility6_PublicInternal()
        => TestAsync(
            """
            public class Program
            {
                public static void Main()
                {
                    C c = [|P|];
                }

                internal class C
                {
                }
            }
            """,
            """
            public class Program
            {
                internal static C P { get; private set; }

                public static void Main()
                {
                    C c = P;
                }

                internal class C
                {
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility7_PublicProtectedInternal()
        => TestAsync(
            """
            public class Program
            {
                public static void Main()
                {
                    C c = [|P|];
                }

                protected internal class C
                {
                }
            }
            """,
            """
            public class Program
            {
                protected internal static C P { get; private set; }

                public static void Main()
                {
                    C c = P;
                }

                protected internal class C
                {
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility8_PublicProtected()
        => TestAsync(
            """
            public class Program
            {
                public static void Main()
                {
                    C c = [|P|];
                }

                protected class C
                {
                }
            }
            """,
            """
            public class Program
            {
                protected static C P { get; private set; }

                public static void Main()
                {
                    C c = P;
                }

                protected class C
                {
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility9_PublicPrivate()
        => TestAsync(
            """
            public class Program
            {
                public static void Main()
                {
                    C c = [|P|];
                }

                private class C
                {
                }
            }
            """,
            """
            public class Program
            {
                private static C P { get; set; }

                public static void Main()
                {
                    C c = P;
                }

                private class C
                {
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility10_PrivatePrivate()
        => TestAsync(
            """
            class outer
            {
                private class Program
                {
                    public static void Main()
                    {
                        C c = [|P|];
                    }

                    private class C
                    {
                    }
                }
            }
            """,
            """
            class outer
            {
                private class Program
                {
                    public static C P { get; private set; }

                    public static void Main()
                    {
                        C c = P;
                    }

                    private class C
                    {
                    }
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility11_PrivateProtected()
        => TestAsync(
            """
            class outer
            {
                private class Program
                {
                    public static void Main()
                    {
                        C c = [|P|];
                    }

                    protected class C
                    {
                    }
                }
            }
            """,
            """
            class outer
            {
                private class Program
                {
                    public static C P { get; private set; }

                    public static void Main()
                    {
                        C c = P;
                    }

                    protected class C
                    {
                    }
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility12_PrivateProtectedInternal()
        => TestAsync(
            """
            class outer
            {
                private class Program
                {
                    public static void Main()
                    {
                        C c = [|P|];
                    }

                    protected internal class C
                    {
                    }
                }
            }
            """,
            """
            class outer
            {
                private class Program
                {
                    public static C P { get; private set; }

                    public static void Main()
                    {
                        C c = P;
                    }

                    protected internal class C
                    {
                    }
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility13_PrivateInternal()
        => TestAsync(
            """
            class outer
            {
                private class Program
                {
                    public static void Main()
                    {
                        C c = [|P|];
                    }

                    internal class C
                    {
                    }
                }
            }
            """,
            """
            class outer
            {
                private class Program
                {
                    public static C P { get; private set; }

                    public static void Main()
                    {
                        C c = P;
                    }

                    internal class C
                    {
                    }
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility14_ProtectedPrivate()
        => TestAsync(
            """
            class outer
            {
                protected class Program
                {
                    public static void Main()
                    {
                        C c = [|P|];
                    }

                    private class C
                    {
                    }
                }
            }
            """,
            """
            class outer
            {
                protected class Program
                {
                    private static C P { get; set; }

                    public static void Main()
                    {
                        C c = P;
                    }

                    private class C
                    {
                    }
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility15_ProtectedInternal()
        => TestAsync(
            """
            class outer
            {
                protected class Program
                {
                    public static void Main()
                    {
                        C c = [|P|];
                    }

                    internal class C
                    {
                    }
                }
            }
            """,
            """
            class outer
            {
                protected class Program
                {
                    public static C P { get; private set; }

                    public static void Main()
                    {
                        C c = P;
                    }

                    internal class C
                    {
                    }
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility16_ProtectedInternalProtected()
        => TestAsync(
            """
            class outer
            {
                protected internal class Program
                {
                    public static void Main()
                    {
                        C c = [|P|];
                    }

                    protected class C
                    {
                    }
                }
            }
            """,
            """
            class outer
            {
                protected internal class Program
                {
                    protected static C P { get; private set; }

                    public static void Main()
                    {
                        C c = P;
                    }

                    protected class C
                    {
                    }
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541698")]
    public Task TestMinimalAccessibility17_ProtectedInternalInternal()
        => TestAsync(
            """
            class outer
            {
                protected internal class Program
                {
                    public static void Main()
                    {
                        C c = [|P|];
                    }

                    internal class C
                    {
                    }
                }
            }
            """,
            """
            class outer
            {
                protected internal class Program
                {
                    public static C P { get; private set; }

                    public static void Main()
                    {
                        C c = P;
                    }

                    internal class C
                    {
                    }
                }
            }
            """,
            new(parseOptions: null));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543153")]
    public Task TestAnonymousObjectInitializer1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var a = new { x = 5 };
                    a = new { x = [|HERE|] };
                }
            }
            """,
            """
            class C
            {
                private int HERE;

                void M()
                {
                    var a = new { x = 5 };
                    a = new { x = HERE };
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543124")]
    public Task TestNoGenerationIntoAnonymousType()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var v = new { };
                    bool b = v.[|Bar|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543543")]
    public Task TestNotOfferedForBoundParametersOfOperators()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                public Program(string s)
                {
                }

                static void Main(string[] args)
                {
                    Program p = "";
                }

                public static implicit operator Program(string str)
                {
                    return new Program([|str|]);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544175")]
    public Task TestNotOnNamedParameterName1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class class1
            {
                public void Test()
                {
                    Goo([|x|]: x);
                }

                public string Goo(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544271")]
    public Task TestNotOnNamedParameterName2()
        => TestMissingInRegularAndScriptAsync(
            """
            class Goo
            {
                public Goo(int a = 42)
                {
                }
            }

            class DogBed : Goo
            {
                public DogBed(int b) : base([|a|]: b)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544164")]
    public Task TestPropertyOnObjectInitializer()
        => TestInRegularAndScriptAsync(
            """
            class Goo
            {
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { [|Gibberish|] = 24 };
                }
            }
            """,
            """
            class Goo
            {
                public int Gibberish { get; internal set; }
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { Gibberish = 24 };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49294")]
    public Task TestPropertyInWithInitializer()
        => TestInRegularAndScriptAsync(
            """
            record Goo
            {
            }

            class Bar
            {
                void goo(Goo g)
                {
                    var c = g with { [|Gibberish|] = 24 };
                }
            }
            """,
            """
            record Goo
            {
                public int Gibberish { get; internal set; }
            }

            class Bar
            {
                void goo(Goo g)
                {
                    var c = g with { Gibberish = 24 };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13166")]
    public Task TestPropertyOnNestedObjectInitializer()
        => TestInRegularAndScriptAsync(
            """
            public class Inner
            {
            }

            public class Outer
            {
                public Inner Inner { get; set; } = new Inner();

                public static Outer X() => new Outer { Inner = { [|InnerValue|] = 5 } };
            }
            """,
            """
            public class Inner
            {
                public int InnerValue { get; internal set; }
            }

            public class Outer
            {
                public Inner Inner { get; set; } = new Inner();

                public static Outer X() => new Outer { Inner = { InnerValue = 5 } };
            }
            """);

    [Fact]
    public Task TestPropertyOnObjectInitializer1()
        => TestInRegularAndScriptAsync(
            """
            class Goo
            {
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { [|Gibberish|] = Gibberish };
                }
            }
            """,
            """
            class Goo
            {
                public object Gibberish { get; internal set; }
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { Gibberish = Gibberish };
                }
            }
            """);

    [Fact]
    public Task TestPropertyOnObjectInitializer2()
        => TestInRegularAndScriptAsync(
            """
            class Goo
            {
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { Gibberish = [|Gibberish|] };
                }
            }
            """,
            """
            class Goo
            {
            }

            class Bar
            {
                public object Gibberish { get; private set; }

                void goo()
                {
                    var c = new Goo { Gibberish = Gibberish };
                }
            }
            """);

    [Fact]
    public Task TestFieldOnObjectInitializer()
        => TestInRegularAndScriptAsync(
            """
            class Goo
            {
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { [|Gibberish|] = 24 };
                }
            }
            """,
            """
            class Goo
            {
                internal int Gibberish;
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { Gibberish = 24 };
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestFieldOnObjectInitializer1()
        => TestInRegularAndScriptAsync(
            """
            class Goo
            {
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { [|Gibberish|] = Gibberish };
                }
            }
            """,
            """
            class Goo
            {
                internal object Gibberish;
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { Gibberish = Gibberish };
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestFieldOnObjectInitializer2()
        => TestInRegularAndScriptAsync(
            """
            class Goo
            {
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { Gibberish = [|Gibberish|] };
                }
            }
            """,
            """
            class Goo
            {
            }

            class Bar
            {
                private object Gibberish;

                void goo()
                {
                    var c = new Goo { Gibberish = Gibberish };
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestOnlyPropertyAndFieldOfferedForObjectInitializer()
        => TestActionCountAsync(
            """
            class Goo
            {
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { . [|Gibberish|] = 24 };
                }
            }
            """,
            2);

    [Fact]
    public Task TestGenerateLocalInObjectInitializerValue()
        => TestInRegularAndScriptAsync(
            """
            class Goo
            {
            }

            class Bar
            {
                void goo()
                {
                    var c = new Goo { Gibberish = [|blah|] };
                }
            }
            """,
            """
            class Goo
            {
            }

            class Bar
            {
                void goo()
                {
                    object blah = null;
                    var c = new Goo { Gibberish = blah };
                }
            }
            """,
            index: LocalIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544319")]
    public Task TestNotOnIncompleteMember1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Class1
            {
                Console.[|WriteLine|](); }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544319")]
    public Task TestNotOnIncompleteMember2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Class1
            { [|WriteLine|]();
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544319")]
    public Task TestNotOnIncompleteMember3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Class1
            {
                [|WriteLine|]
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544384")]
    public Task TestPointerType()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static int x;

                unsafe static void F(int* p)
                {
                    *p = 1;
                }

                static unsafe void Main(string[] args)
                {
                    int[] a = new int[10];
                    fixed (int* p2 = &x, int* p3 = ) F(GetP2([|p2|]));
                }

                unsafe private static int* GetP2(int* p2)
                {
                    return p2;
                }
            }
            """,
            """
            class Program
            {
                static int x;
                private static unsafe int* p2;

                unsafe static void F(int* p)
                {
                    *p = 1;
                }

                static unsafe void Main(string[] args)
                {
                    int[] a = new int[10];
                    fixed (int* p2 = &x, int* p3 = ) F(GetP2(p2));
                }

                unsafe private static int* GetP2(int* p2)
                {
                    return p2;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544510")]
    public Task TestNotOnUsingAlias()
        => TestMissingInRegularAndScriptAsync(
@"using [|S|] = System ; S . Console . WriteLine ( ""hello world"" ) ; ");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544907")]
    public Task TestExpressionTLambda()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                static void Main()
                {
                    Expression<Func<int, int>> e = x => [|Goo|];
                }
            }
            """,
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                public static int Goo { get; private set; }

                static void Main()
                {
                    Expression<Func<int, int>> e = x => Goo;
                }
            }
            """);

    [Fact]
    public Task TestNoGenerationIntoEntirelyHiddenType()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void Goo()
                {
                    int i = D.[|Bar|];
                }
            }

            #line hidden
            class D
            {
            }
            #line default
            """);

    [Fact]
    public Task TestInReturnStatement()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    return [|goo|];
                }
            }
            """,
            """
            class Program
            {
                private object goo;

                void Main()
                {
                    return goo;
                }
            }
            """);

    [Fact]
    public Task TestLocal1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Goo([|bar|]);
                }

                static void Goo(int i)
                {
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    int bar = 0;
                    Goo(bar);
                }

                static void Goo(int i)
                {
                }
            }
            """,
            index: LocalIndex);

    [Fact]
    public Task TestLocalTopLevelNullability()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class Program
            {
                void Main()
                {
                    Goo([|bar|]);
                }

                static void Goo(string? s)
                {
                }
            }
            """,
            """
            #nullable enable

            class Program
            {
                void Main()
                {
                    string? bar = null;
                    Goo(bar);
                }

                static void Goo(string? s)
                {
                }
            }
            """,
            index: LocalIndex);

    [Fact]
    public Task TestLocalNestedNullability()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class Program
            {
                void Main()
                {
                    Goo([|bar|]);
                }

                static void Goo(IEnumerable<string?> s)
                {
                }
            }
            """,
            """
            #nullable enable

            class Program
            {
                void Main()
                {
                    IEnumerable<string?> bar = null;
                    Goo(bar);
                }

                static void Goo(IEnumerable<string?> s)
                {
                }
            }
            """,
            index: LocalIndex);

    [Fact]
    public Task TestOutLocal1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Goo(out [|bar|]);
                }

                static void Goo(out int i)
                {
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    int bar;
                    Goo(out bar);
                }

                static void Goo(out int i)
                {
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809542")]
    public Task TestLocalBeforeComment()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
            #if true
                    // Banner Line 1
                    // Banner Line 2
                    int.TryParse("123", out [|local|]);
            #endif
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
            #if true
                    int local;
                    // Banner Line 1
                    // Banner Line 2
                    int.TryParse("123", out [|local|]);
            #endif
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809542")]
    public Task TestLocalAfterComment()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
            #if true
                    // Banner Line 1
                    // Banner Line 2

                    int.TryParse("123", out [|local|]);
            #endif
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
            #if true
                    // Banner Line 1
                    // Banner Line 2

                    int local;
                    int.TryParse("123", out [|local|]);
            #endif
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateIntoVisiblePortion()
        => TestInRegularAndScriptAsync(
            """
            using System;

            #line hidden
            class Program
            {
                void Main()
                {
            #line default
                    Goo(Program.[|X|])
                }
            }
            """,
            """
            using System;

            #line hidden
            class Program
            {
                void Main()
                {
            #line default
                    Goo(Program.X)
                }

                public static object X { get; private set; }
            }
            """);

    [Fact]
    public Task TestMissingWhenNoAvailableRegionToGenerateInto()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            #line hidden
            class Program
            {
                void Main()
                {
            #line default
                    Goo(Program.[|X|])


            #line hidden
                }
            }
            #line default
            """);

    [Fact]
    public Task TestGenerateLocalAvailableIfBlockIsNotHidden()
        => TestInRegularAndScriptAsync(
            """
            using System;

            #line hidden
            class Program
            {
            #line default
                void Main()
                {
                    Goo([|x|]);
                }
            #line hidden
            }
            #line default
            """,
            """
            using System;

            #line hidden
            class Program
            {
            #line default
                void Main()
                {
                    object x = null;
                    Goo(x);
                }
            #line hidden
            }
            #line default
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545217")]
    public Task TestGenerateLocalNameSimplificationCSharp7()
        => TestAsync(
            """
            class Program
            {
                void goo()
                {
                    bar([|xyz|]);
                }

                struct sfoo
                {
                }

                void bar(sfoo x)
                {
                }
            }
            """,
            """
            class Program
            {
                void goo()
                {
                    sfoo xyz = default(sfoo);
                    bar(xyz);
                }

                struct sfoo
                {
                }

                void bar(sfoo x)
                {
                }
            }
            """,
            new(index: 3, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7)));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545217")]
    public Task TestGenerateLocalNameSimplification()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void goo()
                {
                    bar([|xyz|]);
                }

                struct sfoo
                {
                }

                void bar(sfoo x)
                {
                }
            }
            """,
            """
            class Program
            {
                void goo()
                {
                    sfoo xyz = default;
                    bar(xyz);
                }

                struct sfoo
                {
                }

                void bar(sfoo x)
                {
                }
            }
            """,
            index: LocalIndex);

    [Fact]
    public Task TestParenthesizedExpression()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    int v = 1 + ([|k|]);
                }
            }
            """,
            """
            class Program
            {
                private int k;

                void Main()
                {
                    int v = 1 + (k);
                }
            }
            """);

    [Fact]
    public Task TestInSelect()
        => TestInRegularAndScriptAsync(
            """
            using System.Linq;

            class Program
            {
                void Main(string[] args)
                {
                    var q = from a in args
                            select [|v|];
                }
            }
            """,
            """
            using System.Linq;

            class Program
            {
                private object v;

                void Main(string[] args)
                {
                    var q = from a in args
                            select v;
                }
            }
            """);

    [Fact]
    public Task TestInChecked()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    int[] a = null;
                    int[] temp = checked([|goo|]);
                }
            }
            """,
            """
            class Program
            {
                private int[] goo;

                void Main()
                {
                    int[] a = null;
                    int[] temp = checked(goo);
                }
            }
            """);

    [Fact]
    public Task TestInArrayRankSpecifier()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    var v = new int[[|k|]];
                }
            }
            """,
            """
            class Program
            {
                private int k;

                void Main()
                {
                    var v = new int[k];
                }
            }
            """);

    [Fact]
    public Task TestInConditional1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    int i = [|goo|] ? bar : baz;
                }
            }
            """,
            """
            class Program
            {
                private static bool goo;

                static void Main()
                {
                    int i = goo ? bar : baz;
                }
            }
            """);

    [Fact]
    public Task TestInConditional2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    int i = goo ? [|bar|] : baz;
                }
            }
            """,
            """
            class Program
            {
                private static int bar;

                static void Main()
                {
                    int i = goo ? bar : baz;
                }
            }
            """);

    [Fact]
    public Task TestInConditional3()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    int i = goo ? bar : [|baz|];
                }
            }
            """,
            """
            class Program
            {
                private static int baz;

                static void Main()
                {
                    int i = goo ? bar : baz;
                }
            }
            """);

    [Fact]
    public Task TestInCast()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    var x = (int)[|y|];
                }
            }
            """,
            """
            class Program
            {
                private int y;

                void Main()
                {
                    var x = (int)y;
                }
            }
            """);

    [Fact]
    public Task TestInIf()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    if ([|goo|])
                    {
                    }
                }
            }
            """,
            """
            class Program
            {
                private bool goo;

                void Main()
                {
                    if (goo)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestInSwitch()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    switch ([|goo|])
                    {
                    }
                }
            }
            """,
            """
            class Program
            {
                private int goo;

                void Main()
                {
                    switch (goo)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingOnNamespace()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    [|System|].Console.WriteLine(4);
                }
            }
            """);

    [Fact]
    public Task TestMissingOnType()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    [|System.Console|].WriteLine(4);
                }
            }
            """);

    [Fact]
    public Task TestMissingOnBase()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    [|base|].ToString();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545273")]
    public Task TestGenerateFromAssign1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    [|undefined|] = 1;
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    var undefined = 1;
                }
            }
            """,
            index: PropertyIndex, new(options: ImplicitTypingEverywhere()));

    [Fact]
    public Task TestFuncAssignment()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    [|undefined|] = (x) => 2;
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    System.Func<object, int> undefined = (x) => 2;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545273")]
    public Task TestGenerateFromAssign1NotAsVar()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    [|undefined|] = 1;
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    int undefined = 1;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545273")]
    public Task TestGenerateFromAssign2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    [|undefined|] = new { P = "1" };
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    var undefined = new { P = "1" };
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545269")]
    public Task TestGenerateInVenus1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
            #line 1 "goo"
                void Goo()
                {
                    this.[|Bar|] = 1;
                }
            #line default
            #line hidden
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545269")]
    public async Task TestGenerateInVenus2()
    {
        var code = """
            class C
            {
            #line 1 "goo"
                void Goo()
                {
                    [|Bar|] = 1;
                }
            #line default
            #line hidden
            }
            """;
        await TestExactActionSetOfferedAsync(code, [string.Format(CodeFixesResources.Generate_local_0, "Bar"), string.Format(CodeFixesResources.Generate_parameter_0, "Bar")]);

        await TestInRegularAndScriptAsync(code,
            """
            class C
            {
            #line 1 "goo"
                void Goo()
                {
                    var [|Bar|] = 1;
                }
            #line default
            #line hidden
            }
            """, new(options: ImplicitTypingEverywhere()));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546027")]
    public Task TestGeneratePropertyFromAttribute()
        => TestInRegularAndScriptAsync(
            """
            using System;

            [AttributeUsage(AttributeTargets.Class)]
            class MyAttrAttribute : Attribute
            {
            }

            [MyAttr(123, [|Value|] = 1)]
            class D
            {
            }
            """,
            """
            using System;

            [AttributeUsage(AttributeTargets.Class)]
            class MyAttrAttribute : Attribute
            {
                public int Value { get; set; }
            }

            [MyAttr(123, Value = 1)]
            class D
            {
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545232")]
    public Task TestNewLinePreservationBeforeInsertingLocal()
        => TestInRegularAndScriptAsync(
            """
            using System;
            namespace CSharpDemoApp
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        const int MEGABYTE = 1024 * 1024;
                        Console.WriteLine(MEGABYTE);

                        Calculate([|multiplier|]);
                    }
                    static void Calculate(double multiplier = Math.PI)
                    {
                    }
                }
            }
            """,
            """
            using System;
            namespace CSharpDemoApp
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        const int MEGABYTE = 1024 * 1024;
                        Console.WriteLine(MEGABYTE);

                        double multiplier = 0;
                        Calculate(multiplier);
                    }
                    static void Calculate(double multiplier = Math.PI)
                    {
                    }
                }
            }
            """,
            index: LocalIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863346")]
    public Task TestGenerateInGenericMethod_Local()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class TestClass<T1>
            {
                static T TestMethod<T>(T item)
                {
                    T t = WrapFunc<T>([|NewLocal|]);
                    return t;
                }

                private static T WrapFunc<T>(Func<T1, T> function)
                {
                    T1 zoo = default(T1);
                    return function(zoo);
                }
            }
            """,
            """
            using System;
            class TestClass<T1>
            {
                static T TestMethod<T>(T item)
                {
                    Func<T1, T> NewLocal = null;
                    T t = WrapFunc<T>(NewLocal);
                    return t;
                }

                private static T WrapFunc<T>(Func<T1, T> function)
                {
                    T1 zoo = default(T1);
                    return function(zoo);
                }
            }
            """,
            index: LocalIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863346")]
    public Task TestGenerateInGenericMethod_Property()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class TestClass<T1>
            {
                static T TestMethod<T>(T item)
                {
                    T t = WrapFunc<T>([|NewLocal|]);
                    return t;
                }

                private static T WrapFunc<T>(Func<T1, T> function)
                {
                    T1 zoo = default(T1);
                    return function(zoo);
                }
            }
            """,
            """
            using System;
            class TestClass<T1>
            {
                public static Func<T1, object> NewLocal { get; private set; }

                static T TestMethod<T>(T item)
                {
                    T t = WrapFunc<T>(NewLocal);
                    return t;
                }

                private static T WrapFunc<T>(Func<T1, T> function)
                {
                    T1 zoo = default(T1);
                    return function(zoo);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865067")]
    public Task TestWithYieldReturnInMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                IEnumerable<DayOfWeek> Goo()
                {
                    yield return [|abc|];
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                private DayOfWeek abc;

                IEnumerable<DayOfWeek> Goo()
                {
                    yield return abc;
                }
            }
            """);

    [Fact]
    public Task TestWithYieldReturnInAsyncMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                async IAsyncEnumerable<DayOfWeek> Goo()
                {
                    yield return [|abc|];
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                private DayOfWeek abc;

                async IAsyncEnumerable<DayOfWeek> Goo()
                {
                    yield return abc;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30235")]
    public Task TestWithYieldReturnInLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                void M()
                {
                    IEnumerable<DayOfWeek> F()
                    {
                        yield return [|abc|];
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                private DayOfWeek abc;

                void M()
                {
                    IEnumerable<DayOfWeek> F()
                    {
                        yield return abc;
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/877580")]
    public Task TestWithThrow()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Goo()
                {
                    throw [|MyExp|];
                }
            }
            """,
            """
            using System;

            class Program
            {
                private Exception MyExp;

                void Goo()
                {
                    throw MyExp;
                }
            }
            """, index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeField()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|int* a = goo|];
                }
            }
            """,
            """
            class Class
            {
                private unsafe int* goo;

                void Method()
                {
                    int* a = goo;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeField2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|int*[] a = goo|];
                }
            }
            """,
            """
            class Class
            {
                private unsafe int*[] goo;

                void Method()
                {
                    int*[] a = goo;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeFieldInUnsafeClass()
        => TestInRegularAndScriptAsync(
            """
            unsafe class Class
            {
                void Method()
                {
                    [|int* a = goo|];
                }
            }
            """,
            """
            unsafe class Class
            {
                private int* goo;

                void Method()
                {
                    int* a = goo;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeFieldInNestedClass()
        => TestInRegularAndScriptAsync(
            """
            unsafe class Class
            {
                class MyClass
                {
                    void Method()
                    {
                        [|int* a = goo|];
                    }
                }
            }
            """,
            """
            unsafe class Class
            {
                class MyClass
                {
                    private int* goo;

                    void Method()
                    {
                        int* a = goo;
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeFieldInNestedClass2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                unsafe class MyClass
                {
                    void Method()
                    {
                        [|int* a = Class.goo|];
                    }
                }
            }
            """,
            """
            class Class
            {
                private static unsafe int* goo;

                unsafe class MyClass
                {
                    void Method()
                    {
                        int* a = Class.goo;
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeReadOnlyField()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|int* a = goo|];
                }
            }
            """,
            """
            class Class
            {
                private readonly unsafe int* goo;

                void Method()
                {
                    int* a = goo;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeReadOnlyField2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|int*[] a = goo|];
                }
            }
            """,
            """
            class Class
            {
                private readonly unsafe int*[] goo;

                void Method()
                {
                    int*[] a = goo;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeReadOnlyFieldInUnsafeClass()
        => TestInRegularAndScriptAsync(
            """
            unsafe class Class
            {
                void Method()
                {
                    [|int* a = goo|];
                }
            }
            """,
            """
            unsafe class Class
            {
                private readonly int* goo;

                void Method()
                {
                    int* a = goo;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeReadOnlyFieldInNestedClass()
        => TestInRegularAndScriptAsync(
            """
            unsafe class Class
            {
                class MyClass
                {
                    void Method()
                    {
                        [|int* a = goo|];
                    }
                }
            }
            """,
            """
            unsafe class Class
            {
                class MyClass
                {
                    private readonly int* goo;

                    void Method()
                    {
                        int* a = goo;
                    }
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeReadOnlyFieldInNestedClass2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                unsafe class MyClass
                {
                    void Method()
                    {
                        [|int* a = Class.goo|];
                    }
                }
            }
            """,
            """
            class Class
            {
                private static readonly unsafe int* goo;

                unsafe class MyClass
                {
                    void Method()
                    {
                        int* a = Class.goo;
                    }
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeProperty()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|int* a = goo|];
                }
            }
            """,
            """
            class Class
            {
                public unsafe int* goo { get; private set; }

                void Method()
                {
                    int* a = goo;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafeProperty2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|int*[] a = goo|];
                }
            }
            """,
            """
            class Class
            {
                public unsafe int*[] goo { get; private set; }

                void Method()
                {
                    int*[] a = goo;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafePropertyInUnsafeClass()
        => TestInRegularAndScriptAsync(
            """
            unsafe class Class
            {
                void Method()
                {
                    [|int* a = goo|];
                }
            }
            """,
            """
            unsafe class Class
            {
                public int* goo { get; private set; }

                void Method()
                {
                    int* a = goo;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafePropertyInNestedClass()
        => TestInRegularAndScriptAsync(
            """
            unsafe class Class
            {
                class MyClass
                {
                    void Method()
                    {
                        [|int* a = goo|];
                    }
                }
            }
            """,
            """
            unsafe class Class
            {
                class MyClass
                {
                    public int* goo { get; private set; }

                    void Method()
                    {
                        int* a = goo;
                    }
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530177")]
    public Task TestUnsafePropertyInNestedClass2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                unsafe class MyClass
                {
                    void Method()
                    {
                        [|int* a = Class.goo|];
                    }
                }
            }
            """,
            """
            class Class
            {
                public static unsafe int* goo { get; private set; }

                unsafe class MyClass
                {
                    void Method()
                    {
                        int* a = Class.goo;
                    }
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z|]);
                }
            }
            """,
            """
            class C
            {
                public object Z { get; private set; }

                void M()
                {
                    var x = nameof(Z);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z|]);
                }
            }
            """,
            """
            class C
            {
                private object Z;

                void M()
                {
                    var x = nameof(Z);
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfReadonlyField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z|]);
                }
            }
            """,
            """
            class C
            {
                private readonly object Z;

                void M()
                {
                    var x = nameof(Z);
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfLocal()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z|]);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    object Z = null;
                    var x = nameof(Z);
                }
            }
            """,
            index: LocalIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfProperty2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z.X|]);
                }
            }
            """,
            """
            class C
            {
                public object Z { get; private set; }

                void M()
                {
                    var x = nameof(Z.X);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfField2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z.X|]);
                }
            }
            """,
            """
            class C
            {
                private object Z;

                void M()
                {
                    var x = nameof(Z.X);
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfReadonlyField2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z.X|]);
                }
            }
            """,
            """
            class C
            {
                private readonly object Z;

                void M()
                {
                    var x = nameof(Z.X);
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfLocal2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z.X|]);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    object Z = null;
                    var x = nameof(Z.X);
                }
            }
            """,
            index: LocalIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfProperty3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z.X.Y|]);
                }
            }
            """,
            """
            class C
            {
                public object Z { get; private set; }

                void M()
                {
                    var x = nameof(Z.X.Y);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfField3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z.X.Y|]);
                }
            }
            """,
            """
            class C
            {
                private object Z;

                void M()
                {
                    var x = nameof(Z.X.Y);
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfReadonlyField3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z.X.Y|]);
                }
            }
            """,
            """
            class C
            {
                private readonly object Z;

                void M()
                {
                    var x = nameof(Z.X.Y);
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfLocal3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|Z.X.Y|]);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    object Z = null;
                    var x = nameof(Z.X.Y);
                }
            }
            """,
            index: LocalIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfMissing()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = [|nameof(1 + 2)|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfMissing2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var y = 1 + 2;
                    var x = [|nameof(y)|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfMissing3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var y = 1 + 2;
                    var z = "";
                    var x = [|nameof(y, z)|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfProperty4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|y|], z);
                }
            }
            """,
            """
            class C
            {
                public object y { get; private set; }

                void M()
                {
                    var x = nameof(y, z);
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfField4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|y|], z);
                }
            }
            """,
            """
            class C
            {
                private object y;

                void M()
                {
                    var x = nameof(y, z);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfReadonlyField4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|y|], z);
                }
            }
            """,
            """
            class C
            {
                private readonly object y;

                void M()
                {
                    var x = nameof(y, z);
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfLocal4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|y|], z);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    object y = null;
                    var x = nameof(y, z);
                }
            }
            """,
            index: LocalIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfProperty5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|y|]);
                }

                private object nameof(object y)
                {
                    return null;
                }
            }
            """,
            """
            class C
            {
                public object y { get; private set; }

                void M()
                {
                    var x = nameof(y);
                }

                private object nameof(object y)
                {
                    return null;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfField5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|y|]);
                }

                private object nameof(object y)
                {
                    return null;
                }
            }
            """,
            """
            class C
            {
                private object y;

                void M()
                {
                    var x = nameof(y);
                }

                private object nameof(object y)
                {
                    return null;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfReadonlyField5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|y|]);
                }

                private object nameof(object y)
                {
                    return null;
                }
            }
            """,
            """
            class C
            {
                private readonly object y;

                void M()
                {
                    var x = nameof(y);
                }

                private object nameof(object y)
                {
                    return null;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")]
    public Task TestInsideNameOfLocal5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var x = nameof([|y|]);
                }

                private object nameof(object y)
                {
                    return null;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    object y = null;
                    var x = nameof(y);
                }

                private object nameof(object y)
                {
                    return null;
                }
            }
            """,
            index: LocalIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestConditionalAccessProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(C a)
                {
                    C x = a?[|.Instance|];
                }
            }
            """,
            """
            class C
            {
                public C Instance { get; private set; }

                void Main(C a)
                {
                    C x = a?.Instance;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestConditionalAccessField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(C a)
                {
                    C x = a?[|.Instance|];
                }
            }
            """,
            """
            class C
            {
                private C Instance;

                void Main(C a)
                {
                    C x = a?.Instance;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestConditionalAccessReadonlyField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(C a)
                {
                    C x = a?[|.Instance|];
                }
            }
            """,
            """
            class C
            {
                private readonly C Instance;

                void Main(C a)
                {
                    C x = a?.Instance;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestConditionalAccessVarProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(C a)
                {
                    var x = a?[|.Instance|];
                }
            }
            """,
            """
            class C
            {
                public object Instance { get; private set; }

                void Main(C a)
                {
                    var x = a?.Instance;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestConditionalAccessVarField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(C a)
                {
                    var x = a?[|.Instance|];
                }
            }
            """,
            """
            class C
            {
                private object Instance;

                void Main(C a)
                {
                    var x = a?.Instance;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestConditionalAccessVarReadOnlyField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(C a)
                {
                    var x = a?[|.Instance|];
                }
            }
            """,
            """
            class C
            {
                private readonly object Instance;

                void Main(C a)
                {
                    var x = a?.Instance;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestConditionalAccessNullableProperty()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(C a)
                {
                    int? x = a?[|.B|];
                }
            }
            """,
            """
            class C
            {
                public int B { get; private set; }

                void Main(C a)
                {
                    int? x = a?.B;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestConditionalAccessNullableField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(C a)
                {
                    int? x = a?[|.B|];
                }
            }
            """,
            """
            class C
            {
                private int B;

                void Main(C a)
                {
                    int? x = a?.B;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestConditionalAccessNullableReadonlyField()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void Main(C a)
                {
                    int? x = a?[|.B|];
                }
            }
            """,
            """
            class C
            {
                private readonly int B;

                void Main(C a)
                {
                    int? x = a?.B;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGeneratePropertyInConditionalAccessExpression()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    C x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    C x = a?.B.C;
                }

                public class E
                {
                    public C C { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGeneratePropertyInConditionalAccessExpression2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int x = a?.B.C;
                }

                public class E
                {
                    public int C { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGeneratePropertyInConditionalAccessExpression3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int? x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int? x = a?.B.C;
                }

                public class E
                {
                    public int C { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGeneratePropertyInConditionalAccessExpression4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    var x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    var x = a?.B.C;
                }

                public class E
                {
                    public object C { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGenerateFieldInConditionalAccessExpression()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    C x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    C x = a?.B.C;
                }

                public class E
                {
                    internal C C;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGenerateFieldInConditionalAccessExpression2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int x = a?.B.C;
                }

                public class E
                {
                    internal int C;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGenerateFieldInConditionalAccessExpression3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int? x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int? x = a?.B.C;
                }

                public class E
                {
                    internal int C;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGenerateFieldInConditionalAccessExpression4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    var x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    var x = a?.B.C;
                }

                public class E
                {
                    internal object C;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGenerateReadonlyFieldInConditionalAccessExpression()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    C x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    C x = a?.B.C;
                }

                public class E
                {
                    internal readonly C C;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGenerateReadonlyFieldInConditionalAccessExpression2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int x = a?.B.C;
                }

                public class E
                {
                    internal readonly int C;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGenerateReadonlyFieldInConditionalAccessExpression3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int? x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    int? x = a?.B.C;
                }

                public class E
                {
                    internal readonly int C;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064748")]
    public Task TestGenerateReadonlyFieldInConditionalAccessExpression4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    var x = a?.B.[|C|];
                }

                public class E
                {
                }
            }
            """,
            """
            class C
            {
                public E B { get; private set; }

                void Main(C a)
                {
                    var x = a?.B.C;
                }

                public class E
                {
                    internal readonly object C;
                }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGenerateFieldInPropertyInitializers()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                public int MyProperty { get; } = [|y|];
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                private static int y;

                public int MyProperty { get; } = y;
            }
            """);

    [Fact]
    public Task TestGenerateReadonlyFieldInPropertyInitializers()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                public int MyProperty { get; } = [|y|];
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                private static readonly int y;

                public int MyProperty { get; } = y;
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGeneratePropertyInPropertyInitializers()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                public int MyProperty { get; } = [|y|];
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                public static int y { get; private set; }
                public int MyProperty { get; } = y;
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGenerateFieldInExpressionBodiedProperty()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public int Y => [|y|];
            }
            """,
            """
            class Program
            {
                private int y;

                public int Y => y;
            }
            """);

    [Fact]
    public Task TestGenerateReadonlyFieldInExpressionBodiedProperty()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public int Y => [|y|];
            }
            """,
            """
            class Program
            {
                private readonly int y;

                public int Y => y;
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGeneratePropertyInExpressionBodiedProperty()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public int Y => [|y|];
            }
            """,
            """
            class Program
            {
                public int Y => y;

                public int y { get; private set; }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGenerateFieldInExpressionBodiedOperator()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static C operator --(C p) => [|x|];
            }
            """,
            """
            class C
            {
                private static C x;

                public static C operator --(C p) => x;
            }
            """);

    [Fact]
    public Task TestGenerateReadOnlyFieldInExpressionBodiedOperator()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static C operator --(C p) => [|x|];
            }
            """,
            """
            class C
            {
                private static readonly C x;

                public static C operator --(C p) => x;
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGeneratePropertyInExpressionBodiedOperator()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static C operator --(C p) => [|x|];
            }
            """,
            """
            class C
            {
                public static C x { get; private set; }

                public static C operator --(C p) => x;
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGenerateFieldInExpressionBodiedMethod()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static C GetValue(C p) => [|x|];
            }
            """,
            """
            class C
            {
                private static C x;

                public static C GetValue(C p) => x;
            }
            """);

    [Fact]
    public Task TestGenerateReadOnlyFieldInExpressionBodiedMethod()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static C GetValue(C p) => [|x|];
            }
            """,
            """
            class C
            {
                private static readonly C x;

                public static C GetValue(C p) => x;
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGeneratePropertyInExpressionBodiedMethod()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static C GetValue(C p) => [|x|];
            }
            """,
            """
            class C
            {
                public static C x { get; private set; }

                public static C GetValue(C p) => x;
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27647")]
    public Task TestGeneratePropertyInExpressionBodiedAsyncTaskOfTMethod()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static async System.Threading.Tasks.Task<C> GetValue(C p) => [|x|];
            }
            """,
            """
            class C
            {
                public static C x { get; private set; }

                public static async System.Threading.Tasks.Task<C> GetValue(C p) => x;
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGenerateFieldInDictionaryInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { [[|key|]] = 0 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                private static string key;

                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { [key] = 0 };
                }
            }
            """);

    [Fact]
    public Task TestGeneratePropertyInDictionaryInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = 0, [[|One|]] = 1, ["Two"] = 2 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                public static string One { get; private set; }

                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = 0, [One] = 1, ["Two"] = 2 };
                }
            }
            """);

    [Fact]
    public Task TestGenerateFieldInDictionaryInitializer2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = [|i|] };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                private static int i;

                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = i };
                }
            }
            """);

    [Fact]
    public Task TestGenerateReadOnlyFieldInDictionaryInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { [[|key|]] = 0 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                private static readonly string key;

                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { [key] = 0 };
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateFieldInDictionaryInitializer3()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = 0, [[|One|]] = 1, ["Two"] = 2 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                private static string One;

                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = 0, [One] = 1, ["Two"] = 2 };
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateReadOnlyFieldInDictionaryInitializer2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = [|i|] };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                private static readonly int i;

                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = i };
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGeneratePropertyInDictionaryInitializer2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { [[|key|]] = 0 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                public static string key { get; private set; }

                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { [key] = 0 };
                }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGenerateReadOnlyFieldInDictionaryInitializer3()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = 0, [[|One|]] = 1, ["Two"] = 2 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                private static readonly string One;

                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = 0, [One] = 1, ["Two"] = 2 };
                }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGeneratePropertyInDictionaryInitializer3()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = [|i|] };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                public static int i { get; private set; }

                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = i };
                }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGenerateLocalInDictionaryInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { [[|key|]] = 0 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    string key = null;
                    var x = new Dictionary<string, int> { [key] = 0 };
                }
            }
            """,
            index: LocalIndex);

    [Fact]
    public Task TestGenerateLocalInDictionaryInitializer2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = 0, [[|One|]] = 1, ["Two"] = 2 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    string One = null;
                    var x = new Dictionary<string, int> { ["Zero"] = 0, [One] = 1, ["Two"] = 2 };
                }
            }
            """,
            index: LocalIndex);

    [Fact]
    public Task TestGenerateLocalInDictionaryInitializer3()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = new Dictionary<string, int> { ["Zero"] = [|i|] };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    int i = 0;
                    var x = new Dictionary<string, int> { ["Zero"] = i };
                }
            }
            """,
            index: LocalIndex);

    [Fact]
    public Task TestGenerateVariableFromLambda()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    [|goo|] = () => {
                        return 0;
                    };
                }
            }
            """,
            """
            using System;

            class Program
            {
                private static Func<int> goo;

                static void Main(string[] args)
                {
                    goo = () => {
                        return 0;
                    };
                }
            }
            """);

    [Fact]
    public Task TestGenerateVariableFromLambda2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    [|goo|] = () => {
                        return 0;
                    };
                }
            }
            """,
            """
            using System;

            class Program
            {
                public static Func<int> goo { get; private set; }

                static void Main(string[] args)
                {
                    goo = () => {
                        return 0;
                    };
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact]
    public Task TestGenerateVariableFromLambda3()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    [|goo|] = () => {
                        return 0;
                    };
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Func<int> goo = () =>
                    {
                        return 0;
                    };
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8010")]
    public Task TestGenerationFromStaticProperty_Field()
        => TestInRegularAndScriptAsync(
            """
            using System;

            public class Test
            {
                public static int Property1
                {
                    get
                    {
                        return [|_field|];
                    }
                }
            }
            """,
            """
            using System;

            public class Test
            {
                private static int _field;

                public static int Property1
                {
                    get
                    {
                        return _field;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8010")]
    public Task TestGenerationFromStaticProperty_ReadonlyField()
        => TestInRegularAndScriptAsync(
            """
            using System;

            public class Test
            {
                public static int Property1
                {
                    get
                    {
                        return [|_field|];
                    }
                }
            }
            """,
            """
            using System;

            public class Test
            {
                private static readonly int _field;

                public static int Property1
                {
                    get
                    {
                        return _field;
                    }
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8010")]
    public Task TestGenerationFromStaticProperty_Property()
        => TestInRegularAndScriptAsync(
            """
            using System;

            public class Test
            {
                public static int Property1
                {
                    get
                    {
                        return [|goo|];
                    }
                }
            }
            """,
            """
            using System;

            public class Test
            {
                public static int Property1
                {
                    get
                    {
                        return goo;
                    }
                }

                public static int goo { get; private set; }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8010")]
    public Task TestGenerationFromStaticProperty_Local()
        => TestInRegularAndScriptAsync(
            """
            using System;

            public class Test
            {
                public static int Property1
                {
                    get
                    {
                        return [|goo|];
                    }
                }
            }
            """,
            """
            using System;

            public class Test
            {
                public static int Property1
                {
                    get
                    {
                        int goo = 0;
                        return goo;
                    }
                }
            }
            """,
            index: LocalIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8358")]
    public Task TestSameNameAsInstanceVariableInContainingType()
        => TestInRegularAndScriptAsync(
            """
            class Outer
            {
                int _field;

                class Inner
                {
                    public Inner(int field)
                    {
                        [|_field|] = field;
                    }
                }
            }
            """,
            """
            class Outer
            {
                int _field;

                class Inner
                {
                    private int _field;

                    public Inner(int field)
                    {
                        _field = field;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8358")]
    public Task TestNotOnStaticWithExistingInstance1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int _field;

                void M()
                {
                    C.[|_field|] = 42;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8358")]
    public Task TestNotOnStaticWithExistingInstance2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int _field;

                static C()
                {
                    [|_field|] = 42;
                }
            }
            """);

    [Fact]
    public Task TupleRead()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method((int, string) i)
                {
                    Method([|tuple|]);
                }
            }
            """,
            """
            class Class
            {
                private (int, string) tuple;

                void Method((int, string) i)
                {
                    Method(tuple);
                }
            }
            """);

    [Fact]
    public Task TupleWithOneNameRead()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method((int a, string) i)
                {
                    Method([|tuple|]);
                }
            }
            """,
            """
            class Class
            {
                private (int a, string) tuple;

                void Method((int a, string) i)
                {
                    Method(tuple);
                }
            }
            """);

    [Fact]
    public Task TupleWrite()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|tuple|] = (1, "hello");
                }
            }
            """,
            """
            class Class
            {
                private (int, string) tuple;

                void Method()
                {
                    tuple = (1, "hello");
                }
            }
            """);

    [Fact]
    public Task TupleWithOneNameWrite()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|tuple|] = (a: 1, "hello");
                }
            }
            """,
            """
            class Class
            {
                private (int a, string) tuple;

                void Method()
                {
                    tuple = (a: 1, "hello");
                }
            }
            """);

    [Fact]
    public Task TupleRefReturnProperties()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                public void Goo()
                {
                    ref int i = ref this.[|Bar|];
                }
            }
            """,
            """
            using System;
            class C
            {
                public ref int Bar => throw new NotImplementedException();

                public void Goo()
                {
                    ref int i = ref this.Bar;
                }
            }
            """);

    [Fact]
    public Task TupleRefWithField()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class C
            {
                public void Goo()
                {
                    ref int i = ref this.[|bar|];
                }
            }
            """,
            """
            using System;
            class C
            {
                private int bar;

                public void Goo()
                {
                    ref int i = ref this.bar;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17621")]
    public Task TestWithMatchingTypeName1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            public class Goo
            {
                public Goo(String goo)
                {
                    [|String|] = goo;
                }
            }
            """,
            """
            using System;

            public class Goo
            {
                public Goo(String goo)
                {
                    String = goo;
                }

                public string String { get; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17621")]
    public Task TestWithMatchingTypeName2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            public class Goo
            {
                public Goo(String goo)
                {
                    [|String|] = goo;
                }
            }
            """,
            """
            using System;

            public class Goo
            {
                public Goo(String goo)
                {
                    String = goo;
                }

                public string String { get; private set; }
            }
            """, index: ReadonlyFieldIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18275")]
    public Task TestContextualKeyword1()
        => TestMissingInRegularAndScriptAsync(
            """
            namespace N
            {
                class nameof
                {
                }
            }

            class C
            {
                void M()
                {
                    [|nameof|]
                }
            }
            """);

    [Fact]
    public Task TestPreferReadOnlyIfAfterReadOnlyAssignment()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                private readonly int _goo;

                public Class()
                {
                    _goo = 0;
                    [|_bar|] = 1;
                }
            }
            """,
            """
            class Class
            {
                private readonly int _goo;
                private readonly int _bar;

                public Class()
                {
                    _goo = 0;
                    _bar = 1;
                }
            }
            """);

    [Fact]
    public Task TestPreferReadOnlyIfBeforeReadOnlyAssignment()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                private readonly int _goo;

                public Class()
                {
                    [|_bar|] = 1;
                    _goo = 0;
                }
            }
            """,
            """
            class Class
            {
                private readonly int _bar;
                private readonly int _goo;

                public Class()
                {
                    _bar = 1;
                    _goo = 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19239")]
    public Task TestGenerateReadOnlyPropertyInConstructor()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                public Class()
                {
                    [|Bar|] = 1;
                }
            }
            """,
            """
            class Class
            {
                public Class()
                {
                    Bar = 1;
                }

                public int Bar { get; }
            }
            """);

    [Fact]
    public Task TestPlaceFieldBasedOnSurroundingStatements()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                private int _goo;
                private int _quux;

                public Class()
                {
                    _goo = 0;
                    [|_bar|] = 1;
                    _quux = 2;
                }
            }
            """,
            """
            class Class
            {
                private int _goo;
                private int _bar;
                private int _quux;

                public Class()
                {
                    _goo = 0;
                    _bar = 1;
                    _quux = 2;
                }
            }
            """);

    [Fact]
    public Task TestPlaceFieldBasedOnSurroundingStatements2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                private int goo;
                private int quux;

                public Class()
                {
                    this.goo = 0;
                    this.[|bar|] = 1;
                    this.quux = 2;
                }
            }
            """,
            """
            class Class
            {
                private int goo;
                private int bar;
                private int quux;

                public Class()
                {
                    this.goo = 0;
                    this.bar = 1;
                    this.quux = 2;
                }
            }
            """);

    [Fact]
    public Task TestPlacePropertyBasedOnSurroundingStatements()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                public int Goo { get; }
                public int Quuz { get; }

                public Class()
                {
                    Goo = 0;
                    [|Bar|] = 1;
                    Quux = 2;
                }
            }
            """,
            """
            class Class
            {
                public int Goo { get; }
                public int Bar { get; }
                public int Quuz { get; }

                public Class()
                {
                    Goo = 0;
                    Bar = 1;
                    Quux = 2;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19575")]
    public Task TestNotOnGenericCodeParsedAsExpression()
        => TestMissingAsync("""
            class C
            {
                private void GetEvaluationRuleNames()
                {
                    [|IEnumerable|] < Int32 >
                    return ImmutableArray.CreateRange();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19575")]
    public Task TestOnNonGenericExpressionWithLessThan()
        => TestInRegularAndScriptAsync("""
            class C
            {
                private void GetEvaluationRuleNames()
                {
                    [|IEnumerable|] < Int32
                    return ImmutableArray.CreateRange();
                }
            }
            """,
            """
            class C
            {
                public int IEnumerable { get; private set; }

                private void GetEvaluationRuleNames()
                {
                    IEnumerable < Int32
                    return ImmutableArray.CreateRange();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18988")]
    public Task GroupNonReadonlyFieldsTogether()
        => TestInRegularAndScriptAsync("""
            class C
            {
                public bool isDisposed;

                public readonly int x;
                public readonly int m;

                public C()
                {
                    this.[|y|] = 0;
                }
            }
            """,
            """
            class C
            {
                public bool isDisposed;
                private int y;
                public readonly int x;
                public readonly int m;

                public C()
                {
                    this.y = 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18988")]
    public Task GroupReadonlyFieldsTogether()
        => TestInRegularAndScriptAsync("""
            class C
            {
                public readonly int x;
                public readonly int m;

                public bool isDisposed;

                public C()
                {
                    this.[|y|] = 0;
                }
            }
            """,
            """
            class C
            {
                public readonly int x;
                public readonly int m;
                private readonly int y;
                public bool isDisposed;

                public C()
                {
                    this.y = 0;
                }
            }
            """, index: ReadonlyFieldIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20791")]
    public Task TestWithOutOverload1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    Goo(out [|goo|]);
                }

                void Goo(int i) { }
                void Goo(out bool b) { }
            }
            """,
            """
            class Class
            {
                private bool goo;

                void Method()
                {
                    Goo(out goo);
                }

                void Goo(int i) { }
                void Goo(out bool b) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20791")]
    public Task TestWithOutOverload2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    Goo([|goo|]);
                }

                void Goo(out bool b) { }
                void Goo(int i) { }
            }
            """,
            """
            class Class
            {
                private int goo;

                void Method()
                {
                    Goo(goo);
                }

                void Goo(out bool b) { }
                void Goo(int i) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20791")]
    public Task TestWithRefOverload1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    Goo(ref [|goo|]);
                }

                void Goo(int i) { }
                void Goo(ref bool b) { }
            }
            """,
            """
            class Class
            {
                private bool goo;

                void Method()
                {
                    Goo(ref goo);
                }

                void Goo(int i) { }
                void Goo(ref bool b) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20791")]
    public Task TestWithRefOverload2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    Goo([|goo|]);
                }

                void Goo(ref bool b) { }
                void Goo(int i) { }
            }
            """,
            """
            class Class
            {
                private int goo;

                void Method()
                {
                    Goo(goo);
                }

                void Goo(ref bool b) { }
                void Goo(int i) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateFieldInExpressionBodiedGetter()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public int Property
                {
                    get => [|_field|];
                }
            }
            """,
            """
            class Program
            {
                private int _field;

                public int Property
                {
                    get => _field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateFieldInExpressionBodiedGetterWithDifferentAccessibility()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public int Property
                {
                    protected get => [|_field|];
                    set => throw new System.NotImplementedException();
                }
            }
            """,
            """
            class Program
            {
                private int _field;

                public int Property
                {
                    protected get => _field;
                    set => throw new System.NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateReadonlyFieldInExpressionBodiedGetter()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public int Property
                {
                    get => [|_readonlyField|];
                }
            }
            """,
            """
            class Program
            {
                private readonly int _readonlyField;

                public int Property
                {
                    get => _readonlyField;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGeneratePropertyInExpressionBodiedGetter()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public int Property
                {
                    get => [|prop|];
                }
            }
            """,
            """
            class Program
            {
                public int Property
                {
                    get => prop;
                }
                public int prop { get; private set; }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateFieldInExpressionBodiedSetterInferredFromType()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public int Property
                {
                    set => [|_field|] = value;
                }
            }
            """,
            """
            class Program
            {
                private int _field;

                public int Property
                {
                    set => _field = value;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateFieldInExpressionBodiedLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public void Method()
                {
                    int Local() => [|_field|];
                }
            }
            """,
            """
            class Program
            {
                private int _field;

                public void Method()
                {
                    int Local() => _field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateReadonlyFieldInExpressionBodiedLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public void Method()
                {
                    int Local() => [|_readonlyField|];
                }
            }
            """,
            """
            class Program
            {
                private readonly int _readonlyField;

                public void Method()
                {
                    int Local() => _readonlyField;
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGeneratePropertyInExpressionBodiedLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public void Method()
                {
                    int Local() => [|prop|];
                }
            }
            """,
            """
            class Program
            {
                public int prop { get; private set; }

                public void Method()
                {
                    int Local() => prop;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27647")]
    public Task TestGeneratePropertyInExpressionBodiedAsyncTaskOfTLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public void Method()
                {
                    async System.Threading.Tasks.Task<int> Local() => [|prop|];
                }
            }
            """,
            """
            class Program
            {
                public int prop { get; private set; }

                public void Method()
                {
                    async System.Threading.Tasks.Task<int> Local() => prop;
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateFieldInExpressionBodiedLocalFunctionInferredFromType()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public void Method()
                {
                    int Local() => [|_field|] = 12;
                }
            }
            """,
            """
            class Program
            {
                private int _field;

                public void Method()
                {
                    int Local() => _field = 12;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateFieldInBlockBodiedLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public void Method()
                {
                    int Local()
                    {
                        return [|_field|];
                    }
                }
            }
            """,
            """
            class Program
            {
                private int _field;

                public void Method()
                {
                    int Local()
                    {
                        return _field;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateReadonlyFieldInBlockBodiedLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public void Method()
                {
                    int Local()
                    {
                        return [|_readonlyField|];
                    }
                }
            }
            """,
            """
            class Program
            {
                private readonly int _readonlyField;

                public void Method()
                {
                    int Local()
                    {
                        return _readonlyField;
                    }
                }
            }
            """,
            index: ReadonlyFieldIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGeneratePropertyInBlockBodiedLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public void Method()
                {
                    int Local()
                    {
                        return [|prop|];
                    }
                }
            }
            """,
            """
            class Program
            {
                public int prop { get; private set; }

                public void Method()
                {
                    int Local()
                    {
                        return prop;
                    }
                }
            }
            """,
            index: PropertyIndex);

    [Fact]
    public Task TestGeneratePropertyInBlockBodiedAsyncTaskOfTLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public void Method()
                {
                    async System.Threading.Tasks.Task<int> Local()
                    {
                        return [|prop|];
                    }
                }
            }
            """,
            """
            class Program
            {
                public int prop { get; private set; }

                public void Method()
                {
                    async System.Threading.Tasks.Task<int> Local()
                    {
                        return prop;
                    }
                }
            }
            """,
            index: PropertyIndex);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateFieldInBlockBodiedLocalFunctionInferredFromType()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public void Method()
                {
                    int Local() 
                    {
                        return [|_field|] = 12;
                    }
                }
            }
            """,
            """
            class Program
            {
                private int _field;

                public void Method()
                {
                    int Local() 
                    {
                        return _field = 12;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateFieldInBlockBodiedLocalFunctionInsideLambdaExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                public void Method()
                {
                    Action action = () => 
                    {
                        int Local()
                        {
                            return [|_field|];
                        }
                    };
                }
            }
            """,
            """
            using System;

            class Program
            {
                private int _field;

                public void Method()
                {
                    Action action = () => 
                    {
                        int Local()
                        {
                            return _field;
                        }
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26993")]
    public Task TestGenerateFieldInExpressionBodiedLocalFunctionInsideLambdaExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                public void Method()
                {
                    Action action = () => 
                    {
                        int Local() => [|_field|];
                    };
                }
            }
            """,
            """
            using System;

            class Program
            {
                private int _field;

                public void Method()
                {
                    Action action = () => 
                    {
                        int Local() => _field;
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26406")]
    public Task TestIdentifierInsideLock1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    lock ([|goo|])
                    {
                    }
                }
            }
            """,
            """
            class Class
            {
                private object goo;

                void Method()
                {
                    lock (goo)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26406")]
    public Task TestIdentifierInsideLock2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    lock ([|goo|])
                    {
                    }
                }
            }
            """,
            """
            class Class
            {
                private readonly object goo;

                void Method()
                {
                    lock (goo)
                    {
                    }
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26406")]
    public Task TestIdentifierInsideLock3()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    lock ([|goo|])
                    {
                    }
                }
            }
            """,
            """
            class Class
            {
                public object goo { get; private set; }

                void Method()
                {
                    lock (goo)
                    {
                    }
                }
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPattern1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { [|X|]: int i })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { X: int i })
                    {
                    }
                }

                class Blah
                {
                    public int X { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPattern2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    Blah o = null;
                    if (o is { [|X|]: int i })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    Blah o = null;
                    if (o is { X: int i })
                    {
                    }
                }

                class Blah
                {
                    public int X { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPattern3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { X: { [|Y|]: int i } })
                    {
                    }
                }

                class Frob
                {
                }

                class Blah
                {
                    public Frob X;
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { X: { Y: int i } })
                    {
                    }
                }

                class Frob
                {
                    public int Y { get; internal set; }
                }

                class Blah
                {
                    public Frob X;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPattern4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { [|X|]: })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { X: })
                    {
                    }
                }

                class Blah
                {
                    public object X { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPattern5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { [|X|]: Frob { } })
                    {
                    }
                }

                class Blah
                {
                }

                class Frob
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { X: Frob { } })
                    {
                    }
                }

                class Blah
                {
                    public Frob X { get; internal set; }
                }

                class Frob
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPattern6()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { [|X|]: (1, 2) })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { X: (1, 2) })
                    {
                    }
                }

                class Blah
                {
                    public (int, int) X { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPattern7()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { [|X|]: (y: 1, z: 2) })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { X: (y: 1, z: 2) })
                    {
                    }
                }

                class Blah
                {
                    public (int y, int z) X { get; internal set; }
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPattern8()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { [|X|]: () })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { X: () })
                    {
                    }
                }

                class Blah
                {
                    public object X { get; internal set; }
                }
            }
            """);

    [Fact]
    public Task TestExtendedPropertyPatternInIsPattern()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                Blah SomeBlah { get; set; }

                void M2()
                {
                    object o = null;
                    if (o is C { SomeBlah.[|X|]: (y: 1, z: 2) })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class C
            {
                Blah SomeBlah { get; set; }

                void M2()
                {
                    object o = null;
                    if (o is C { SomeBlah.X: (y: 1, z: 2) })
                    {
                    }
                }

                class Blah
                {
                    public (int y, int z) X { get; internal set; }
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12)));

    [Fact]
    public Task TestConstantPatternInPropertyPattern()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                Blah SomeBlah { get; set; }

                void M2()
                {
                    object o = null;
                    if (o is C { SomeBlah: [|MissingConstant|] })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                private const Blah MissingConstant;

                Blah SomeBlah { get; set; }

                void M2()
                {
                    object o = null;
                    if (o is C { SomeBlah: MissingConstant })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12)));

    [Fact]
    public Task TestConstantPatternInExtendedPropertyPattern()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                C SomeC { get; set; }
                Blah SomeBlah { get; set; }

                void M2()
                {
                    object o = null;
                    if (o is C { SomeC.SomeBlah: [|MissingConstant|] })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                private const Blah MissingConstant;

                C SomeC { get; set; }
                Blah SomeBlah { get; set; }

                void M2()
                {
                    object o = null;
                    if (o is C { SomeC.SomeBlah: MissingConstant })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPattern9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { [|X|]: (1) })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { X: (1) })
                    {
                    }
                }

                class Blah
                {
                    public int X { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPattern10()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { [|X|]: (y: 1) })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    if (o is Blah { X: (y: 1) })
                    {
                    }
                }

                class Blah
                {
                    public object X { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsPatternWithNullablePattern()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                void M2()
                {
                    object? o = null;
                    object? zToMatch = null;
                    if (o is Blah { [|X|]: (y: 1, z: zToMatch) })
                    {
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            #nullable enable

            class C
            {
                void M2()
                {
                    object? o = null;
                    object? zToMatch = null;
                    if (o is Blah { X: (y: 1, z: zToMatch) })
                    {
                    }
                }

                class Blah
                {
                    public (int y, object? z) X { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInCasePattern1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    switch (o)
                    {
                        case Blah { [|X|]: int i }:
                            break;
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    switch (o)
                    {
                        case Blah { X: int i }:
                            break;
                    }
                }

                class Blah
                {
                    public int X { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInCasePattern2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    Blah o = null;
                    switch (o)
                    {
                        case { [|X|]: int i }:
                            break;
                    }
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    Blah o = null;
                    switch (o)
                    {
                        case { X: int i }:
                            break;
                    }
                }

                class Blah
                {
                    public int X { get; internal set; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9090")]
    public Task TestPropertyPatternInIsSwitchExpression1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    _ = o switch { Blah { [|X|]: int i } => 0, _ => 0 };
                }

                class Blah
                {
                }
            }
            """,
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    _ = o switch { Blah { X: int i } => 0, _ => 0 };
                }

                class Blah
                {
                    public int X { get; internal set; }
                }
            }
            """);

    [Fact]
    public Task TestPropertyPatternGenerateConstant()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M2()
                {
                    object o = null;
                    _ = o switch { Blah { X: [|Y|] } => 0, _ => 0 };
                }

                class Blah
                {
                    public int X;
                }
            }
            """,
            """
            class C
            {
                private const int Y;

                void M2()
                {
                    object o = null;
                    _ = o switch { Blah { X: Y } => 0, _ => 0 };
                }

                class Blah
                {
                    public int X;
                }
            }
            """);

    [Fact]
    public Task TestAddParameter()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    [|goo|];
                }
            }
            """,
            """
            class Class
            {
                void Method(object goo)
                {
                    goo;
                }
            }
            """, index: Parameter);

    [Fact]
    public Task TestAddParameter_DoesntAddToInterface()
        => TestInRegularAndScriptAsync(
            """
            interface Interface
            {
                void Method();
            }

            class Class
            {
                public void Method()
                {
                    [|goo|];
                }
            }
            """,
            """
            interface Interface
            {
                void Method();
            }

            class Class
            {
                public void Method(object goo)
                {
                    [|goo|];
                }
            }
            """, index: Parameter);

    [Fact]
    public Task TestAddParameterAndOverrides_AddsToInterface()
        => TestInRegularAndScriptAsync(
            """
            interface Interface
            {
                void Method();
            }

            class Class : Interface
            {
                public void Method()
                {
                    [|goo|];
                }
            }
            """,
            """
            interface Interface
            {
                void Method(object goo);
            }

            class Class : Interface
            {
                public void Method(object goo)
                {
                    [|goo|];
                }
            }
            """, index: ParameterAndOverrides);

    [Fact]
    public Task TestAddParameterIsOfCorrectType()
        => TestInRegularAndScriptAsync(
"""
class Class
{
    void Method()
    {
        M1([|goo|]);
    }

    void M1(int a);
}
""",
"""
class Class
{
    void Method(int goo)
    {
        M1(goo);
    }

    void M1(int a);
}
""", index: Parameter);

    [Fact]
    public Task TestAddParameterAndOverrides_IsOfCorrectType()
        => TestInRegularAndScriptAsync(
            """
            interface Interface
            {
                void Method();
            }

            class Class : Interface
            {
                public void Method()
                {
                    M1([|goo|]);
                }

                void M1(int a);
            }
            """,
            """
            interface Interface
            {
                void Method(int goo);
            }

            class Class : Interface
            {
                public void Method(int goo)
                {
                    M1(goo);
                }

                void M1(int a);
            }
            """, index: ParameterAndOverrides);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26502")]
    public Task TestNoReadOnlyMembersWhenInLambdaInConstructor()
        => TestExactActionSetOfferedAsync(
            """
            using System;

            class C
            {
                public C()
                {
                    Action a = () =>
                    {
                        this.[|Field|] = 1;
                    };
                }
            }
            """,
            [
                string.Format(CodeFixesResources.Generate_property_0, "Field"),
                string.Format(CodeFixesResources.Generate_field_0, "Field"),
            ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26502")]
    public Task TestNoReadOnlyMembersWhenInLocalFunctionInConstructor()
        => TestExactActionSetOfferedAsync(
            """
            using System;

            class C
            {
                public C()
                {
                    void Goo()
                    {
                        this.[|Field|] = 1;
                    };
                }
            }
            """,
            [
                string.Format(CodeFixesResources.Generate_property_0, "Field"),
                string.Format(CodeFixesResources.Generate_field_0, "Field"),
            ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45367")]
    public Task DoNotOfferPropertyOrFieldInNamespace()
        => TestExactActionSetOfferedAsync(
            """
            using System;

            namespace ConsoleApp5
            {
                class MyException: Exception

                internal MyException(int error, int offset, string message) : base(message)
                {
                    [|Error|] = error;
                    Offset = offset;
                }
            """,
            [
                string.Format(CodeFixesResources.Generate_local_0, "Error", "MyException"),
                string.Format(CodeFixesResources.Generate_parameter_0, "Error", "MyException"),
            ]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48172")]
    public Task TestMissingOfferParameterInTopLevel()
        => TestMissingAsync("[|Console|].WriteLine();", new TestParameters(Options.Regular));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47586")]
    public Task TestGenerateParameterFromLambda()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Diagnostics;

            class Class
            {
                private static void AssertSomething()
                {
                    Action<int> call = _ => Debug.Assert([|expected|]);
                }
            }
            """,
            """
            using System;
            using System.Diagnostics;

            class Class
            {
                private static void AssertSomething(bool expected)
                {
                    Action<int> call = _ => Debug.Assert(expected);
                }
            }
            """, index: Parameter);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47586")]
    public Task TestGenerateParameterFromLambdaInLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Diagnostics;

            class Class
            {
                private static void AssertSomething()
                {
                    void M()
                    {
                        Action<int> call = _ => Debug.Assert([|expected|]);
                    }
                }
            }
            """,
            """
            using System;
            using System.Diagnostics;

            class Class
            {
                private static void AssertSomething()
                {
                    void M(bool expected)
                    {
                        Action<int> call = _ => Debug.Assert(expected);
                    }
                }
            }
            """, index: Parameter);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/27646")]
    [InlineData("yield")]
    [InlineData("partial")]
    [InlineData("group")]
    [InlineData("join")]
    [InlineData("into")]
    [InlineData("let")]
    [InlineData("by")]
    [InlineData("where")]
    [InlineData("select")]
    [InlineData("get")]
    [InlineData("set")]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("orderby")]
    [InlineData("alias")]
    [InlineData("on")]
    [InlineData("equals")]
    [InlineData("ascending")]
    [InlineData("descending")]
    [InlineData("assembly")]
    [InlineData("module")]
    [InlineData("type")]
    [InlineData("global")]
    [InlineData("field")]
    [InlineData("method")]
    [InlineData("param")]
    [InlineData("property")]
    [InlineData("typevar")]
    [InlineData("when")]
    [InlineData("_")]
    [InlineData("or")]
    [InlineData("and")]
    [InlineData("not")]
    [InlineData("with")]
    [InlineData("init")]
    [InlineData("record")]
    [InlineData("managed")]
    [InlineData("unmanaged")]
    [InlineData("dynamic")]
    public Task TestContextualKeywordsThatDoNotProbablyStartSyntacticConstructs_ReturnStatement(string keyword)
        => TestInRegularAndScriptAsync(
            $$"""
            class C
            {
                int M()
                {
                    [|return {{keyword}}|];
                }
            }
            """,
            $$"""
            class C
            {
                private int {{keyword}};

                int M()
                {
                    return {{keyword}};
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/27646")]
    [InlineData("from")]
    [InlineData("nameof")]
    [InlineData("async")]
    [InlineData("await")]
    [InlineData("var")]
    public Task TestContextualKeywordsThatCanProbablyStartSyntacticConstructs_ReturnStatement(string keyword)
        => TestMissingInRegularAndScriptAsync(
            $$"""
            class C
            {
                int M()
                {
                    [|return {{keyword}}|];
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/27646")]
    [InlineData("from")]
    [InlineData("nameof")]
    [InlineData("async")]
    [InlineData("await")]
    [InlineData("var")]
    public Task TestContextualKeywordsThatCanProbablyStartSyntacticConstructs_OnTheirOwn(string keyword)
        => TestMissingInRegularAndScriptAsync(
            $$"""
            class C
            {
                int M()
                {
                    [|{{keyword}}|]
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/27646")]
    [InlineData("from")]
    [InlineData("nameof")]
    [InlineData("async")]
    [InlineData("await")]
    [InlineData("var")]
    public Task TestContextualKeywordsThatCanProbablyStartSyntacticConstructs_Local(string keyword)
        => TestMissingInRegularAndScriptAsync(
            $$"""
            class Program
            {
                void Main()
                {
                    var x = [|{{keyword}}|];
                }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60842")]
    public Task TestGenerateParameterBeforeCancellationToken_OneParameter()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(CancellationToken cancellationToken)
                {
                    await Task.Delay([|time|]);
                }
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(System.TimeSpan time, CancellationToken cancellationToken)
                {
                    await Task.Delay(time);
                }
            }
            """, index: 4);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60842")]
    public Task TestGenerateParameterBeforeCancellationToken_SeveralParameters()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(string someParameter, CancellationToken cancellationToken)
                {
                    await Task.Delay([|time|]);
                }
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(string someParameter, System.TimeSpan time, CancellationToken cancellationToken)
                {
                    await Task.Delay(time);
                }
            }
            """, index: 4);

    [Fact]
    public Task TestGenerateParameterBeforeCancellationTokenAndOptionalParameter()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(bool someParameter = true, CancellationToken cancellationToken)
                {
                    await Task.Delay([|time|]);
                }
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(System.TimeSpan time, bool someParameter = true, CancellationToken cancellationToken)
                {
                    await Task.Delay(time);
                }
            }
            """, index: 4);

    [Fact]
    public Task TestGenerateParameterBeforeCancellationTokenAndOptionalParameter_MultipleParameters()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(int value, bool someParameter = true, CancellationToken cancellationToken)
                {
                    await Task.Delay([|time|]);
                }
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(int value, System.TimeSpan time, bool someParameter = true, CancellationToken cancellationToken)
                {
                    await Task.Delay(time);
                }
            }
            """, index: 4);

    [Fact]
    public Task TestGenerateParameterBeforeOptionalParameter()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(bool someParameter = true)
                {
                    await Task.Delay([|time|]);
                }
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(System.TimeSpan time, bool someParameter = true)
                {
                    await Task.Delay(time);
                }
            }
            """, index: 4);

    [Fact]
    public Task TestGenerateParameterBeforeParamsParameter()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(params double[] x)
                {
                    await Task.Delay([|time|]);
                }
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(System.TimeSpan time, params double[] x)
                {
                    await Task.Delay(time);
                }
            }
            """, index: 4);

    [Fact]
    public Task TestGenerateParameterBeforeThisParameter()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading;
            using System.Threading.Tasks;

            public static class TestClass
            {
                public static int Method(this CancellationToken cancellationToken)
                {
                    return [|test|];
                }
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            public static class TestClass
            {
                public static int Method(this CancellationToken cancellationToken, int test)
                {
                    return test;
                }
            }
            """, index: 4);

    [Fact]
    public Task TestGenerateParameterBeforeAssortmentOfExceptions()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading;
            using System.Threading.Tasks;

            public static class TestClass
            {
                public static int Method(this CancellationToken cancellationToken, out int x, params bool[] z)
                {
                    return [|test|];
                }
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            public static class TestClass
            {
                public static int Method(this CancellationToken cancellationToken, int test, out int x, params bool[] z)
                {
                    return test;
                }
            }
            """, index: 4);

    [Fact]
    public Task TestGenerateParameterBeforeMultipleExceptions_BetweenOutParams()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(out int x, int y, out int z, params double[] x)
                {
                    await Task.Delay([|time|]);
                }
            }
            """,
            """
            using System.Threading;
            using System.Threading.Tasks;

            class C
            {
                public async Task M(out int x, int y, System.TimeSpan time, out int z, params double[] x)
                {
                    await Task.Delay(time);
                }
            }
            """, index: 4);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50764")]
    public Task TestMissingWhenGeneratingFunctionPointer()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            public unsafe class Bar
            {
                public static ZZZ()
                {
                     delegate*<void> i = &[|Goo|];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68322")]
    public Task TestImplicitObjectCreationExpression()
        => TestInRegularAndScriptAsync(
            """
            class Example
            {
                public Example(int argument) { }

                void M()
                {
                    Example e = new([|_field|]);
                }
            }
            """,
            """
            class Example
            {
                private int _field;

                public Example(int argument) { }

                void M()
                {
                    Example e = new(_field);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68322")]
    public Task TestImplicitCollectionCreationExpression()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Example
            {
                void M()
                {
                    List<int> list = new() { [|_field|] };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Example
            {
                private int _field;

                void M()
                {
                    List<int> list = new() { [|_field|] };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58491")]
    public Task TestGeneratePropertiesFromTopLevel()
        => TestInRegularAndScriptAsync(
            """
            var x = new Test() { [|A|] = 1, B = 1 };
            class Test
            {
            }
            """,
            """
            var x = new Test() { A = 1, B = 1 };
            class Test
            {
                public int A { get; internal set; }
            }
            """);

    [Fact]
    public Task TestNullConditionalAssignment1()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(Class c)
                {
                    c?.[|goo|] = 1;
                }
            }
            """,
            """
            class Class
            {
                private int goo;

                void Method(Class c)
                {
                    c?.goo = 1;
                }
            }
            """);

    [Fact]
    public Task TestNullConditionalAssignment2()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(Class c)
                {
                    c?.[|Goo|] = 1;
                }
            }
            """,
            """
            class Class
            {
                public int Goo { get; private set; }

                void Method(Class c)
                {
                    c?.Goo = 1;
                }
            }
            """);

    [Fact]
    public Task TestNullConditionalAssignment3()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(D c)
                {
                    c?.[|Goo|] = 1;
                }
            }

            class D
            {
            }
            """,
            """
            class Class
            {
                void Method(D c)
                {
                    c?.Goo = 1;
                }
            }
            
            class D
            {
                public int Goo { get; internal set; }
            }
            """);

    [Fact]
    public Task TestNullConditionalAssignment4()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method(D? c)
                {
                    c?.[|Goo|] = 1;
                }
            }

            struct D
            {
            }
            """,
            """
            class Class
            {
                void Method(D? c)
                {
                    c?.Goo = 1;
                }
            }
            
            struct D
            {
                public int Goo { get; internal set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81071")]
    public Task TestNotOfferedInEventAddAccessor()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                event EventHandler E
                {
                    add { [|ev|] += value; }
                    remove { ev -= value; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81071")]
    public Task TestNotOfferedInEventRemoveAccessor()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                event EventHandler E
                {
                    add { ev += value; }
                    remove { [|ev|] -= value; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81071")]
    public Task TestNotOfferedInPropertyGetAccessor()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int P
                {
                    get { return [|x|]; }
                    set { }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81071")]
    public Task TestNotOfferedInPropertySetAccessor()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int P
                {
                    get { return 0; }
                    set { [|x|] = value; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81071")]
    public Task TestNotOfferedInIndexerGetAccessor()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int this[int index]
                {
                    get { return [|x|]; }
                    set { }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81071")]
    public Task TestNotOfferedInIndexerSetAccessor()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                int this[int index]
                {
                    get { return 0; }
                    set { [|x|] = value; }
                }
            }
            """);
}
