// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.AddParameter;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddParameter;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
public sealed class AddParameterTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpAddParameterCodeFixProvider());

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    [Fact]
    public Task TestMissingWithImplicitConstructor()
        => TestMissingAsync(
            """
            class C
            {
            }

            class D
            {
                void M()
                {
                    new [|C|](1);
                }
            }
            """);

    [Fact]
    public Task TestOnEmptyConstructor()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C() { }
            }

            class D
            {
                void M()
                {
                    new [|C|](1);
                }
            }
            """,
            """
            class C
            {
                public C(int v) { }
            }

            class D
            {
                void M()
                {
                    new C(1);
                }
            }
            """);

    [Fact]
    public Task TestNamedArg()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C() { }
            }

            class D
            {
                void M()
                {
                    new C([|p|]: 1);
                }
            }
            """,
            """
            class C
            {
                public C(int p) { }
            }

            class D
            {
                void M()
                {
                    new C(p: 1);
                }
            }
            """);

    [Fact]
    public Task TestMissingWithConstructorWithSameNumberOfParams()
        => TestMissingAsync(
            """
            class C
            {
                public C(bool b) { }
            }

            class D
            {
                void M()
                {
                    new [|C|](1);
                }
            }
            """);

    [Fact]
    public Task TestAddBeforeMatchingArg()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(int i) { }
            }

            class D
            {
                void M()
                {
                    new [|C|](true, 1);
                }
            }
            """,
            """
            class C
            {
                public C(bool v, int i) { }
            }

            class D
            {
                void M()
                {
                    new C(true, 1);
                }
            }
            """);

    [Fact]
    public Task TestAddAfterMatchingConstructorParam()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(int i) { }
            }

            class D
            {
                void M()
                {
                    new [|C|](1, true);
                }
            }
            """,
            """
            class C
            {
                public C(int i, bool v) { }
            }

            class D
            {
                void M()
                {
                    new C(1, true);
                }
            }
            """);

    [Fact]
    public Task TestParams1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(params int[] i) { }
            }

            class D
            {
                void M()
                {
                    new C([|true|], 1);
                }
            }
            """,
            """
            class C
            {
                public C(bool v, params int[] i) { }
            }

            class D
            {
                void M()
                {
                    new C(true, 1);
                }
            }
            """);

    [Fact]
    public Task TestParams2()
        => TestMissingAsync(
            """
            class C
            {
                public C(params int[] i) { }
            }

            class D
            {
                void M()
                {
                    new [|C|](1, true);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")]
    public Task TestMultiLineParameters1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(int i,
                         /* goo */ int j)
                {

                }

                private void Goo()
                {
                    new [|C|](true, 0, 0);
                }
            }
            """,
            """
            class C
            {
                public C(bool v,
                         int i,
                         /* goo */ int j)
                {

                }

                private void Goo()
                {
                    new C(true, 0, 0);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")]
    public Task TestMultiLineParameters2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(int i,
                         /* goo */ int j)
                {

                }

                private void Goo()
                {
                    new [|C|](0, true, 0);
                }
            }
            """,
            """
            class C
            {
                public C(int i,
                         bool v,
                         /* goo */ int j)
                {

                }

                private void Goo()
                {
                    new C(0, true, 0);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")]
    public Task TestMultiLineParameters3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(int i,
                         /* goo */ int j)
                {

                }

                private void Goo()
                {
                    new [|C|](0, 0, true);
                }
            }
            """,
            """
            class C
            {
                public C(int i,
                         /* goo */ int j,
                         bool v)
                {

                }

                private void Goo()
                {
                    new C(0, 0, true);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")]
    public Task TestMultiLineParameters4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(
                    int i,
                    /* goo */ int j)
                {

                }

                private void Goo()
                {
                    new [|C|](true, 0, 0);
                }
            }
            """,
            """
            class C
            {
                public C(
                    bool v,
                    int i,
                    /* goo */ int j)
                {

                }

                private void Goo()
                {
                    new C(true, 0, 0);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")]
    public Task TestMultiLineParameters5()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(
                    int i,
                    /* goo */ int j)
                {

                }

                private void Goo()
                {
                    new [|C|](0, true, 0);
                }
            }
            """,
            """
            class C
            {
                public C(
                    int i,
                    bool v,
                    /* goo */ int j)
                {

                }

                private void Goo()
                {
                    new C(0, true, 0);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20708")]
    public Task TestMultiLineParameters6()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(
                    int i,
                    /* goo */ int j)
                {

                }

                private void Goo()
                {
                    new [|C|](0, 0, true);
                }
            }
            """,
            """
            class C
            {
                public C(
                    int i,
                    /* goo */ int j,
                    bool v)
                {

                }

                private void Goo()
                {
                    new C(0, 0, true);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20973")]
    public Task TestNullArg1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(int i) { }
            }

            class D
            {
                void M()
                {
                    new [|C|](null, 1);
                }
            }
            """,
            """
            class C
            {
                public C(object value, int i) { }
            }

            class D
            {
                void M()
                {
                    new C(null, 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20973")]
    public Task TestNullArg2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(string s) { }
            }

            class D
            {
                void M()
                {
                    new [|C|](null, 1);
                }
            }
            """,
            """
            class C
            {
                public C(string s, int v) { }
            }

            class D
            {
                void M()
                {
                    new C(null, 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20973")]
    public Task TestDefaultArg1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(int i) { }
            }

            class D
            {
                void M()
                {
                    new [|C|](default, 1);
                }
            }
            """,
            """
            class C
            {
                public C(int i, int v) { }
            }

            class D
            {
                void M()
                {
                    new C(default, 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20973")]
    public Task TestDefaultArg2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(string s) { }
            }

            class D
            {
                void M()
                {
                    new [|C|](default, 1);
                }
            }
            """,
            """
            class C
            {
                public C(string s, int v) { }
            }

            class D
            {
                void M()
                {
                    new C(default, 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationInstanceMethod1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M1()
                {
                }
                void M2()
                {
                    int i=0;
                    [|M1|](i);
                }
            }
            """,
            """
            class C
            {
                void M1(int i)
                {
                }
                void M2()
                {
                    int i=0;
                    M1(i);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationInheritedMethodGetFixed()
        => TestInRegularAndScriptAsync(
            """
            class Base
            {
                protected void M1()
                {
                }
            }
            class C1 : Base
            {
                void M2()
                {
                    int i = 0;
                    [|M1|](i);
                }
            }
            """,
            """
            class Base
            {
                protected void M1(int i)
                {
                }
            }
            class C1 : Base
            {
                void M2()
                {
                    int i = 0;
                    M1(i);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationInheritedMethodInMetadatGetsNotFixed()
        => TestMissingAsync(
            """
            class C1
            {
                void M2()
                {
                    int i = 0;
                    [|GetHashCode|](i);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class C1
            {
                void M1()
                {
                    int Local() => 1;
                    [|Local|](2);
                }
            }
            """,
            """
            class C1
            {
                void M1()
                {
                    int Local(int v) => 1;
                    Local(2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    [Trait("TODO", "Fix broken")]
    public Task TestInvocationLambda1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            class C1
            {
                void M1()
                {
                    Action a = () => { };
                    [|a|](2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationStaticMethod()
        => TestInRegularAndScriptAsync(
            """
            class C1
            {
                static void M1()
                {
                }
                void M2()
                {
                    [|M1|](1);
                }
            }
            """,
            """
            class C1
            {
                static void M1(int v)
                {
                }
                void M2()
                {
                    M1(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationExtensionMethod()
        => TestInRegularAndScriptAsync("""
            namespace N {
            static class Extensions
            {
                public static void ExtensionM1(this object o)
                {
                }
            }
            class C1
            {
                void M1()
                {
                    new object().[|ExtensionM1|](1);
                }
            }}
            """, """
            namespace N {
            static class Extensions
            {
                public static void ExtensionM1(this object o, int v)
                {
                }
            }
            class C1
            {
                void M1()
                {
                    new object().ExtensionM1(1);
                }
            }}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationExtensionMethod_StaticInvocationStyle()
        => TestInRegularAndScriptAsync("""
            namespace N {
            static class Extensions
            {
                public static void ExtensionM1(this object o)
                {
                }
            }
            class C1
            {
                void M1()
                {
                    Extensions.[|ExtensionM1|](new object(), 1);
                }
            }}
            """, """
            namespace N {
            static class Extensions
            {
                public static void ExtensionM1(this object o, int v)
                {
                }
            }
            class C1
            {
                void M1()
                {
                    Extensions.ExtensionM1(new object(), 1);
                }
            }}
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public async Task TestInvocationOverride()
    {
        var code = """
            class Base
            {
                protected virtual void M1() { }
            }
            class C1 : Base
            {
                protected override void M1() { }
                void M2()
                {
                    [|M1|](1);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, """
            class Base
            {
                protected virtual void M1() { }
            }
            class C1 : Base
            {
                protected override void M1(int v) { }
                void M2()
                {
                    M1(1);
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(code, """
            class Base
            {
                protected virtual void M1(int v) { }
            }
            class C1 : Base
            {
                protected override void M1(int v) { }
                void M2()
                {
                    M1(1);
                }
            }
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public async Task TestInvocationExplicitInterface()
    {
        var code = """
            interface I1
            {
                void M1();
            }
            class C1 : I1
            {
                void I1.M1() { }
                void M2()
                {
                    ((I1)this).[|M1|](1);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, """
            interface I1
            {
                void M1(int v);
            }
            class C1 : I1
            {
                void I1.M1() { }
                void M2()
                {
                    ((I1)this).M1(1);
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(code, """
            interface I1
            {
                void M1(int v);
            }
            class C1 : I1
            {
                void I1.M1(int v) { }
                void M2()
                {
                    ((I1)this).M1(1);
                }
            }
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public async Task TestInvocationImplicitInterface()
    {
        var code =
            """
            interface I1
            {
                void M1();
            }
            class C1 : I1
            {
                public void M1() { }
                void M2()
                {
                    [|M1|](1);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, """
            interface I1
            {
                void M1();
            }
            class C1 : I1
            {
                public void M1(int v) { }
                void M2()
                {
                    M1(1);
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(code, """
            interface I1
            {
                void M1(int v);
            }
            class C1 : I1
            {
                public void M1(int v) { }
                void M2()
                {
                    M1(1);
                }
            }
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public async Task TestInvocationImplicitInterfaces()
    {
        var code =
            """
            interface I1
            {
                void M1();
            }
            interface I2
            {
                void M1();
            }
            class C1 : I1, I2
            {
                public void M1() { }
                void M2()
                {
                    [|M1|](1);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, """
            interface I1
            {
                void M1();
            }
            interface I2
            {
                void M1();
            }
            class C1 : I1, I2
            {
                public void M1(int v) { }
                void M2()
                {
                    M1(1);
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(code, """
            interface I1
            {
                void M1(int v);
            }
            interface I2
            {
                void M1(int v);
            }
            class C1 : I1, I2
            {
                public void M1(int v) { }
                void M2()
                {
                    M1(1);
                }
            }
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    [Trait("TODO", "Fix broken")]
    public Task TestInvocationGenericMethod()
        => TestInRegularAndScriptAsync(
            """
            class C1
            {
                void M1<T>(T arg) { }
                void M2()
                {
                    [|M1|](1, 2);
                }
            }
            """,
            """
            class C1
            {
                void M1<T>(T arg, int v) { }
                void M2()
                {
                    M1(1, 2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationRecursion()
        => TestInRegularAndScriptAsync(
            """
            class C1
            {
                void M1()
                {
                    [|M1|](1);
                }
            }
            """,
            """
            class C1
            {
                void M1(int v)
                {
                    M1(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public async Task TestInvocationOverloads1()
    {
        var code =
            """
            class C1
            {
                void M1(string s) { }
                void M1(int i) { }
                void M2()
                {
                    [|M1|](1, 2);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, """
            class C1
            {
                void M1(string s) { }
                void M1(int i, int v) { }
                void M2()
                {
                    M1(1, 2);
                }
            }
            """, 0);
        await TestInRegularAndScriptAsync(code, """
            class C1
            {
                void M1(int v, string s) { }
                void M1(int i) { }
                void M2()
                {
                    M1(1, 2);
                }
            }
            """, 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public async Task TestInvocationOverloads2()
    {
        var code =
            """
            class C1
            {
                void M1(string s1, string s2) { }
                void M1(string s) { }
                void M1(int i) { }
                void M2()
                {
                    M1(1, [|2|]);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, """
            class C1
            {
                void M1(string s1, string s2) { }
                void M1(string s) { }
                void M1(int i, int v) { }
                void M2()
                {
                    M1(1, 2);
                }
            }
            """, 0);
        await TestInRegularAndScriptAsync(code, """
            class C1
            {
                void M1(string s1, string s2) { }
                void M1(int v, string s) { }
                void M1(int i) { }
                void M2()
                {
                    M1(1, 2);
                }
            }
            """, 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationTuple1()
        => TestInRegularAndScriptAsync("""
            class C1
            {
                void M1((int, int) t1)
                {
                }
                void M2()
                {
                    [|M1|]((0, 0), (1, "1"));
                }
            }
            """, """
            class C1
            {
                void M1((int, int) t1, (int, string) value)
                {
                }
                void M2()
                {
                    M1((0, 0), (1, "1"));
                }
            }
            """, 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationTuple2()
        => TestInRegularAndScriptAsync("""
            class C1
            {
                void M1((int, int) t1)
                {
                }
                void M2()
                {
                    var tup = (1, "1");
                    [|M1|]((0, 0), tup);
                }
            }
            """, """
            class C1
            {
                void M1((int, int) t1, (int, string) tup)
                {
                }
                void M2()
                {
                    var tup = (1, "1");
                    M1((0, 0), tup);
                }
            }
            """, 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationTuple3()
        => TestInRegularAndScriptAsync("""
            class C1
            {
                void M1((int, int) t1)
                {
                }
                void M2()
                {
                    var tup = (i: 1, s: "1");
                    [|M1|]((0, 0), tup);
                }
            }
            """, """
            class C1
            {
                void M1((int, int) t1, (int i, string s) tup)
                {
                }
                void M2()
                {
                    var tup = (i: 1, s: "1");
                    M1((0, 0), tup);
                }
            }
            """, 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_Missing_TypeArguments_AddingTypeArgumentAndParameter()
        => TestMissingInRegularAndScriptAsync("""
            class C1
            {
                void M1<T>(T i) { }
                void M2()
                {
                    [|M1|]<int, bool>(1, true);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_Missing_TypeArguments_AddingTypeArgument()
        => TestMissingInRegularAndScriptAsync("""
            class C1
            {
                void M1(int i) { }
                void M2()
                {
                    [|M1<bool>|](1, true);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    [Trait("TODO", "Fix missing")]
    public Task TestInvocation_Missing_ExplicitInterfaceImplementation()
        => TestMissingAsync("""
            interface I1
            {
                void M1();
            }
            class C1 : I1
            {
                    void I1.M1() { }
                    void I1.[|M1|](int i) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_OverloadResolutionFailure()
        => TestInRegularAndScriptAsync("""
            class C1
            {
                void M1(int i1, int i2) { }
                void M1(double d) { }
                void M2()
                {
                    M1([|1.0|], 1);
                }
            }
            """, """
            class C1
            {
                void M1(int i1, int i2) { }
                void M1(double d, int v) { }
                void M2()
                {
                    M1(1.0, 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_LambdaExpressionParameter()
        => TestInRegularAndScriptAsync("""
            class C1
            {
                void M1(int i1, int i2) { }
                void M1(System.Action a) { }
                void M2()
                {
                    M1([|()=> { }|], 1);
                }
            }
            """, """
            class C1
            {
                void M1(int i1, int i2) { }
                void M1(System.Action a, int v) { }
                void M2()
                {
                    M1(()=> { }, 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_NamedParameter()
        => TestInRegularAndScriptAsync("""
            class C1
            {
                void M1(int i1) { }
                void M2()
                {
                    M1([|i2|]: 1);
                }
            }
            """, """
            class C1
            {
                void M1(int i1, int i2) { }
                void M2()
                {
                    M1(i2: 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationAddTypeParameter_AddTypeParameterIfUserSpecifiesOne_OnlyTypeArgument()
        => TestMissingInRegularAndScriptAsync("""
            class C1
            {
                void M1() { }
                void M2()
                {
                    [|M1|]<bool>();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocationAddTypeParameter_AddTypeParameterIfUserSpecifiesOne_TypeArgumentAndParameterArgument()
        => TestMissingInRegularAndScriptAsync("""
            class C1
            {
                void M1() { }
                void M2()
                {
                    [|M1|]<bool>(true);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_ExisitingTypeArgumentIsNotGeneralized()
        => TestInRegularAndScriptAsync("""
            class C1
            {
                void M1<T>(T v) { }
                void M2()
                {
                    [|M1|](true, true);
                }
            }
            """, """
            class C1
            {
                void M1<T>(T v, bool v1) { }
                void M2()
                {
                    M1(true, true);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_AddParameterToMethodWithParams()
        => TestInRegularAndScriptAsync("""
            class C1
            {
                static void M1(params int[] nums) { }
                static void M2()
                {
                    M1([|true|], 4);
                }
            }
            """, """
            class C1
            {
                static void M1(bool v, params int[] nums) { }
                static void M2()
                {
                    M1(true, 4);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public async Task TestInvocation_Cascading_FixingVirtualFixesOverrideToo()
    {
        // error CS1501: No overload for method 'M1' takes 1 arguments
        var code =
            """
            class BaseClass
            {
                protected virtual void M1() { }
            }
            class Derived1: BaseClass
            {
                protected override void M1() { }
            }
            class Test: BaseClass
            {
                void M2() 
                {
                    [|M1|](1);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, """
            class BaseClass
            {
                protected virtual void M1(int v) { }
            }
            class Derived1: BaseClass
            {
                protected override void M1() { }
            }
            class Test: BaseClass
            {
                void M2() 
                {
                    M1(1);
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(code, """
            class BaseClass
            {
                protected virtual void M1(int v) { }
            }
            class Derived1: BaseClass
            {
                protected override void M1(int v) { }
            }
            class Test: BaseClass
            {
                void M2() 
                {
                    M1(1);
                }
            }
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_Cascading_PartialMethods()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace N1
            {
                partial class C1
                {
                    partial void PartialM();
                }
            }
                    </Document>
                    <Document>
            namespace N1
            {
                partial class C1
                {
                    partial void PartialM() { }
                    void M1()
                    {
                        [|PartialM|](1);
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace N1
            {
                partial class C1
                {
                    partial void PartialM(int v);
                }
            }
                    </Document>
                    <Document>
            namespace N1
            {
                partial class C1
                {
                    partial void PartialM(int v) { }
                    void M1()
                    {
                        PartialM(1);
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task TestInvocation_Cascading_ExtendedPartialMethods()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace N1
            {
                partial class C1
                {
                    public partial void PartialM();
                }
            }
                    </Document>
                    <Document>
            namespace N1
            {
                partial class C1
                {
                    public partial void PartialM() { }
                    void M1()
                    {
                        [|PartialM|](1);
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            namespace N1
            {
                partial class C1
                {
                    public partial void PartialM(int v);
                }
            }
                    </Document>
                    <Document>
            namespace N1
            {
                partial class C1
                {
                    public partial void PartialM(int v) { }
                    void M1()
                    {
                        PartialM(1);
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_Cascading_PartialMethodsInSameDocument()
        => TestInRegularAndScriptAsync("""
            namespace N1
            {
                partial class C1
                {
                    partial void PartialM();
                }
                partial class C1
                {
                    partial void PartialM() { }
                    void M1()
                    {
                        [|PartialM|](1);
                    }
                }
            }
            """, """
            namespace N1
            {
                partial class C1
                {
                    partial void PartialM(int v);
                }
                partial class C1
                {
                    partial void PartialM(int v) { }
                    void M1()
                    {
                        PartialM(1);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_Cascading_BaseNotInSource()
        => TestMissingAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <MetadataReferenceFromSource Language="C#" CommonReferences="true">
                        <Document FilePath="ReferencedDocument">
            namespace N
            {
                public class BaseClass
                {
                    public virtual void M() { }
                }
            }
                        </Document>
                    </MetadataReferenceFromSource>
                    <Document FilePath="TestDocument">
            namespace N
            {
                public class Derived: BaseClass
                {
                    public void M2()
                    {
                        [|M|](1);
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public async Task TestInvocation_Cascading_RootNotInSource()
    {
        // error CS1501: No overload for method 'M' takes 1 arguments
        var code =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <MetadataReferenceFromSource Language="C#" CommonReferences="true">
                        <Document FilePath="ReferencedDocument">namespace N
            {
                public class BaseClass
                {
                    public virtual void M() { }
                }
            }</Document>
                    </MetadataReferenceFromSource>
                    <Document FilePath="TestDocument">namespace N
            {
                public class Derived: BaseClass
                {
                    public override void M() { }
                }
                public class DerivedDerived: Derived
                {
                    public void M2()
                    {
                        [|M|](1);
                    }
                }
            }</Document>
                </Project>
            </Workspace>
            """;
        await TestInRegularAndScriptAsync(code, """
            namespace N
            {
                public class Derived: BaseClass
                {
                    public override void M(int v) { }
                }
                public class DerivedDerived: Derived
                {
                    public void M2()
                    {
                        M(1);
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(code, """
            namespace N
            {
                public class Derived: BaseClass
                {
                    public override void M({|Conflict:int v|}) { }
                }
                public class DerivedDerived: Derived
                {
                    public void M2()
                    {
                        M(1);
                    }
                }
            }
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_Cascading_ManyReferencesInManyProjects()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="A1">
                    <Document FilePath="ReferencedDocument">
            namespace N
            {
                public class BaseClass
                {
                    public virtual void M() { }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true"  AssemblyName="A2">
                    <ProjectReference>A1</ProjectReference>
                    <Document>
            namespace N
            {
                public class Derived1: BaseClass
                {
                    public override void M() { }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true"  AssemblyName="A3">
                    <ProjectReference>A1</ProjectReference>
                    <Document>
            namespace N
            {
                public class Derived2: BaseClass
                {
                    public override void M() { }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true"  AssemblyName="A4">
                    <ProjectReference>A3</ProjectReference>
                    <Document>
            namespace N
            {
                public class T
                {
                    public void Test() { 
                        new Derived2().[|M|](1);
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="A1">
                    <Document FilePath="ReferencedDocument">
            namespace N
            {
                public class BaseClass
                {
                    public virtual void M(int v) { }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true"  AssemblyName="A2">
                    <ProjectReference>A1</ProjectReference>
                    <Document>
            namespace N
            {
                public class Derived1: BaseClass
                {
                    public override void M(int v) { }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true"  AssemblyName="A3">
                    <ProjectReference>A1</ProjectReference>
                    <Document>
            namespace N
            {
                public class Derived2: BaseClass
                {
                    public override void M(int v) { }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true"  AssemblyName="A4">
                    <ProjectReference>A3</ProjectReference>
                    <Document>
            namespace N
            {
                public class T
                {
                    public void Test() { 
                        new Derived2().M(1);
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public async Task TestInvocation_Cascading_OfferFixCascadingForImplicitInterface()
    {
        // error CS1501: No overload for method 'M1' takes 1 arguments
        var code =
            """
            interface I1
            {
                void M1();
            }
            class C: I1
            {
                public void M1() { }
                void MTest() 
                {
                    [|M1|](1);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, """
            interface I1
            {
                void M1();
            }
            class C: I1
            {
                public void M1(int v) { }
                void MTest() 
                {
                    M1(1);
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(code, """
            interface I1
            {
                void M1(int v);
            }
            class C: I1
            {
                public void M1(int v) { }
                void MTest() 
                {
                    M1(1);
                }
            }
            """, index: 1);
    }

#if !CODE_STYLE

    // CodeStyle layer does not support cross language application of fixes.

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_Cascading_CrossLanguage()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VB1">
                    <Document FilePath="ReferencedDocument">
            Namespace N
                Public Class BaseClass
                    Public Overridable Sub M()
                    End Sub
                End Class
            End Namespace
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true"  AssemblyName="A2">
                    <ProjectReference>VB1</ProjectReference>
                    <Document>
            namespace N
            {
                public class Derived: BaseClass
                {
                    public override void M() { }
                }
                public class T
                {
                    public void Test() { 
                        new Derived().[|M|](1);
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VB1">
                    <Document FilePath="ReferencedDocument">
            Namespace N
                Public Class BaseClass
                    Public Overridable Sub M(v As Integer)
                    End Sub
                End Class
            End Namespace
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true"  AssemblyName="A2">
                    <ProjectReference>VB1</ProjectReference>
                    <Document>
            namespace N
            {
                public class Derived: BaseClass
                {
                    public override void M(int v) { }
                }
                public class T
                {
                    public void Test() { 
                        new Derived().M(1);
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, index: 1);

#endif

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public async Task TestInvocation_InvocationStyles_Positional_MoreThanOneArgumentToMuch()
    {
        var code =
            """
            class C
            {
                void M() { }
                void Test()
                {
                    [|M|](1, 2, 3, 4);
                }
            }
            """;
        await TestActionCountAsync(code, 1);
        await TestInRegularAndScriptAsync(code, """
            class C
            {
                void M(int v) { }
                void Test()
                {
                    M(1, 2, 3, 4);
                }
            }
            """, index: 0);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_Positional_WithOptionalParam()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M(int i = 1) { }
                void Test()
                {
                    [|M|](1, 2);
                }
            }
            """, """
            class C
            {
                void M(int i = 1, int v = 0) { }
                void Test()
                {
                    M(1, 2);
                }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_Named_WithOptionalParam()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M(int i1, int i2 = 1) { }
                void Test()
                {
                    M(1, i2: 2, [|i3|]: 3);
                }
            }
            """, """
            class C
            {
                void M(int i1, int i2 = 1, int i3 = 0) { }
                void Test()
                {
                    M(1, i2: 2, i3: 3);
                }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_Positional_WithParams()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M(params int[] ints) { }
                void Test()
                {
                    M([|"text"|]);
                }
            }
            """, """
            class C
            {
                void M(string v, params int[] ints) { }
                void Test()
                {
                    M("text");
                }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_Named_WithTypemissmatch()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                void M(int i) { }
                void Test()
                {
                    M(i: [|"text"|]);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_NamedAndPositional1()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M(int i1, string s) { }
                void Test()
                {
                    M(1, s: "text", [|i2|]: 0);
                }
            }
            """, """
            class C
            {
                void M(int i1, string s, int i2) { }
                void Test()
                {
                    M(1, s: "text", i2: 0);
                }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_NamedAndPositional2()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                void M(string s) { }
                void Test()
                {
                    M(1, [|s|]: "text");
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_Incomplete_1()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M() { }
                void Test()
                {
                    [|M|](1
                }
            }
            """, """
            class C
            {
                void M(int v) { }
                void Test()
                {
                    M(1
                }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_Incomplete_2()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M(int v) { }
                void Test()
                {
                    [|M|]("text", 1
            """, """
            class C
            {
                void M(string v1, int v) { }
                void Test()
                {
                    M("text", 1
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_RefParameter()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M() { }
                void Test()
                {
                    int i = 0;
                    [|M|](ref i);
                }
            }
            """, """
            class C
            {
                void M(ref int i) { }
                void Test()
                {
                    int i = 0;
                    M(ref i);
                }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_OutParameter_WithTypeDeclarationOutsideArgument()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M() { }
                void Test()
                {
                    int i = 0;
                    [|M|](out i);
                }
            }
            """, """
            class C
            {
                void M(out int i) { }
                void Test()
                {
                    int i = 0;
                    M(out i);
                }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_OutParameter_WithTypeDeclarationInArgument()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M() { }
                void Test()
                {
                    [|M|](out int i);
                }
            }
            """, """
            class C
            {
                void M(out int i) { }
                void Test()
                {
                    M(out int i);
                }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_InvocationStyles_OutParameter_WithVarTypeDeclarationInArgument()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M() { }
                void Test()
                {
                    [|M|](out var i);
                }
            }
            """, """
            class C
            {
                void M(out object i) { }
                void Test()
                {
                    M(out var i);
                }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21446")]
    public Task TestInvocation_Indexer_NotSupported()
        => TestMissingAsync("""
            public class C {
                public int this[int i] 
                { 
                    get => 1; 
                    set {} 
                }

                public void Test() {
                    var i = [|this[0,0]|];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29061")]
    public Task TestThis_DoNotOfferToFixTheConstructorWithTheDiagnosticOnIt()
        => TestMissingAsync("""
            public class C {

                public C(): [|this|](1)
                { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29061")]
    public async Task TestThis_Fix_IfACandidateIsAvailable()
    {
        // error CS1729: 'C' does not contain a constructor that takes 2 arguments
        var code =
            """
            class C 
            {
                public C(int i) { }

                public C(): [|this|](1, 1)
                { }
            }
            """;
        await TestInRegularAndScriptAsync(code, """
            class C 
            {
                public C(int i, int v) { }

                public C(): this(1, 1)
                { }
            }
            """, index: 0);
        await TestActionCountAsync(code, 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29061")]
    public async Task TestBase_Fix_IfACandidateIsAvailable()
    {
        // error CS1729: 'B' does not contain a constructor that takes 1 arguments
        var code =
            """
            public class B
            {
                B() { }
            }
            public class C : B
            {
                public C(int i) : [|base|](i) { }
            }
            """;
        await TestInRegularAndScriptAsync(code, """
            public class B
            {
                B(int i) { }
            }
            public class C : B
            {
                public C(int i) : base(i) { }
            }
            """, index: 0);
        await TestActionCountAsync(code, 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29753")]
    public Task LocalFunction_AddParameterToLocalFunctionWithOneParameter()
        => TestInRegularAndScriptAsync("""
            class Rsrp
            {
              public void M()
              {
                [|Local|]("ignore this", true);
                void Local(string whatever)
                {

                }
              }
            }
            """, """
            class Rsrp
            {
              public void M()
              {
                Local("ignore this", true);
                void Local(string whatever, bool v)
                {

                }
              }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29752")]
    public Task LocalFunction_AddNamedParameterToLocalFunctionWithOneParameter()
        => TestInRegularAndScriptAsync("""
            class Rsrp
            {
                public void M()
                {
                    Local("ignore this", [|mynewparameter|]: true);
                    void Local(string whatever)
                    {

                    }
                }
            }
            """, """
            class Rsrp
            {
                public void M()
                {
                    Local("ignore this", mynewparameter: true);
                    void Local(string whatever, bool mynewparameter)
                    {

                    }
                }
            }
            """, index: 0);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39270")]
    public Task TestWithArgThatHasImplicitConversionToParamType1()
        => TestInRegularAndScriptAsync(
            """
            class BaseClass { }

            class MyClass : BaseClass
            {
                void TestFunc()
                {
                    MyClass param1 = new MyClass();
                    int newparam = 1;

                    [|MyFunc|](param1, newparam);
                }

                void MyFunc(BaseClass param1) { }
            }
            """,
            """
            class BaseClass { }

            class MyClass : BaseClass
            {
                void TestFunc()
                {
                    MyClass param1 = new MyClass();
                    int newparam = 1;

                    MyFunc(param1, newparam);
                }

                void MyFunc(BaseClass param1, int newparam) { }
            }
            """);

    [Fact]
    public Task TestOnExtensionGetEnumerator()
        => TestInRegularAndScriptAsync("""
            using System.Collections.Generic;
            namespace N {
            static class Extensions
            {
                public static IEnumerator<int> GetEnumerator(this object o)
                {
                }
            }
            class C1
            {
                void M1()
                {
                    new object().[|GetEnumerator|](1);
                    foreach (var a in new object());
                }
            }}
            """, """
            using System.Collections.Generic;
            namespace N {
            static class Extensions
            {
                public static IEnumerator<int> GetEnumerator(this object o, int v)
                {
                }
            }
            class C1
            {
                void M1()
                {
                    new object().GetEnumerator(1);
                    foreach (var a in new object());
                }
            }}
            """);

    [Fact]
    public async Task TestOnExtensionGetAsyncEnumerator()
    {
        var code =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            namespace N {
            static class Extensions
            {
                public static IAsyncEnumerator<int> GetAsyncEnumerator(this object o)
                {
                }
            }
            class C1
            {
                async Task M1()
                {
                    new object().[|GetAsyncEnumerator|](1);
                    await foreach (var a in new object());
                }
            }}
            """ + IAsyncEnumerable;
        var fix =
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            namespace N {
            static class Extensions
            {
                public static IAsyncEnumerator<int> GetAsyncEnumerator(this object o, int v)
                {
                }
            }
            class C1
            {
                async Task M1()
                {
                    new object().GetAsyncEnumerator(1);
                    await foreach (var a in new object());
                }
            }}
            """ + IAsyncEnumerable;
        await TestInRegularAndScriptAsync(code, fix);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44271")]
    public Task TopLevelStatement()
        => TestInRegularAndScriptAsync("""
            [|local|](1, 2, 3);

            void local(int x, int y)
            {
            }
            """,
            """
            [|local|](1, 2, 3);

            void local(int x, int y, int v)
            {
            }
            """, new(parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44271")]
    public Task TopLevelStatement_Nested()
        => TestInRegularAndScriptAsync("""
            void outer()
            {
                [|local|](1, 2, 3);

                void local(int x, int y)
                {
                }
            }
            """,
            """
            void outer()
            {
                local(1, 2, 3);

                void local(int x, int y, int v)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42559")]
    public Task TestAddParameter_ImplicitObjectCreation()
        => TestInRegularAndScriptAsync("""
            class C
            {
                C(int i) { }

                void M()
                {
                   C c = [||]new(1, 2);
                }
            }
            """,
            """
            class C
            {
                C(int i, int v) { }

                void M()
                {
                   C c = new(1, 2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48042")]
    public Task TestNamedArgOnExtensionMethod()
        => TestInRegularAndScriptAsync(
            """
            namespace r
            {
                static class AbcExtensions
                {
                    public static Abc Act(this Abc state, bool p = true) => state;
                }
                class Abc {
                    void Test()
                        => new Abc().Act([|param3|]: 123);
                }
            }
            """,
            """
            namespace r
            {
                static class AbcExtensions
                {
                    public static Abc Act(this Abc state, bool p = true, int param3 = 0) => state;
                }
                class Abc {
                    void Test()
                        => new Abc().Act(param3: 123);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54408")]
    public Task TestPositionalRecord()
        => TestInRegularAndScriptAsync("""
            var b = "B";
            var r = [|new R(1, b)|];

            record R(int A);

            namespace System.Runtime.CompilerServices
            {
                public static class IsExternalInit { }
            }
            """, """
            var b = "B";
            var r = new R(1, b);

            record R(int A, string b);

            namespace System.Runtime.CompilerServices
            {
                public static class IsExternalInit { }
            }
            """, new(parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact]
    public Task Test_PrimaryConstructor_Class()
        => TestInRegularAndScriptAsync("""
            var b = "B";
            var r = [|new R(1, b)|];

            class R(int A);
            """, """
            var b = "B";
            var r = new R(1, b);

            class R(int A, string b);
            """, new(parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp12)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54408")]
    public Task TestPositionalRecordStruct()
        => TestInRegularAndScriptAsync("""
            var b = "B";
            var r = [|new R(1, b)|];

            record struct R(int A);

            namespace System.Runtime.CompilerServices
            {
                public static class IsExternalInit { }
            }
            """, """
            var b = "B";
            var r = new R(1, b);

            record struct R(int A, string b);

            namespace System.Runtime.CompilerServices
            {
                public static class IsExternalInit { }
            }
            """, new(parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact]
    public Task Test_PrimaryConstructor_Struct()
        => TestInRegularAndScriptAsync("""
            var b = "B";
            var r = [|new R(1, b)|];

            struct R(int A);
            """, """
            var b = "B";
            var r = new R(1, b);

            struct R(int A, string b);
            """, new(parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp12)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56952")]
    public Task TestRecordsNamingConventions()
        => TestInRegularAndScriptAsync("""
            [|new Test("repro")|];

            record Test();

            """, """
            new Test("repro");

            record Test(string V);

            """);

    [Fact]
    public Task TestNamingConventions_PrimaryConstructor_Class()
        => TestInRegularAndScriptAsync("""
            [|new Test("repro")|];

            class Test();
            """, """
            new Test("repro");

            class Test(string v);
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56952")]
    public Task TestRecordsNamingConventions_RecordStruct()
        => TestInRegularAndScriptAsync("""
            [|new Test("repro")|];

            record struct Test();

            """, """
            new Test("repro");

            record struct Test(string V);

            """);

    [Fact]
    public Task TestNamingConventions_PrimaryConstructor_Struct()
        => TestInRegularAndScriptAsync("""
            [|new Test("repro")|];

            struct Test();
            """, """
            new Test("repro");

            struct Test(string v);
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61715")]
    public Task TestMethodGroup1()
        => TestInRegularAndScriptAsync("""
            public class Example
            {
                public void Add(int x)
                {
                }

                public void DoSomething()
                {
                }

                public void Main()
                {
                    [|DoSomething|](Add);
                }
            }
            """, """
            public class Example
            {
                public void Add(int x)
                {
                }

                public void DoSomething(System.Action<int> add)
                {
                }

                public void Main()
                {
                    DoSomething(Add);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61715")]
    public Task TestMethodGroup2()
        => TestInRegularAndScriptAsync("""
            public class Example
            {
                public void Add(int x, string y)
                {
                }

                public void DoSomething()
                {
                }

                public void Main()
                {
                    [|DoSomething|](Add);
                }
            }
            """, """
            public class Example
            {
                public void Add(int x, string y)
                {
                }

                public void DoSomething(System.Action<int, string> add)
                {
                }

                public void Main()
                {
                    DoSomething(Add);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61715")]
    public Task TestMethodGroup3()
        => TestInRegularAndScriptAsync("""
            public class Example
            {
                public int Add(int x, string y)
                {
                    return 0;
                }

                public void DoSomething()
                {
                }

                public void Main()
                {
                    [|DoSomething|](Add);
                }
            }
            """, """
            public class Example
            {
                public int Add(int x, string y)
                {
                    return 0;
                }

                public void DoSomething(System.Func<int, string, int> add)
                {
                }

                public void Main()
                {
                    DoSomething(Add);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71428")]
    public Task TestAddConstructorParameterWithExistingField_BlockInitialize()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;
                private readonly string t;
                private readonly int i;

                public C(string s, int i)
                {
                    this.s = s;
                    this.i = i;
                }
            }

            class D
            {
                void M(string t)
                {
                    new [|C|]("", t, 0);
                }
            }
            """,
            """
            class C
            {
                private readonly string s;
                private readonly string t;
                private readonly int i;

                public C(string s, string t, int i)
                {
                    this.s = s;
                    this.t = t;
                    this.i = i;
                }
            }
            
            class D
            {
                void M(string t)
                {
                    new C("", t, 0);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71428")]
    public Task TestAddConstructorParameterWithExistingField_ExpressionBodyInitialize()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;
                private readonly string t;

                public C(string s)
                    => this.s = s;
            }

            class D
            {
                void M(string t)
                {
                    new [|C|]("", t);
                }
            }
            """,
            """
            class C
            {
                private readonly string s;
                private readonly string t;

                public C(string s, string t)
                {
                    this.s = s;
                    this.t = t;
                }
            }
            
            class D
            {
                void M(string t)
                {
                    new C("", t);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71428")]
    public Task TestAddConstructorParameterWithExistingField_TupleInitialize()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string s;
                private readonly string t;
                private readonly string i;

                public C(string s, string t)
                {
                    (this.s, this.t) = (s, t);
                }
            }

            class D
            {
                void M(string i)
                {
                    new [|C|]("", "", i);
                }
            }
            """,
            """
            class C
            {
                private readonly string s;
                private readonly string t;
                private readonly string i;

                public C(string s, string t, string i)
                {
                    (this.s, this.t, this.i) = (s, t, i);
                }
            }
            
            class D
            {
                void M(string i)
                {
                    new C("", "", i);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71428")]
    public Task TestAddConstructorParameterWithExistingField_UnderscoreName()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private readonly string _s;

                public C()
                {
                }
            }

            class D
            {
                void M(string s)
                {
                    new [|C|](s);
                }
            }
            """,
            """
            class C
            {
                private readonly string _s;

                public C(string s)
                {
                    _s = s;
                }
            }
            
            class D
            {
                void M(string s)
                {
                    new C(s);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71428")]
    public Task TestAddConstructorParameterWithExistingField_PrimaryConstructor()
        => TestInRegularAndScriptAsync(
            """
            class C()
            {
                private readonly string _name;
            }

            class D
            {
                void M(string name)
                {
                    new [|C|](name);
                }
            }
            """,
            """
            class C(string name)
            {
                private readonly string _name = name;
            }
            
            class D
            {
                void M(string name)
                {
                    new C(name);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71428")]
    public Task TestAddConstructorParameterWithExistingProperty_PrimaryConstructor()
        => TestInRegularAndScriptAsync(
            """
            class C()
            {
                private string Name { get; }
            }

            class D
            {
                void M(string name)
                {
                    new [|C|](name);
                }
            }
            """,
            """
            class C(string name)
            {
                private string Name { get; } = name;
            }
            
            class D
            {
                void M(string name)
                {
                    new C(name);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71428")]
    public Task TestAddConstructorParameterWithExistingThrowingProperty_PrimaryConstructor()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C()
            {
                private string Name => throw new NotImplementedException();
            }

            class D
            {
                void M(string name)
                {
                    new [|C|](name);
                }
            }
            """,
            """
            using System;

            class C(string name)
            {
                private string Name { get; } = name;
            }
            
            class D
            {
                void M(string name)
                {
                    new C(name);
                }
            }
            """);
}
