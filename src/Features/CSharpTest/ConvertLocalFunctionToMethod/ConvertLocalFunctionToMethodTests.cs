// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertLocalFunctionToMethod;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLocalFunctionToMethod
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsConvertLocalFunctionToMethod)]
    public class ConvertLocalFunctionToMethodTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertLocalFunctionToMethodCodeRefactoringProvider();

        [Fact]
        public async Task TestCaptures1()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    static void Use<T>(T a) {}
                    static void Use<T>(ref T a) {}

                    static void LocalFunction() {} // trigger rename

                    void M<T1, T2>(T1 param1, T2 param2)
                        where T1 : struct
                        where T2 : struct
                    {
                        var local1 = 0;
                        var local2 = 0;
                        void [||]LocalFunction()
                        {
                            Use(param1);
                            Use(ref param2);
                            Use(local1);
                            Use(ref local2);
                            Use(this);
                            LocalFunction();
                        }
                        LocalFunction();
                        System.Action x = LocalFunction;
                    }
                }
                """,
                """
                class C
                {
                    static void Use<T>(T a) {}
                    static void Use<T>(ref T a) {}

                    static void LocalFunction() {} // trigger rename

                    void M<T1, T2>(T1 param1, T2 param2)
                        where T1 : struct
                        where T2 : struct
                    {
                        var local1 = 0;
                        var local2 = 0;
                        LocalFunction1(param1, ref param2, local1, ref local2);
                        System.Action x = () => LocalFunction1(param1, ref param2, local1, ref local2);
                    }

                    private void LocalFunction1<T1, T2>(T1 param1, ref T2 param2, int local1, ref int local2)
                        where T1 : struct
                        where T2 : struct
                    {
                        Use(param1);
                        Use(ref param2);
                        Use(local1);
                        Use(ref local2);
                        Use(this);
                        LocalFunction1(param1, ref param2, local1, ref local2);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestCaptures2()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    public static void M()
                    {
                        var s = default(S);
                        SetValue(3);
                        void [||]SetValue(int value) => s.Value = value;
                    }
                }
                struct S
                {
                    public int Value;
                }
                """,
                """
                class C
                {
                    public static void M()
                    {
                        var s = default(S);
                        SetValue(3, ref s);
                    }

                    private static void SetValue(int value, ref S s) => s.Value = value;
                }
                struct S
                {
                    public int Value;
                }
                """);
        }

        [Fact]
        public async Task TestCaptures3()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    public static void M()
                    {
                        LocalFunction(3);
                        void [||]LocalFunction(int value)
                        {
                            System.Func<int> x = () => value;
                        }
                    }
                }
                """,
                """
                class C
                {
                    public static void M()
                    {
                        LocalFunction(3);
                    }

                    private static void LocalFunction(int value)
                    {
                        System.Func<int> x = () => value;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestCaptures4()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    public static void M(int i, int j)
                    {
                        LocalFunction1();
                        int [||]LocalFunction1() => i;
                        int LocalFunction2() => j;
                    }
                }
                """,
                """
                class C
                {
                    public static void M(int i, int j)
                    {
                        LocalFunction1(i);
                        int LocalFunction2() => j;
                    }

                    private static int LocalFunction1(int i) => i;
                }
                """);
        }

        [Fact]
        public async Task TestCaptures5()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    public static void M()
                    {
                        LocalFunction();
                        void [||]LocalFunction()
                        {
                            int value = 123;
                            System.Func<int> x = () => value;
                        }
                    }
                }
                """,
                """
                class C
                {
                    public static void M()
                    {
                        LocalFunction();
                    }

                    private static void LocalFunction()
                    {
                        int value = 123;
                        System.Func<int> x = () => value;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestTypeParameters1()
        {
            await TestInRegularAndScriptAsync(
                """
                class C<T0>
                {
                    static void LocalFunction() {} // trigger rename

                    void M<T1, T2>(int i)
                        where T1 : struct
                    {
                        void Local1<T3, T4>()
                            where T4 : struct
                        {
                            void [||]LocalFunction<T5, T6>(T5 a, T6 b)
                                where T5 : struct
                            {
                                _ = typeof(T2);
                                _ = typeof(T4);
                                LocalFunction(a, b);
                                System.Action<T5, T6> x = LocalFunction;
                            }
                            LocalFunction<byte, int>(5, 6);
                            LocalFunction(5, 6);
                        }
                    }
                }
                """,
                """
                class C<T0>
                {
                    static void LocalFunction() {} // trigger rename

                    void M<T1, T2>(int i)
                        where T1 : struct
                    {
                        void Local1<T3, T4>()
                            where T4 : struct
                        {
                            LocalFunction1<T2, T4, byte, int>(5, 6);
                            LocalFunction1<T2, T4, int, int>(5, 6);
                        }
                    }

                    private static void LocalFunction1<T2, T4, T5, T6>(T5 a, T6 b)
                        where T4 : struct
                        where T5 : struct
                    {
                        _ = typeof(T2);
                        _ = typeof(T4);
                        LocalFunction1<T2, T4, T5, T6>(a, b);
                        System.Action<T5, T6> x = (T5 a1, T6 b1) => LocalFunction1<T2, T4, T5, T6>(a1, b1);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestTypeParameters2()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(int i)
                    {
                        int [||]LocalFunction<T1, T2>(T1 a, T2 b) => i;
                        LocalFunction(2, 3);
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        LocalFunction(2, 3, i);
                    }

                    private static int LocalFunction<T1, T2>(T1 a, T2 b, int i) => i;
                }
                """);
        }

        [Fact]
        public async Task TestNameConflict()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void LocalFunction() {} // trigger rename

                    void M()
                    {
                        void [||]LocalFunction() => M();
                        LocalFunction();
                        System.Action x = LocalFunction;
                    }
                }
                """,
                """
                class C
                {
                    void LocalFunction() {} // trigger rename

                    void M()
                    {
                        LocalFunction1();
                        System.Action x = LocalFunction1;
                    }

                    private void LocalFunction1() => M();
                }
                """);
        }

        [Fact]
        public async Task TestNamedArguments1()
        {
            await TestAsync(
                """
                class C
                {
                    void LocalFunction() {} // trigger rename

                    void M()
                    {
                        int var = 2;
                        int [||]LocalFunction(int i)
                        {
                            return var;
                        }
                        LocalFunction(i: 0);
                    }
                }
                """,
                """
                class C
                {
                    void LocalFunction() {} // trigger rename

                    void M()
                    {
                        int var = 2;
                        LocalFunction1(i: 0, var);
                    }

                    private static int LocalFunction1(int i, int var)
                    {
                        return var;
                    }
                }
                """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2));
        }

        [Fact]
        public async Task TestNamedArguments2()
        {
            await TestAsync(
                """
                class C
                {
                    void LocalFunction() {} // trigger rename

                    void M()
                    {
                        int var = 2;
                        int [||]LocalFunction(int i)
                        {
                            return var;
                        }
                        LocalFunction(i: 0);
                    }
                }
                """,
                """
                class C
                {
                    void LocalFunction() {} // trigger rename

                    void M()
                    {
                        int var = 2;
                        LocalFunction1(i: 0, var: var);
                    }

                    private static int LocalFunction1(int i, int var)
                    {
                        return var;
                    }
                }
                """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7));
        }

        [Fact]
        public async Task TestDelegate1()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void LocalFunction() {} // trigger rename

                    void M(int i)
                    {
                        int [||]LocalFunction() => i;
                        System.Func<int> x = LocalFunction;
                    }
                }
                """,
                """
                class C
                {
                    void LocalFunction() {} // trigger rename

                    void M(int i)
                    {
                        System.Func<int> x = () => LocalFunction1(i);
                    }

                    private static int LocalFunction1(int i) => i;
                }
                """);
        }

        [Fact]
        public async Task TestDelegate2()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void LocalFunction() {} // trigger rename
                    delegate int D(int a, ref string b);
                    void M(int i, int j)
                    {
                        int [||]LocalFunction(int a, ref string b) => i = j;
                        var x = (D)LocalFunction;
                    }
                }
                """,
                """
                class C
                {
                    void LocalFunction() {} // trigger rename
                    delegate int D(int a, ref string b);
                    void M(int i, int j)
                    {
                        var x = (D)((int a, ref string b) => LocalFunction1(a, ref b, ref i, j));
                    }

                    private static int LocalFunction1(int a, ref string b, ref int i, int j) => i = j;
                }
                """);
        }

        [Fact]
        public async Task TestAsyncFunction1()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                class C
                {
                    void M<T>(Func<CancellationToken, Task<T>> func) {}
                    void M<T>(Task<T> task)
                    {
                        async Task<T> [||]LocalFunction(CancellationToken c)
                        {
                            return await task;
                        }
                        M(LocalFunction);
                    }
                }
                """,
                """
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                class C
                {
                    void M<T>(Func<CancellationToken, Task<T>> func) {}
                    void M<T>(Task<T> task)
                    {
                        M(c => LocalFunction(c, task));
                    }

                    private static async Task<T> LocalFunction<T>(CancellationToken c, Task<T> task)
                    {
                        return await task;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAsyncFunction2()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                class C
                {
                    void M(Action<CancellationToken> func) {}
                    void M<T>(Task<T> task)
                    {
                        async void [||]LocalFunction(CancellationToken c)
                        {
                            await task;
                        }
                        M(LocalFunction);
                    }
                }
                """,
                """
                using System;
                using System.Threading;
                using System.Threading.Tasks;
                class C
                {
                    void M(Action<CancellationToken> func) {}
                    void M<T>(Task<T> task)
                    {
                        M(c => LocalFunction(c, task));
                    }

                    private static async void LocalFunction<T>(CancellationToken c, Task<T> task)
                    {
                        await task;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestCaretPositon()
        {
            await TestAsync("C [||]LocalFunction(C c)");
            await TestAsync("C Local[||]Function(C c)");
            await TestAsync("C [|LocalFunction|](C c)");
            await TestAsync("C LocalFunction[||](C c)");
            await TestAsync("C Local[|Function|](C c)");
            await TestAsync("[||]C LocalFunction(C c)");
            await TestAsync("C[||] LocalFunction(C c)");
            await TestAsync("C LocalFunction([||]C c)");
            await TestAsync("C LocalFunction(C [||]c)");
            await TestMissingAsync("[|C|] LocalFunction(C c)");
            await TestMissingAsync("C[| |]LocalFunction(C c)");
            await TestMissingAsync("C LocalFunction([|C c|])");

            async Task TestAsync(string signature)
            {
                await TestInRegularAndScriptAsync(
$@"class C
{{
    void M()
    {{
        {signature}
        {{
            return null;
        }}
    }}
}}",
"""
class C
{
    void M()
    {
    }

