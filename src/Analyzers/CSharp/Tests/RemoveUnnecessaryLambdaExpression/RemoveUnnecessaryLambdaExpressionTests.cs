// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryLambdaExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryLambdaExpression;

using VerifyCS = CSharpCodeFixVerifier<
   CSharpRemoveUnnecessaryLambdaExpressionDiagnosticAnalyzer,
   CSharpRemoveUnnecessaryLambdaExpressionCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
public class RemoveUnnecessaryLambdaExpressionTests
{
    private static async Task TestInRegularAndScriptAsync(
        string testCode,
        string fixedCode,
        LanguageVersion version = LanguageVersion.CSharp12,
        OutputKind? outputKind = null)
    {
        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = version,
            TestState =
            {
                OutputKind = outputKind,
            }
        }.RunAsync();
    }

    private static Task TestMissingInRegularAndScriptAsync(
        string testCode,
        LanguageVersion version = LanguageVersion.CSharp12,
        OutputKind? outputKind = null)
        => TestInRegularAndScriptAsync(testCode, testCode, version, outputKind);

    [Fact]
    public async Task TestMissingInCSharp10()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(s => Quux(s));
                }

                void Bar(Func<int, string> f) { }
                string Quux(int i) => default;
            }
            """, LanguageVersion.CSharp10);
    }

    [Fact]
    public async Task TestBasicCase()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar([|s => |]Quux(s));
                }

                void Bar(Func<int, string> f) { }
                string Quux(int i) => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, string> f) { }
                string Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestWithOptionOff()
    {
        var code = """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(s => Quux(s));
                }

                void Bar(Func<int, string> f) { }
                string Quux(int i) => default;
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp12,
            Options = { { CSharpCodeStyleOptions.PreferMethodGroupConversion, new CodeStyleOption2<bool>(false, NotificationOption2.None) } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotOnStaticLambda()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(static s => Quux(s));
                }

                void Bar(Func<int, string> f) { }
                static string Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestNotWithOptionalParameter()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(s => Quux(s));
                }

                void Bar(Func<int, string> f) { }
                static string Quux(int i, int j = 0) => default;
            }
            """);
    }

    [Fact]
    public async Task TestNotWithParams1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(s => Quux(s));
                }

                void Bar(Func<int, string> f) { }
                static string Quux(int i, params int[] j) => default;
            }
            """);
    }

    [Fact]
    public async Task TestNotWithParams2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(s => Quux(s));
                }

                void Bar(Func<object, string> f) { }
                static string Quux(params object[] j) => default;
            }
            """);
    }

    [Fact]
    public async Task TestWithParams1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar([|s => |]Quux(s));
                }

                void Bar(Func<object[], string> f) { }
                string Quux(params object[] o) => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<object[], string> f) { }
                string Quux(params object[] o) => default;
            }
            """);
    }

    [Fact]
    public async Task TestNotWithRefChange1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(s => Quux(ref s));
                }

                void Bar(Func<int, string> f) { }
                static string Quux(ref int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestNotWithRefChange2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            delegate string X(ref int i);

            class C
            {
                void Goo()
                {
                    Bar((ref int s) => Quux(s));
                }

                void Bar(X x) { }
                static string Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestWithSameRef()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            delegate string X(ref int i);

            class C
            {
                void Goo()
                {
                    Bar([|(ref int s) => |]Quux(ref s));
                }

                void Bar(X x) { }
                static string Quux(ref int i) => default;
            }
            """,

            """
            using System;

            delegate string X(ref int i);

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(X x) { }
                static string Quux(ref int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestNotOnConversionToObject()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    object o = (int s) => Quux(s);
                }

                void Bar(Func<int, string> f) { }
                static string Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestWithParenthesizedLambda()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar([|(int s) => |]Quux(s));
                }

                void Bar(Func<int, string> f) { }
                string Quux(int i) => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, string> f) { }
                string Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestWithAnonymousMethod()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar([|delegate (int s) { return |]Quux(s); });
                }

                void Bar(Func<int, string> f) { }
                string Quux(int i) => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, string> f) { }
                string Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestWithAnonymousMethodNoParameterList()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar([|delegate { return |]Quux(); });
                }

                void Bar(Func<string> f) { }
                string Quux() => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<string> f) { }
                string Quux() => default;
            }
            """);
    }

    [Fact]
    public async Task TestFixCoContravariance1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar([|s => |]Quux(s));
                }

                void Bar(Func<object, string> f) { }
                string Quux(object o) => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<object, string> f) { }
                string Quux(object o) => default;
            }
            """);
    }

    [Fact]
    public async Task TestFixCoContravariance2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar([|s => |]Quux(s));
                }

                void Bar(Func<string, object> f) { }
                string Quux(object o) => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<string, object> f) { }
                string Quux(object o) => default;
            }
            """);
    }

    [Fact]
    public async Task TestFixCoContravariance3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(s => {|CS1662:{|CS0266:Quux(s)|}|});
                }

                void Bar(Func<string, string> f) { }
                object Quux(object o) => default;
            }
            """);
    }

    [Fact]
    public async Task TestFixCoContravariance4()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(s => Quux({|CS1503:s|}));
                }

                void Bar(Func<object, object> f) { }
                string Quux(string o) => default;
            }
            """);
    }

    [Fact]
    public async Task TestFixCoContravariance5()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(s => Quux({|CS1503:s|}));
                }

                void Bar(Func<object, string> f) { }
                object Quux(string o) => default;
            }
            """);
    }

    [Fact]
    public async Task TestTwoArgs()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar([|(s1, s2) => |]Quux(s1, s2));
                }

                void Bar(Func<int, bool, string> f) { }
                string Quux(int i, bool b) => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, bool, string> f) { }
                string Quux(int i, bool b) => default;
            }
            """);
    }

    [Fact]
    public async Task TestMultipleArgIncorrectPassing1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar((s1, s2) => Quux(s2, s1));
                }

                void Bar(Func<int, int, string> f) { }
                string Quux(int i, int b) => default;
            }
            """);
    }

    [Fact]
    public async Task TestMultipleArgIncorrectPassing2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar((s1, s2) => Quux(s1, s1));
                }

                void Bar(Func<int, int, string> f) { }
                string Quux(int i, int b) => default;
            }
            """);
    }

    [Fact]
    public async Task TestMultipleArgIncorrectPassing3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar((s1, s2) => Quux(s1, true));
                }

                void Bar(Func<int, bool, string> f) { }
                string Quux(int i, bool b) => default;
            }
            """);
    }

    [Fact]
    public async Task TestReturnStatement()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar([|(s1, s2) => {
                        return |]Quux(s1, s2);
                    });
                }

                void Bar(Func<int, bool, string> f) { }
                string Quux(int i, bool b) => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, bool, string> f) { }
                string Quux(int i, bool b) => default;
            }
            """);
    }

    [Fact]
    public async Task TestReturnStatement2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar([|(s1, s2) => {
                        return |]this.Quux(s1, s2);
                    });
                }

                void Bar(Func<int, bool, string> f) { }
                string Quux(int i, bool b) => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(this.Quux);
                }

                void Bar(Func<int, bool, string> f) { }
                string Quux(int i, bool b) => default;
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542562")]
    public async Task TestMissingOnAmbiguity1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class A
            {
                static void Goo<T>(T x)
                {
                }

                static void Bar(Action<int> x)
                {
                }

                static void Bar(Action<string> x)
                {
                }

                static void Main()
                {
                    {|CS0121:Bar|}(x => Goo(x));
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542562")]
    public async Task TestWithConstraint1()
    {
        var code = """
            using System;
            class A
            {
                static void Goo<T>(T x) where T : class
                {
                }

                static void Bar(Action<int> x)
                {
                }

                static void Bar(Action<string> x)
                {
                }

                static void Main()
                {
                    Bar([|x => |]Goo<string>(x));
                }
            }
            """;

        var expected = """
            using System;
            class A
            {
                static void Goo<T>(T x) where T : class
                {
                }

                static void Bar(Action<int> x)
                {
                }

                static void Bar(Action<string> x)
                {
                }

                static void Main()
                {
                    Bar(Goo<string>);
                }
            }
            """;
        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542562")]
    public async Task TestWithConstraint2()
    {
        var code = """
            using System;
            class A
            {
                static void Goo<T>(T x) where T : class
                {
                }

                static void Bar(Action<int> x)
                {
                }

                static void Bar(Action<string> x)
                {
                }

                static void Main()
                {
                    Bar(x => Goo(x));
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627092")]
    public async Task TestMissingOnLambdaWithDynamic_1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    C<string>.InvokeGoo();
                }
            }

            class C<T>
            {
                public static void InvokeGoo()
                {
                    Action<dynamic, string> goo = (x, y) => C<T>.Goo(x, y); // Simplify lambda expression
                    goo(1, "");
                }

                static void Goo(object x, object y)
                {
                    Console.WriteLine("Goo(object x, object y)");
                }

                static void Goo(object x, T y)
                {
                    Console.WriteLine("Goo(object x, T y)");
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627092")]
    public async Task TestWithLambdaWithDynamic()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    C<string>.InvokeGoo();
                }
            }

            class C<T>
            {
                public static void InvokeGoo()
                {
                    Action<dynamic> goo = [|x => |]C<T>.Goo(x); // Simplify lambda expression
                    goo(1);
                }

                private static void Goo(dynamic x)
                {
                    throw new NotImplementedException();
                }

                static void Goo(object x, object y)
                {
                    Console.WriteLine("Goo(object x, object y)");
                }

                static void Goo(object x, T y)
                {
                    Console.WriteLine("Goo(object x, T y)");
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    C<string>.InvokeGoo();
                }
            }

            class C<T>
            {
                public static void InvokeGoo()
                {
                    Action<dynamic> goo = C<T>.Goo; // Simplify lambda expression
                    goo(1);
                }

                private static void Goo(dynamic x)
                {
                    throw new NotImplementedException();
                }

                static void Goo(object x, object y)
                {
                    Console.WriteLine("Goo(object x, object y)");
                }

                static void Goo(object x, T y)
                {
                    Console.WriteLine("Goo(object x, T y)");
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544625")]
    public async Task ParenthesizeIfParseChanges()
    {
        var code = """
            using System;
            class C
            {
                static void M()
                {
                    C x = new C();
                    int y = 1;
                    Bar([|() => { return |]Console.ReadLine(); } < x, y > (1 + 2));
                }

                static void Bar(object a, object b) { }
                public static bool operator <(Func<string> y, C x) { return true; }
                public static bool operator >(Func<string> y, C x) { return true; }
            }
            """;

        var expected = """
            using System;
            class C
            {
                static void M()
                {
                    C x = new C();
                    int y = 1;
                    Bar((Console.ReadLine) < x, y > (1 + 2));
                }

                static void Bar(object a, object b) { }
                public static bool operator <(Func<string> y, C x) { return true; }
                public static bool operator >(Func<string> y, C x) { return true; }
            }
            """;

        await TestInRegularAndScriptAsync(code, expected);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545856")]
    public async Task TestNotWithSideEffects()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Main()
                {
                    Func<string> a = () => new C().ToString();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545994")]
    public async Task TestExpressionStatement()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action a = [|() => {
                        |]Console.WriteLine();
                    };
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action a = Console.WriteLine;
                }
            }
            """);
    }

    [Fact]
    public async Task TestTaskOfT1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|s => |]Quux(s));
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncTaskOfT1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|async s => await |]Quux(s));
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncTaskOfT2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|async s => await |]Quux(s).ConfigureAwait(false));
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncNoAwait1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(async s => Quux(s));
                }

                void Bar(Func<int, Task<string>> f) { }
                string Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestTaskOfT1_Return()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|s => { return |]Quux(s); });
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncTaskOfT1_Return()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|async s => { return await |]Quux(s); });
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncTaskOfT2_Return()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|async s => { return await |]Quux(s).ConfigureAwait(false); });
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task<string>> f) { }
                Task<string> Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncNoAwait1_Return()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(async s => { return Quux(s); });
                }

                void Bar(Func<int, Task<string>> f) { }
                string Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestTask1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|s => |]Quux(s));
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncTask1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|async s => await |]Quux(s));
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncTask2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|async s => await |]Quux(s).ConfigureAwait(false));
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestTask1_ExpressionStatement()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(s {|CS1643:=>|} { Quux(s); });
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncTask1_ExpressionStatement()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|async s => { await |]Quux(s); });
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncTask2_ExpressionStatement()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar([|async s => { await |]Quux(s).ConfigureAwait(false); });
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(Quux);
                }

                void Bar(Func<int, Task> f) { }
                Task Quux(int i) => default;
            }
            """);
    }

    [Fact]
    public async Task TestAsyncNoAwait1_ExpressionStatement()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Bar(async s => { Quux(s); });
                }

                void Bar(Func<int, Task> f) { }
                void Quux(int i) { }
            }
            """);
    }

    [Fact]
    public async Task TestExplicitGenericCall()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action a = [|() => |]Quux<int>();
                }

                void Quux<T>() { }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action a = Quux<int>;
                }

                void Quux<T>() { }
            }
            """);
    }

    [Fact]
    public async Task TestImplicitGenericCall()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> a = b => Quux(b);
                }

                void Quux<T>(T t) { }
            }
            """);
    }

    [Fact]
    public async Task TestNullabilityChanges()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            #nullable enable

            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                void Goo(List<string> assemblies, HashSet<string> usedProjectFileNames)
                {
                    var projectAssemblyFileNames = Select(assemblies, a => GetFileName(a));
                    var v = Any(projectAssemblyFileNames, usedProjectFileNames.Contains);
                }

                static List<TResult> Select<TItem, TResult>(List<TItem> items, Func<TItem, TResult> map) => new();

                [return: NotNullIfNotNull("path")]
                static string? GetFileName(string? path) => path;

                static bool Any<T>(List<T> immutableArray, Func<T, bool> predicate) => true;
            }

            namespace System.Diagnostics.CodeAnalysis
            {
                [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
                public sealed class NotNullIfNotNullAttribute : Attribute
                {
                    public string ParameterName => "";

                    public NotNullIfNotNullAttribute(string parameterName)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestTrivia1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(/*before*/[|s => |]Quux(s)/*after*/);
                }

                void Bar(Func<int, string> f) { }
                string Quux(int i) => default;
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Bar(/*before*/Quux/*after*/);
                }

                void Bar(Func<int, string> f) { }
                string Quux(int i) => default;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63465")]
    public async Task TestNotWithPartialDefinition()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Diagnostics;

            public partial class C
            {
                internal void M1()
                {
                    M2(x => M3(x));
                }

                partial void M3(string s);

                private static void M2(Action<string> a) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63465")]
    public async Task TestWithPartialDefinitionAndImplementation()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Diagnostics;

            public partial class C
            {
                internal void M1()
                {
                    M2([|x => |]M3(x));
                }

                partial void M3(string s);
                partial void M3(string s) { }

                private static void M2(Action<string> a) { }
            }
            """,
            """
            using System;
            using System.Diagnostics;

            public partial class C
            {
                internal void M1()
                {
                    M2(M3);
                }

                partial void M3(string s);
                partial void M3(string s) { }

                private static void M2(Action<string> a) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63464")]
    public async Task TestNotWithConditionalAttribute()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            using System.Diagnostics;

            public class C
            {
                internal void M1()
                {
                    M2(x => M3(x));
                }

                [Conditional("DEBUG")]
                internal void M3(string s) { }

                private static void M2(Action<string> a) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69094")]
    public async Task TestNotWithAssignmentOfInvokedExpression1()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            TaskCompletionSource<bool> valueSet = new();
            Helper helper = new(v => valueSet.SetResult(v));
            helper.Set(true);
            valueSet = new();
            helper.Set(false);

            class Helper
            {
               private readonly Action<bool> action;
               internal Helper(Action<bool> action)
               {
                 this.action = action;
               }

               internal void Set(bool value) => action(value);
            }
            
            """, outputKind: OutputKind.ConsoleApplication);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69094")]
    public async Task TestWithoutAssignmentOfInvokedExpression1()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            TaskCompletionSource<bool> valueSet = new();
            Helper helper = new([|v => |]valueSet.SetResult(v));
            helper.Set(true);
            helper.Set(false);

            class Helper
            {
               private readonly Action<bool> action;
               internal Helper(Action<bool> action)
               {
                 this.action = action;
               }

               internal void Set(bool value) => action(value);
            }
            
            """, """
            using System;
            using System.Threading.Tasks;

            TaskCompletionSource<bool> valueSet = new();
            Helper helper = new(valueSet.SetResult);
            helper.Set(true);
            helper.Set(false);

            class Helper
            {
               private readonly Action<bool> action;
               internal Helper(Action<bool> action)
               {
                 this.action = action;
               }

               internal void Set(bool value) => action(value);
            }
            
            """,
            outputKind: OutputKind.ConsoleApplication);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69094")]
    public async Task TestNotWithAssignmentOfInvokedExpression2()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    TaskCompletionSource<bool> valueSet = new();
                    Helper helper = new(v => valueSet.SetResult(v));
                    helper.Set(true);
                    valueSet = new();
                    helper.Set(false);
                }
            }

            class Helper
            {
               private readonly Action<bool> action;
               internal Helper(Action<bool> action)
               {
                 this.action = action;
               }

               internal void Set(bool value) => action(value);
            }
            
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69094")]
    public async Task TestWithoutAssignmentOfInvokedExpression2()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    TaskCompletionSource<bool> valueSet = new();
                    Helper helper = new([|v => |]valueSet.SetResult(v));
                    helper.Set(true);
                    helper.Set(false);
                }
            }

            class Helper
            {
               private readonly Action<bool> action;
               internal Helper(Action<bool> action)
               {
                 this.action = action;
               }

               internal void Set(bool value) => action(value);
            }
            
            """, """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    TaskCompletionSource<bool> valueSet = new();
                    Helper helper = new(valueSet.SetResult);
                    helper.Set(true);
                    helper.Set(false);
                }
            }

            class Helper
            {
               private readonly Action<bool> action;
               internal Helper(Action<bool> action)
               {
                 this.action = action;
               }

               internal void Set(bool value) => action(value);
            }
            
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69094")]
    public async Task TestWithoutAssignmentOfInvokedExpression3()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    TaskCompletionSource<bool> valueSet = new();
                    Helper helper = new([|v => |]valueSet.SetResult(v));
                    helper.Set(true);
                    helper.Set(false);

                    var v = () =>
                    {
                        // this is a different local.  it should not impact the outer simplification
                        TaskCompletionSource<bool> valueSet = new();
                        valueSet = new();
                    };
                }
            }

            class Helper
            {
               private readonly Action<bool> action;
               internal Helper(Action<bool> action)
               {
                 this.action = action;
               }

               internal void Set(bool value) => action(value);
            }
            
            """, """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    TaskCompletionSource<bool> valueSet = new();
                    Helper helper = new(valueSet.SetResult);
                    helper.Set(true);
                    helper.Set(false);
            
                    var v = () =>
                    {
                        // this is a different local.  it should not impact the outer simplification
                        TaskCompletionSource<bool> valueSet = new();
                        valueSet = new();
                    };
                }
            }

            class Helper
            {
               private readonly Action<bool> action;
               internal Helper(Action<bool> action)
               {
                 this.action = action;
               }

               internal void Set(bool value) => action(value);
            }
            
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71300")]
    public async Task TestWithWriteInOtherMethod()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            using System.Linq;

            public class Repro
            {
                private readonly MethodProvider _methodProvider;

                public Repro(MethodProvider methodProvider)
                {
                    // Assignment that should not block feature.
                    _methodProvider = methodProvider;
                }

                public void Main()
                {
                    int[] numbers = { 1, 2, 3, 4, 5 };
                    string[] asStrings = numbers.Select([|x => |]_methodProvider.ToStr(x)).ToArray();
                    Console.WriteLine(asStrings.Length);
                }
            }

            public class MethodProvider
            {
                public string ToStr(int x)
                {
                    return x.ToString();
                }
            }
            """,
            """
            using System;
            using System.Linq;

            public class Repro
            {
                private readonly MethodProvider _methodProvider;

                public Repro(MethodProvider methodProvider)
                {
                    // Assignment that should not block feature.
                    _methodProvider = methodProvider;
                }

                public void Main()
                {
                    int[] numbers = { 1, 2, 3, 4, 5 };
                    string[] asStrings = numbers.Select(_methodProvider.ToStr).ToArray();
                    Console.WriteLine(asStrings.Length);
                }
            }

            public class MethodProvider
            {
                public string ToStr(int x)
                {
                    return x.ToString();
                }
            }
            """);
    }
}
