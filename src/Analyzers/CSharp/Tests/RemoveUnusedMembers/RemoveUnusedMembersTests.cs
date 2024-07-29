// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedMembers;

using static Microsoft.CodeAnalysis.CSharp.UsePatternCombinators.AnalyzedPattern;
using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnusedMembersDiagnosticAnalyzer,
    CSharpRemoveUnusedMembersCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
public class RemoveUnusedMembersTests
{
    [Theory, CombinatorialData]
    public void TestStandardProperty(AnalyzerProperty property)
        => VerifyCS.VerifyStandardProperty(property);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31582")]
    public async Task FieldReadViaSuppression()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                #nullable enable
                class MyClass
                {
                    string? _field = null;
                    public void M()
                    {
                        _field!.ToString();
                    }
                }
                """
        }.RunAsync();
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task NonPrivateField(string accessibility)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class MyClass
                {
                    {{accessibility}} int _goo;
                }
                """,
        }.RunAsync();

    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task NonPrivateFieldWithConstantInitializer(string accessibility)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class MyClass
                {
                    {{accessibility}} int _goo = 0;
                }
                """,
        }.RunAsync();
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task NonPrivateFieldWithNonConstantInitializer(string accessibility)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class MyClass
                {
                    {{accessibility}} int _goo = _goo2;
                    private static readonly int _goo2 = 0;
                }
                """,
        }.RunAsync();
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task NonPrivateMethod(string accessibility)
    {
        var code = $$"""
            class MyClass
            {
                {{accessibility}} void M() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task NonPrivateProperty(string accessibility)
    {
        var code = $$"""
            class MyClass
            {
                {{accessibility}} int P { get; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task NonPrivateIndexer(string accessibility)
    {
        var code = $$"""
            class MyClass
            {
                {{accessibility}}
                int this[int arg] { get { return 0; } set { } }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task NonPrivateEvent(string accessibility)
    {
        var code = $$"""
            using System;

            class MyClass
            {
                {{accessibility}} event EventHandler RaiseCustomEvent;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsUnused()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|_goo|];
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task MethodIsUnused()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|M|]() => 0;
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task GenericMethodIsUnused()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|M|]<T>() => 0;
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task MethodInGenericTypeIsUnused()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass<T>
            {
                private int [|M|]() => 0;
            }
            """,
            """
            class MyClass<T>
            {
            }
            """);
    }

    [Fact]
    public async Task InstanceConstructorIsUnused_NoArguments()
    {
        // We only flag constructors with arguments.
        var code = """
            class MyClass
            {
                private MyClass() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task InstanceConstructorIsUnused_WithArguments()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private [|MyClass|](int i) { }
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task StaticConstructorIsNotFlagged()
    {
        var code = """
            class MyClass
            {
                static MyClass() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task DestructorIsNotFlagged()
    {
        var code = """
            class MyClass
            {
                ~MyClass() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task PropertyIsUnused()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|P|] { get; set; }
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task IndexerIsUnused()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|this|][int x] { get { return 0; } set { } }
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task EventIsUnused()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private event System.EventHandler [|e|];
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task EntryPointMethodNotFlagged()
    {
        var code = """
            class MyClass
            {
                private static void Main() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task EntryPointMethodNotFlagged_02()
    {
        var code = """
            using System.Threading.Tasks;

            class MyClass
            {
                private static async Task Main() => await Task.CompletedTask;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task EntryPointMethodNotFlagged_03()
    {
        var code = """
            using System.Threading.Tasks;

            class MyClass
            {
                private static async Task<int> Main() => await Task.FromResult(0);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task EntryPointMethodNotFlagged_04()
    {
        var code = """
            using System.Threading.Tasks;

            class MyClass
            {
                private static Task Main() => Task.CompletedTask;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31572")]
    public async Task EntryPointMethodNotFlagged_05()
    {
        var code = """
            using System.Threading.Tasks;

            class MyClass
            {
                private static int Main() => 0;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task EntryPointMethodNotFlagged_06()
    {
        var code = """
            return 0;
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(2,1): error CS8805: Program using top-level statements must be an executable.
                DiagnosticResult.CompilerError("CS8805").WithSpan(1, 1, 1, 10),
            },
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task EntryPointMethodNotFlagged_07()
    {
        var code = """
            return 0;
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code, code },
            },
            FixedState =
            {
                Sources = { code, code },
            },
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(2,1): error CS8805: Program using top-level statements must be an executable.
                DiagnosticResult.CompilerError("CS8805").WithSpan(1, 1, 1, 10),
                // /0/Test1.cs(2,1): error CS8802: Only one compilation unit can have top-level statements.
                DiagnosticResult.CompilerError("CS8802").WithSpan("/0/Test1.cs", 1, 1, 1, 7),
            },
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task EntryPointMethodNotFlagged_08()
    {
        var code = """
            return 0;
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                OutputKind = OutputKind.ConsoleApplication,
            },
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task EntryPointMethodNotFlagged_09()
    {
        var code = """
            return 0;
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code, code },
                OutputKind = OutputKind.ConsoleApplication,
            },
            FixedState =
            {
                Sources = { code, code },
            },
            ExpectedDiagnostics =
            {
                // /0/Test1.cs(2,1): error CS8802: Only one compilation unit can have top-level statements.
                DiagnosticResult.CompilerError("CS8802").WithSpan("/0/Test1.cs", 1, 1, 1, 7),
            },
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task FieldIsUnused_ReadOnly()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private readonly int [|_goo|];
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task PropertyIsUnused_ReadOnly()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|P|] { get; }
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task EventIsUnused_ReadOnly()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                // error CS0106: The modifier 'readonly' is not valid for this item
                private readonly event System.EventHandler {|CS0106:[|E|]|};
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task FieldIsUnused_Static()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private static int [|_goo|];
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task MethodIsUnused_Static()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private static void [|M|]() { }
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task PropertyIsUnused_Static()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private static int [|P|] { get { return 0; } }
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task IndexerIsUnused_Static()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                // error CS0106: The modifier 'static' is not valid for this item
                private static int {|CS0106:[|this|]|}[int x] { get { return 0; } set { } }
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task EventIsUnused_Static()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private static event System.EventHandler [|e1|];
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task MethodIsUnused_Extern()
    {
        var code = """
            using System.Runtime.InteropServices;

            class C
            {
                [DllImport("Assembly.dll")]
                private static extern void M();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MethodIsUnused_Abstract()
    {
        var code = """
            abstract class C
            {
                protected abstract void M();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MethodIsUnused_InterfaceMethod()
    {
        var code = """
            interface I
            {
                void M();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MethodIsUnused_ExplicitInterfaceImplementation()
    {
        var code = """
            interface I
            {
                void M();
            }

            class C : I
            {
                void I.M() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task PropertyIsUnused_ExplicitInterfaceImplementation()
    {
        var code = """
            interface I
            {
                int P { get; set; }
            }

            class C : I
            {
                int I.P { get { return 0; } set { } }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30965")]
    public async Task EventIsUnused_ExplicitInterfaceImplementation()
    {
        var code = """
            interface I
            {
                event System.Action E;
            }

            class C : I
            {
                event System.Action I.E
                {
                    add { }
                    remove { }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30894")]
    public async Task WriteOnlyProperty_NotWritten()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                int [|P|] { set { } }
            }
            """,
            """
            class C
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30894")]
    public async Task WriteOnlyProperty_Written()
    {
        var code = """
            class C
            {
                int P { set { } }
                public void M(int i) => P = i;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsUnused_Const()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private const int [|_goo|] = 0;
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task FieldIsRead_ExpressionBody()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M() => _goo;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_BlockBody()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M() { return _goo; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_ExpressionLambda()
    {
        var code = """
            using System;
            class MyClass
            {
                private int _goo;
                public void M()
                {
                    Func<int> getGoo = () => _goo;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_BlockLambda()
    {
        var code = """
            using System;
            class MyClass
            {
                private int _goo;
                public void M()
                {
                    Func<int> getGoo = () => { return _goo; };
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_Delegate()
    {
        var code = """
            using System;
            class MyClass
            {
                private int _goo;
                public void M()
                {
                    Func<int> getGoo = delegate { return _goo; };
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_ExpressionBodyLocalFunction()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M()
                {
                    int LocalFunction() => _goo;
                    return LocalFunction();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_BlockBodyLocalFunction()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M()
                {
                    int LocalFunction() { return _goo; }
                    return LocalFunction();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_Accessor()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int Goo
                {
                    get
                    {
                        return _goo;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_Deconstruction()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public void M(int x)
                {
                    var y = (_goo, x);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_DifferentInstance()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M() => new MyClass()._goo;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_ObjectInitializer()
    {
        var code = """
            class C
            {
                public int F;
            }
            class MyClass
            {
                private int _goo;
                public C M() => new C() { F = _goo };
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_ThisInstance()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M() => this._goo;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_Attribute()
    {
        var code = """
            class MyClass
            {
                private const string _goo = "";

                [System.Obsolete(_goo)]
                public void M() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MethodIsInvoked()
    {
        var code = """
            class MyClass
            {
                private int M1() => 0;
                public int M2() => M1();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MethodIsAddressTaken()
    {
        var code = """
            class MyClass
            {
                private int M1() => 0;
                public void M2()
                {
                    System.Func<int> m1 = M1;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task GenericMethodIsInvoked_ExplicitTypeArguments()
    {
        var code = """
            class MyClass
            {
                private int M1<T>() => 0;
                public int M2() => M1<int>();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task GenericMethodIsInvoked_ImplicitTypeArguments()
    {
        var code = """
            class MyClass
            {
                private T M1<T>(T t) => t;
                public int M2() => M1(0);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MethodInGenericTypeIsInvoked_NoTypeArguments()
    {
        var code = """
            class MyClass<T>
            {
                private int M1() => 0;
                public int M2() => M1();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MethodInGenericTypeIsInvoked_NonConstructedType()
    {
        var code = """
            class MyClass<T>
            {
                private int M1() => 0;
                public int M2(MyClass<T> m) => m.M1();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MethodInGenericTypeIsInvoked_ConstructedType()
    {
        var code = """
            class MyClass<T>
            {
                private int M1() => 0;
                public int M2(MyClass<int> m) => m.M1();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task InstanceConstructorIsUsed_NoArguments()
    {
        var code = """
            class MyClass
            {
                private MyClass() { }
                public static readonly MyClass Instance = new MyClass();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task InstanceConstructorIsUsed_WithArguments()
    {
        var code = """
            class MyClass
            {
                private MyClass(int i) { }
                public static readonly MyClass Instance = new MyClass(0);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task InstanceConstructorIsUsed_RecordCopyConstructor()
    {
        var code = """
                   var a = new A();
                   
                   sealed record A()
                   {
                       private A(A other) => throw new System.NotImplementedException();
                   }
                   """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                OutputKind = OutputKind.ConsoleApplication
            },
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task PropertyIsRead()
    {
        var code = """
            class MyClass
            {
                private int P => 0;
                public int M() => P;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task IndexerIsRead()
    {
        var code = """
            class MyClass
            {
                private int this[int x] { get { return 0; } set { } }
                public int M(int x) => this[x];
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task EventIsRead()
    {
        var code = """
            using System;

            class MyClass
            {
                private event EventHandler e;
                public EventHandler P => e;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task EventIsSubscribed()
    {
        var code = """
            using System;

            class MyClass
            {
                private event EventHandler e;
                public void M()
                {
                    e += MyHandler;
                }

                static void MyHandler(object sender, EventArgs e)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task EventIsRaised()
    {
        var code = """
            using System;

            class MyClass
            {
                private event EventHandler _eventHandler;

                public void RaiseEvent(EventArgs e)
                {
                    _eventHandler(this, e);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32488")]
    public async Task FieldInNameOf()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public string _goo2 = nameof(_goo);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33765")]
    public async Task GenericFieldInNameOf()
    {
        var code = """
            class MyClass<T>
            {
                private T _goo;
                public string _goo2 = nameof(MyClass<int>._goo);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31581")]
    public async Task MethodInNameOf()
    {
        var code = """
            class MyClass
            {
                private void M() { }
                private string _goo = nameof(M);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33765")]
    public async Task GenericMethodInNameOf()
    {
        var code = """
            class MyClass<T>
            {
                private void M() { }
                private string _goo2 = nameof(MyClass<int>.M);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31581")]
    public async Task PropertyInNameOf()
    {
        var code = """
            class MyClass
            {
                private int P { get; }
                public string _goo = nameof(P);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32522")]
    public async Task TestDynamicInvocation()
    {
        var code = """
            class MyClass
            {
                private void M(dynamic d) { }
                public void M2(dynamic d) => M(d);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32522")]
    public async Task TestDynamicObjectCreation()
    {
        var code = """
            class MyClass
            {
                private MyClass(int i) { }
                public static MyClass Create(dynamic d) => new MyClass(d);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32522")]
    public async Task TestDynamicIndexerAccess()
    {
        var code = """
            class MyClass
            {
                private int[] _list;
                private int this[int index] => _list[index];
                public int M2(dynamic d) => this[d];
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldInDocComment()
    {
        var code = """
            /// <summary>
            /// <see cref="C._goo"/>
            /// </summary>
            class C
            {
                private static int {|IDE0052:_goo|};
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldInDocComment_02()
    {
        var code = """
            class C
            {
                /// <summary>
                /// <see cref="_goo"/>
                /// </summary>
                private static int {|IDE0052:_goo|};
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldInDocComment_03()
    {
        var code = """
            class C
            {
                /// <summary>
                /// <see cref="_goo"/>
                /// </summary>
                public void M() { }

                private static int {|IDE0052:_goo|};
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48247")]
    public async Task GenericMethodInDocComment()
    {
        var code = """
            class C<T>
            {
                /// <summary>
                /// <see cref="C{Int32}.M2()"/>
                /// </summary>
                public void M1() { }

                private void {|IDE0052:M2|}() { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsOnlyWritten()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public void M()
                {
                    _goo = 0;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33994")]
    public async Task PropertyIsOnlyWritten()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class MyClass
                {
                    private int {|#0:P|} { get; set; }
                    public void M()
                    {
                        P = 0;
                    }
                }
                """,
            ExpectedDiagnostics =
            {
                // Test0.cs(3,17): info IDE0052: Private property 'MyClass.P' can be converted to a method as its get accessor is never invoked.
                VerifyCS
                    .Diagnostic(new CSharpRemoveUnusedMembersDiagnosticAnalyzer().SupportedDiagnostics.First(x => x.Id == "IDE0052"))
                    .WithMessage(string.Format(AnalyzersResources.Private_property_0_can_be_converted_to_a_method_as_its_get_accessor_is_never_invoked, "MyClass.P"))
                    .WithLocation(0),
            },
        }.RunAsync();
    }

    [Fact]
    public async Task IndexerIsOnlyWritten()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:this|}[int x] { get { return 0; } set { } }
                public void M(int x, int y)
                {
                    this[x] = y;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task EventIsOnlyWritten()
    {
        var code = """
            class MyClass
            {
                private event System.EventHandler e { add { } remove { } }
                public void M()
                {
                    // CS0079: The event 'MyClass.e' can only appear on the left hand side of += or -=
                    {|CS0079:e|} = null;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsOnlyInitialized_NonConstant()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|} = M();
                public static int M() => 0;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsOnlyWritten_Deconstruction()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public void M()
                {
                    int x;
                    (_goo, x) = (0, 0);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsOnlyWritten_ObjectInitializer()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public MyClass M() => new MyClass() { _goo = 0 };
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsOnlyWritten_InProperty()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public int Goo
                {
                    get { return 0; }
                    set { _goo = value; }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsReadAndWritten()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public void M()
                {
                    _goo = 0;
                    System.Console.WriteLine(_goo);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task PropertyIsReadAndWritten()
    {
        var code = """
            class MyClass
            {
                private int P { get; set; }
                public void M()
                {
                    P = 0;
                    System.Console.WriteLine(P);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task IndexerIsReadAndWritten()
    {
        var code = """
            class MyClass
            {
                private int this[int x] { get { return 0; } set { } }
                public void M(int x)
                {
                    this[x] = 0;
                    System.Console.WriteLine(this[x]);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsReadAndWritten_InProperty()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int Goo
                {
                    get { return _goo; }
                    set { _goo = value; }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30397")]
    public async Task FieldIsIncrementedAndValueUsed()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M1() => ++_goo;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30397")]
    public async Task FieldIsIncrementedAndValueUsed_02()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M1() { return ++_goo; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsIncrementedAndValueDropped()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public void M1() => ++_goo;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsIncrementedAndValueDropped_02()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public void M1() { ++_goo; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task PropertyIsIncrementedAndValueUsed()
    {
        var code = """
            class MyClass
            {
                private int P { get; set; }
                public int M1() => ++P;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task PropertyIsIncrementedAndValueDropped()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:P|} { get; set; }
                public void M1() { ++P; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43191")]
    public async Task PropertyIsIncrementedAndValueDropped_VerifyAnalyzerMessage()
    {
        var code = """
            class MyClass
            {
                private int {|#0:P|} { get; set; }
                public void M1() { ++P; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code, new DiagnosticResult(
            CSharpRemoveUnusedMembersDiagnosticAnalyzer.s_removeUnreadMembersRule)
            .WithLocation(0)
            .WithArguments("MyClass.P"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43191")]
    public async Task PropertyIsIncrementedAndValueDropped_NoDiagnosticWhenPropertyIsReadSomewhereElse()
    {
        var code = """
            class MyClass
            {
                private int P { get; set; }
                public void M1() { ++P; }
                public int M2() => P;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code, []);
    }

    [Fact]
    public async Task IndexerIsIncrementedAndValueUsed()
    {
        var code = """
            class MyClass
            {
                private int this[int x] { get { return 0; } set { } }
                public int M1(int x) => ++this[x];
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task IndexerIsIncrementedAndValueDropped()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:this|}[int x] { get { return 0; } set { } }
                public void M1(int x) => ++this[x];
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43191")]
    public async Task IndexerIsIncrementedAndValueDropped_VerifyAnalyzerMessage()
    {
        var code = """
            class MyClass
            {
                private int {|#0:this|}[int x] { get { return 0; } set { } }
                public void M1(int x) => ++this[x];
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code, new DiagnosticResult(
            CSharpRemoveUnusedMembersDiagnosticAnalyzer.s_removeUnreadMembersRule)
            .WithLocation(0)
            .WithArguments("MyClass.this"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43191")]
    public async Task IndexerIsIncrementedAndValueDropped_NoDiagnosticWhenIndexerIsReadSomewhereElse()
    {
        var code = """
            class MyClass
            {
                private int this[int x] { get { return 0; } set { } }
                public void M1(int x) => ++this[x];
                public int M2(int x) => this[x];
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code, []);
    }

    [Fact]
    public async Task FieldIsTargetOfCompoundAssignmentAndValueUsed()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M1(int x) => _goo += x;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsTargetOfCompoundAssignmentAndValueUsed_02()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M1(int x) { return _goo += x; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsTargetOfCompoundAssignmentAndValueDropped()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public void M1(int x) => _goo += x;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsTargetOfCompoundAssignmentAndValueDropped_02()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public void M1(int x) { _goo += x; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task PropertyIsTargetOfCompoundAssignmentAndValueUsed()
    {
        var code = """
            class MyClass
            {
                private int P { get; set; }
                public int M1(int x) => P += x;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task PropertyIsTargetOfCompoundAssignmentAndValueDropped()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:P|} { get; set; }
                public void M1(int x) { P += x; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43191")]
    public async Task PropertyIsTargetOfCompoundAssignmentAndValueDropped_VerifyAnalyzerMessage()
    {
        var code = """
            class MyClass
            {
                private int {|#0:P|} { get; set; }
                public void M1(int x) { P += x; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code, new DiagnosticResult(
            CSharpRemoveUnusedMembersDiagnosticAnalyzer.s_removeUnreadMembersRule)
            .WithLocation(0)
            .WithArguments("MyClass.P"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43191")]
    public async Task PropertyIsTargetOfCompoundAssignmentAndValueDropped_NoDiagnosticWhenPropertyIsReadSomewhereElse()
    {
        var code = """
            class MyClass
            {
                private int P { get; set; }
                public void M1(int x) { P += x; }
                public int M2() => P;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code, []);
    }

    [Fact]
    public async Task IndexerIsTargetOfCompoundAssignmentAndValueUsed()
    {
        var code = """
            class MyClass
            {
                private int this[int x] { get { return 0; } set { } }
                public int M1(int x, int y) => this[x] += y;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task IndexerIsTargetOfCompoundAssignmentAndValueDropped()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:this|}[int x] { get { return 0; } set { } }
                public void M1(int x, int y) => this[x] += y;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43191")]
    public async Task IndexerIsTargetOfCompoundAssignmentAndValueDropped_VerifyAnalyzerMessage()
    {
        var code = """
            class MyClass
            {
                private int {|#0:this|}[int x] { get { return 0; } set { } }
                public void M1(int x, int y) => this[x] += y;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code, new DiagnosticResult(
            CSharpRemoveUnusedMembersDiagnosticAnalyzer.s_removeUnreadMembersRule)
            .WithLocation(0)
            .WithArguments("MyClass.this"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43191")]
    public async Task IndexerIsTargetOfCompoundAssignmentAndValueDropped_NoDiagnosticWhenIndexerIsReadSomewhereElse()
    {
        var code = """
            class MyClass
            {
                private int this[int x] { get { return 0; } set { } }
                public void M1(int x, int y) => this[x] += y;
                public int M2(int x) => this[x];
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code, []);
    }

    [Fact]
    public async Task FieldIsTargetOfAssignmentAndParenthesized()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public void M1(int x) => (_goo) = x;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsTargetOfAssignmentAndHasImplicitConversion()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public static implicit operator int(MyClass c) => 0;
                public void M1(MyClass c) => _goo = c;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsArg()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M1() => M2(_goo);
                public int M2(int i) { i = 0; return i; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsInArg()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M1() => M2(_goo);
                public int M2(in int i) { return i; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRefArg()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M1() => M2(ref _goo);
                public int M2(ref int i) => i;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsOutArg()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public int M1() => M2(out _goo);
                public int M2(out int i) { i = 0; return i; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MethodIsArg()
    {
        var code = """
            class MyClass
            {
                private int M() => 0;
                public int M1() => M2(M);
                public int M2(System.Func<int> m) => m();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task PropertyIsArg()
    {
        var code = """
            class MyClass
            {
                private int P => 0;
                public int M1() => M2(P);
                public int M2(int p) => p;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task IndexerIsArg()
    {
        var code = """
            class MyClass
            {
                private int this[int x] { get { return 0; } set { } }
                public int M1(int x) => M2(this[x]);
                public int M2(int p) => p;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task EventIsArg()
    {
        var code = """
            using System;

            class MyClass
            {
                private event EventHandler _e;
                public EventHandler M1() => M2(_e);
                public EventHandler M2(EventHandler e) => e;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MultipleFields_AllUnused()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|_goo|] = 0, [|_bar|] = 0;
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Theory, CombinatorialData]
    public async Task MultipleFields_AllUnused_FixOne(
        [CombinatorialValues("[|_goo|]", "[|_goo|] = 0")] string firstField,
        [CombinatorialValues("[|_bar|]", "[|_bar|] = 2")] string secondField,
        [CombinatorialValues(0, 1)] int diagnosticIndex)
    {
        var source = $$"""
            class MyClass
            {
                private int {{firstField}}, {{secondField}};
            }
            """;
        var fixedSource = $$"""
            class MyClass
            {
                private int {{(diagnosticIndex == 0 ? secondField : firstField)}};
            }
            """;
        var batchFixedSource = """
            class MyClass
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedState =
            {
                Sources = { fixedSource },
                MarkupHandling = MarkupMode.Allow,
            },
            BatchFixedCode = batchFixedSource,
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            DiagnosticSelector = fixableDiagnostics => fixableDiagnostics[diagnosticIndex],
        }.RunAsync();
    }

    [Fact]
    public async Task MultipleFields_SomeUnused()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|_goo|] = 0, _bar = 0;
                public int M() => _bar;
            }
            """,
            """
            class MyClass
            {
                private int _bar = 0;
                public int M() => _bar;
            }
            """);
    }

    [Fact]
    public async Task MultipleFields_SomeUnused_02()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int _goo = 0, [|_bar|] = 0;
                public int M() => _goo;
            }
            """,
            """
            class MyClass
            {
                private int _goo = 0;
                public int M() => _goo;
            }
            """);
    }

    [Fact]
    public async Task FieldIsRead_InNestedType()
    {
        var code = """
            class MyClass
            {
                private int _goo;

                class Derived : MyClass
                {
                    public int M() => _goo;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task MethodIsInvoked_InNestedType()
    {
        var code = """
            class MyClass
            {
                private int M1() => 0;

                class Derived : MyClass
                {
                    public int M2() => M1();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldOfNestedTypeIsUnused()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                class NestedType
                {
                    private int [|_goo|];
                }
            }
            """,
            """
            class MyClass
            {
                class NestedType
                {
                }
            }
            """);
    }

    [Fact]
    public async Task FieldOfNestedTypeIsRead()
    {
        var code = """
            class MyClass
            {
                class NestedType
                {
                    private int _goo;

                    public int M() => _goo;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsUnused_PartialClass()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            partial class MyClass
            {
                private int [|_goo|];
            }
            """,
            """
            partial class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task FieldIsRead_PartialClass()
    {
        var code = """
            partial class MyClass
            {
                private int _goo;
            }
            partial class MyClass
            {
                public int M() => _goo;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_PartialClass_DifferentFile()
    {
        var source1 = """
            partial class MyClass
            {
                private int _goo;
            }
            """;
        var source2 = """
            partial class MyClass
            {
                public int M() => _goo;
            }
            """;

        await new VerifyCS.Test
        {
            TestState = { Sources = { source1, source2 } },
            FixedState = { Sources = { source1, source2 } },
        }.RunAsync();
    }

    [Fact]
    public async Task FieldIsOnlyWritten_PartialClass_DifferentFile()
    {
        var source1 = """
            partial class MyClass
            {
                private int {|IDE0052:_goo|};
            }
            """;
        var source2 = """
            partial class MyClass
            {
                public void M() { _goo = 0; }
            }
            """;

        await new VerifyCS.Test
        {
            TestState = { Sources = { source1, source2 } },
            FixedState = { Sources = { source1, source2 } },
        }.RunAsync();
    }

    [Fact]
    public async Task FieldIsRead_InParens()
    {
        var code = """
            class MyClass
            {
                private int _goo;
                public int M() => (_goo);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsWritten_InParens()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public void M() { (_goo) = 1; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsWritten_InParens_02()
    {
        var code = """
            class MyClass
            {
                private int {|IDE0052:_goo|};
                public int M() => (_goo) = 1;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsRead_InDeconstruction_InParens()
    {
        var code = """
            class C
            {
                private int i;

                public void M()
                {
                    var x = ((i, 0), 0);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldInTypeWithGeneratedCode()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                private int [|i|];

                [System.CodeDom.Compiler.GeneratedCodeAttribute("", "")]
                private int j;

                public void M()
                {
                }
            }
            """,
            """
            class C
            {
                [System.CodeDom.Compiler.GeneratedCodeAttribute("", "")]
                private int j;

                public void M()
                {
                }
            }
            """);
    }

    [Fact]
    public async Task FieldIsGeneratedCode()
    {
        var code = """
            class C
            {
                [System.CodeDom.Compiler.GeneratedCodeAttribute("", "")]
                private int i;

                public void M()
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldUsedInGeneratedCode()
    {
        var code = """
            class C
            {
                private int i;

                [System.CodeDom.Compiler.GeneratedCodeAttribute("", "")]
                public int M() => i;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsUnusedInType_SyntaxError()
    {
        var code = """
            class C
            {
                private int i;

                public int M() { return {|CS1525:=|} {|CS1525:;|} }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsUnusedInType_SemanticError()
    {
        var code = """
            class C
            {
                private int i;

                // 'ii' is undefined.
                public int M() => {|CS0103:ii|};
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FieldIsUnusedInType_SemanticErrorInDifferentType()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                private int [|i|];
            }

            class C2
            {
                // 'ii' is undefined.
                public int M() => {|CS0103:ii|};
            }
            """,
            """
            class C
            {
            }

            class C2
            {
                // 'ii' is undefined.
                public int M() => {|CS0103:ii|};
            }
            """);
    }

    [Fact]
    public async Task StructLayoutAttribute_ExplicitLayout()
    {
        var code = """
            using System.Runtime.InteropServices;

            [StructLayoutAttribute(LayoutKind.Explicit)]
            class C
            {
                [FieldOffset(0)]
                private int i;

                [FieldOffset(4)]
                private int i2;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task StructLayoutAttribute_SequentialLayout()
    {
        var code = """
            using System.Runtime.InteropServices;

            [StructLayoutAttribute(LayoutKind.Sequential)]
            struct S
            {
                private int i;
                private int i2;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_OnType_ReferencesField()
    {
        var code = """
            [System.Diagnostics.DebuggerDisplayAttribute("{s}")]
            class C
            {
                private string s;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_OnType_ReferencesMethod()
    {
        var code = """
            [System.Diagnostics.DebuggerDisplayAttribute("{GetString()}")]
            class C
            {
                private string GetString() => "";
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_OnType_ReferencesProperty()
    {
        var code = """
            [System.Diagnostics.DebuggerDisplayAttribute("{MyString}")]
            class C
            {
                private string MyString => "";
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_OnField_ReferencesField()
    {
        var code = """
            class C
            {
                private string s;

                [System.Diagnostics.DebuggerDisplayAttribute("{s}")]
                public int M;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_OnProperty_ReferencesMethod()
    {
        var code = """
            class C
            {
                private string GetString() => "";

                [System.Diagnostics.DebuggerDisplayAttribute("{GetString()}")]
                public int M => 0;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_OnProperty_ReferencesProperty()
    {
        var code = """
            class C
            {
                private string MyString { get { return ""; } }

                [System.Diagnostics.DebuggerDisplayAttribute("{MyString}")]
                public int M { get { return 0; } }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task DebuggerDisplayAttribute_OnNestedTypeMember_ReferencesField()
    {
        var code = """
            class C
            {
                private static string s;

                class Nested
                {
                    [System.Diagnostics.DebuggerDisplayAttribute("{C.s}")]
                    public int M;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30886")]
    public async Task SerializableConstructor_TypeImplementsISerializable()
    {
        var code = """
            using System.Runtime.Serialization;

            class C : ISerializable
            {
                public C()
                {
                }

                private C(SerializationInfo info, StreamingContext context)
                {
                }

                public void GetObjectData(SerializationInfo info, StreamingContext context)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30886")]
    public async Task SerializableConstructor_BaseTypeImplementsISerializable()
    {
        var code = """
            using System;
            using System.Runtime.Serialization;

            class C : Exception 
            {
                public C()
                {
                }

                private C(SerializationInfo info, StreamingContext context)
                    : base(info, context)
                {
                }

                public void GetObjectData(SerializationInfo info, StreamingContext context)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Theory]
    [InlineData(@"[System.Runtime.Serialization.OnDeserializingAttribute]")]
    [InlineData(@"[System.Runtime.Serialization.OnDeserializedAttribute]")]
    [InlineData(@"[System.Runtime.Serialization.OnSerializingAttribute]")]
    [InlineData(@"[System.Runtime.Serialization.OnSerializedAttribute]")]
    [InlineData(@"[System.Runtime.InteropServices.ComRegisterFunctionAttribute]")]
    [InlineData(@"[System.Runtime.InteropServices.ComUnregisterFunctionAttribute]")]
    public async Task MethodsWithSpecialAttributes(string attribute)
    {
        var code = $$"""
            class C
            {
                {{attribute}}
                private void M()
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30887")]
    public async Task ShouldSerializePropertyMethod()
    {
        var code = """
            class C
            {
                private bool ShouldSerializeData()
                {
                    return true;
                }

                public int Data { get; private set; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38491")]
    public async Task ResetPropertyMethod()
    {
        var code = """
            class C
            {
                private void ResetData()
                {
                    return;
                }

                public int Data { get; private set; }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30377")]
    public async Task EventHandlerMethod()
    {
        var code = """
            using System;

            class C
            {
                private void M(object o, EventArgs args)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32727")]
    public async Task NestedStructLayoutTypeWithReference()
    {
        var code = """
            using System.Runtime.InteropServices;

            class Program
            {
                private const int MAX_PATH = 260;

                [StructLayout(LayoutKind.Sequential)]
                internal struct ProcessEntry32
                {
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
                    public string szExeFile;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task FixAllFields_Document()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|_goo|] = 0, [|_bar|];
                private int [|_x|] = 0, [|_y|], _z = 0;
                private string [|_fizz|] = null;

                public int Method() => _z;
            }
            """,
            """
            class MyClass
            {
                private int _z = 0;

                public int Method() => _z;
            }
            """);
    }

    [Fact]
    public async Task FixAllMethods_Document()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|M1|]() => 0;
                private void [|M2|]() { }
                private static void [|M3|]() { }
                private class NestedClass
                {
                    private void [|M4|]() { }
                }
            }
            """,
            """
            class MyClass
            {
                private class NestedClass
                {
                }
            }
            """);
    }

    [Fact]
    public async Task FixAllProperties_Document()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class MyClass
            {
                private int [|P1|] => 0;
                private int [|P2|] { get; set; }
                private int [|P3|] { get { return 0; } set { } }
                private int [|this|][int i] { get { return 0; } }
            }
            """,
            """
            class MyClass
            {
            }
            """);
    }

    [Fact]
    public async Task FixAllEvents_Document()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class MyClass
            {
                private event EventHandler [|E1|], E2 = null, [|E3|];
                private event EventHandler [|E4|], [|E5|] = null;
                private event EventHandler [|E|]
                {
                    add { }
                    remove { }
                }

                public void M()
                {
                    EventHandler handler = E2;
                }
            }
            """,
            """
            using System;

            class MyClass
            {
                private event EventHandler E2 = null;

                public void M()
                {
                    EventHandler handler = E2;
                }
            }
            """);
    }

    [Fact]
    public async Task FixAllMembers_Project()
    {
        var source1 = """
            using System;

            partial class MyClass
            {
                private int [|f1|], f2 = 0, [|f3|];
                private void [|M1|]() { }
                private int [|P1|] => 0;
                private int [|this|][int x] { get { return 0; } set { } }
                private event EventHandler [|e1|], [|e2|] = null;
            }

            class MyClass2
            {
                private void [|M2|]() { }
            }
            """;
        var source2 = """
            partial class MyClass
            {
                private void [|M3|]() { }
                public int M4() => f2;
            }

            static class MyClass3
            {
                private static void [|M5|]() { }
            }
            """;
        var fixedSource1 = """
            using System;

            partial class MyClass
            {
                private int f2 = 0;
            }

            class MyClass2
            {
            }
            """;
        var fixedSource2 = """
            partial class MyClass
            {
                public int M4() => f2;
            }

            static class MyClass3
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestState = { Sources = { source1, source2 } },
            FixedState = { Sources = { fixedSource1, fixedSource2 } },
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32702")]
    public async Task UsedExtensionMethod_ReferencedFromPartialMethod()
    {
        var source1 = """
            static partial class B
            {
                public static void Entry() => PartialMethod();
                static partial void PartialMethod();
            }
            """;
        var source2 = """
            static partial class B
            {
                static partial void PartialMethod()
                {
                    UsedMethod();
                }

                private static void UsedMethod() { }
            }
            """;

        await new VerifyCS.Test
        {
            TestState = { Sources = { source1, source2 } },
            FixedState = { Sources = { source1, source2 } },
        }.RunAsync();
    }

    [Fact]
    public async Task UsedExtensionMethod_ReferencedFromExtendedPartialMethod()
    {
        var source1 = """
            static partial class B
            {
                public static void Entry() => PartialMethod();
                public static partial void PartialMethod();
            }
            """;
        var source2 = """
            static partial class B
            {
                public static partial void PartialMethod()
                {
                    UsedMethod();
                }

                private static void UsedMethod() { }
            }
            """;

        await new VerifyCS.Test
        {
            TestState = { Sources = { source1, source2 } },
            FixedState = { Sources = { source1, source2 } },
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32842")]
    public async Task FieldIsRead_NullCoalesceAssignment()
    {
        var code = """
            public class MyClass
            {
                private MyClass _field;
                public MyClass Property => _field ??= new MyClass();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32842")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/66975")]
    public async Task FieldIsNotRead_NullCoalesceAssignment()
    {
        var code = """
            public class MyClass
            {
                private MyClass _field;
                public void M() => _field ??= new MyClass();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37213")]
    public async Task UsedPrivateExtensionMethod()
    {
        var code = """
            public static class B
            {
                public static void PublicExtensionMethod(this string s) => s.PrivateExtensionMethod();
                private static void PrivateExtensionMethod(this string s) { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30884")]
    public async Task TestMessageForConstructor()
    {
        await VerifyCS.VerifyAnalyzerAsync(
            """
            class C
            {
                private {|#0:C|}(int i) { }
            }
            """,
            // /0/Test0.cs(3,13): info IDE0051: Private member 'C.C' is unused
            VerifyCS.Diagnostic("IDE0051").WithLocation(0).WithArguments("C.C"));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62856")]
    public async Task DontWarnForAwaiterMethods()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class C : ICriticalNotifyCompletion
            {
                public async Task M()
                {
                    await this;
                }

                private C GetAwaiter() => this;
                private bool IsCompleted => false;
                private void GetResult() { }
                public void OnCompleted(Action continuation) => Task.Run(continuation);
                public void UnsafeOnCompleted(Action continuation) => Task.Run(continuation);
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62856")]
    public async Task WarnForAwaiterMethodsNotImplementingInterface()
    {
        const string code = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class C
            {
                private C [|GetAwaiter|]() => this;
                private bool [|IsCompleted|] => false;
                private void [|GetResult|]() { }
                public void OnCompleted(Action continuation) => Task.Run(continuation);
                public void UnsafeOnCompleted(Action continuation) => Task.Run(continuation);
            }
            """;
        const string fixedCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            class C
            {
                public void OnCompleted(Action continuation) => Task.Run(continuation);
                public void UnsafeOnCompleted(Action continuation) => Task.Run(continuation);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }
}