    private static C LocalFunction(C c)
    {
        return null;
    }
}
""");
            }

            async Task TestMissingAsync(string signature)
            {
                await this.TestMissingAsync(
$@"class C
{{
    void M()
    {{
        {signature}
        {{
            return null;
        }}
    }}
}}");
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection1()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        [|C LocalFunction(C c)
                        {
                            return null;
                        }|]
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                    }

                    private static C LocalFunction(C c)
                    {
                        return null;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection2()
        {

            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        C LocalFunction(C c)[|
                        {
                            return null;
                        }|]
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection3()
        {
            await TestInRegularAndScriptAsync(

                """
                class C
                {
                    void M()
                    {
                [|
                        C LocalFunction(C c)
                        {
                            return null;
                        }
                        |]
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        
                    }

                    private static C LocalFunction(C c)
                    {
                        return null;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection4()
        {

            await this.TestMissingAsync(
    """
    class C
    {
        void M()
        {

            object a = null[|;
            C LocalFunction(C c)
            {      
                return null;

            }|]
        }
    }
    """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection5()
        {

            await this.TestMissingAsync(
    """
    class C
    {
        void M()
        {

            [|
            C LocalFunction(C c)
            {      
                return null;

            }
            object|] a = null
        }
    }
    """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection6()
        {

            await this.TestMissingAsync(
    """
    class C
    {
        void M()
        {
            C LocalFunction(C c)
            {
                object b = null;
                [|
                object a = null;
                return null;
                |]

            }
        }
    }
    """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection7()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        C LocalFunction(C c)
                        {
                            [|return null;|]
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection8()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        [|C LocalFunction(C c)|]
                        {
                            return null;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                    }

                    private static C LocalFunction(C c)
                    {
                        return null;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection9()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        C LocalFunction(C c)
                        {
                            return null;
                        }[||]
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                    }

                    private static C LocalFunction(C c)
                    {
                        return null;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection10()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        [||]C LocalFunction(C c)
                        {
                            return null;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                    }

                    private static C LocalFunction(C c)
                    {
                        return null;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task TestMethodBlockSelection11()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        object a = null;[||]
                        C LocalFunction(C c)
                        {
                            return null;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32976")]
        public async Task TestUnsafeLocalFunction()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    public unsafe void UnsafeFunction()
                    {
                        byte b = 1;
                        [|unsafe byte* GetPtr(byte* bytePt)
                        {
                            return bytePt;
                        }|]
                        var aReference = GetPtr(&b);
                    }
                }
                """,
                """
                class C
                {
                    public unsafe void UnsafeFunction()
                    {
                        byte b = 1;
                        var aReference = GetPtr(&b);
                    }

                    private static unsafe byte* GetPtr(byte* bytePt)
                    {
                        return bytePt;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32976")]
        public async Task TestUnsafeLocalFunctionInUnsafeMethod()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    public unsafe void UnsafeFunction()
                    {
                        byte b = 1;
                        [|byte* GetPtr(byte* bytePt)
                        {
                            return bytePt;
                        }|]
                        var aReference = GetPtr(&b);
                    }
                }
                """,
                """
                class C
                {
                    public unsafe void UnsafeFunction()
                    {
                        byte b = 1;
                        var aReference = GetPtr(&b);
                    }

                    private static unsafe byte* GetPtr(byte* bytePt)
                    {
                        return bytePt;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32976")]
        public async Task TestLocalFunctionInUnsafeMethod()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    public unsafe void UnsafeFunction()
                    {
                        byte b = 1;
                        [|byte GetPtr(byte bytePt)
                        {
                            return bytePt;
                        }|]
                        var aReference = GetPtr(b);
                    }
                }
                """,
                """
                class C
                {
                    public unsafe void UnsafeFunction()
                    {
                        byte b = 1;
                        var aReference = GetPtr(b);
                    }

                    private static byte GetPtr(byte bytePt)
                    {
                        return bytePt;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestTopLevelStatements()
        {
            await TestMissingAsync("""
                Console.WriteLine("Hello");
                {
                    public static int [|Add|](int x, int y)
                    {
                        return x + y;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32975")]
        public async Task TestRefReturn()
        {
            await TestInRegularAndScript1Async("""
                class ClassA
                {
                    class RefClass { }
                    RefClass refClass = new RefClass();
                    public void RefLocalFunction()
                    {
                        ref RefClass [|GetRef|]()
                        {
                            return ref refClass;
                        }
                        ref var aReference = ref GetRef();
                    }
                }
                """, """
                class ClassA
                {
                    class RefClass { }
                    RefClass refClass = new RefClass();
                    public void RefLocalFunction()
                    {
                        ref var aReference = ref GetRef();
                    }

                    private ref RefClass GetRef()
                    {
                        return ref refClass;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32975")]
        public async Task TestAttributes()
        {
            await TestInRegularAndScript1Async("""
                class ClassA
                {
                    class RefClass { }
                    RefClass refClass = new RefClass();
                    public void RefLocalFunction()
                    {
                        [System.STAThread]
                        ref RefClass [|GetRef|]()
                        {
                            return ref refClass;
                        }
                        ref var aReference = ref GetRef();
                    }
                }
                """, """
                class ClassA
                {
                    class RefClass { }
                    RefClass refClass = new RefClass();
                    public void RefLocalFunction()
                    {
                        ref var aReference = ref GetRef();
                    }

                    [System.STAThread]
                    private ref RefClass GetRef()
                    {
                        return ref refClass;
                    }
                }
                """);
        }
    }
}
