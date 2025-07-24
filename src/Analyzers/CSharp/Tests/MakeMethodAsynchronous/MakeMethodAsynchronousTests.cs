// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.MakeMethodAsynchronous;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeMethodAsynchronous;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodAsynchronous)]
public sealed partial class MakeMethodAsynchronousTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpMakeMethodAsynchronousCodeFixProvider());

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task AwaitInVoidMethodWithModifiers()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class Program
            {
                public static void Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                public static async void Test()
                {
                    await Task.Delay(1);
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26312")]
    public async Task AwaitInTaskMainMethodWithModifiers()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                public static void Main()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """;
        await TestAsync(initial, """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                public static async Task Main()
                {
                    await Task.Delay(1);
                }
            }
            """, new(parseOptions: CSharpParseOptions.Default,
            compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication)));

        // no option offered to keep void
        await TestActionCountAsync(initial, count: 1, new TestParameters(compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication)));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26312")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task AwaitInVoidMainMethodWithModifiers_NotEntryPoint()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class Program
            {
                public void Main()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                public async void Main()
                {
                    await Task.Delay(1);
                }
            }
            """, index: 1);

    [Fact]
    public Task AwaitInVoidMethodWithModifiers2()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class Program 
            {
                public static void Test() 
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class Program 
            {
                public static async Task TestAsync() 
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task AwaitInTaskMethodNoModifiers()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class Program 
            {
                Task Test() 
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class Program 
            {
                async Task Test() 
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task AwaitInTaskMethodWithModifiers()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class Program
            {
                public/*Comment*/static/*Comment*/Task/*Comment*/Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                public/*Comment*/static/*Comment*/async Task/*Comment*/Test()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact]
    public Task AwaitInLambdaFunction()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    Action a = () => Console.WriteLine();
                    Func<Task> b = () => [|await Task.Run(a);|]
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    Action a = () => Console.WriteLine();
                    Func<Task> b = async () => await Task.Run(a);
                }
            }
            """);

    [Fact]
    public Task AwaitInLambdaAction()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    Action a = () => [|await Task.Delay(1);|]
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    Action a = async () => await Task.Delay(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task BadAwaitInNonAsyncMethod()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                void Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async void Test()
                {
                    await Task.Delay(1);
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task BadAwaitInNonAsyncMethod2()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                Task Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task Test()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task BadAwaitInNonAsyncMethod3()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                Task<int> Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> Test()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact]
    public Task BadAwaitInNonAsyncMethod4()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                int Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> TestAsync()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task BadAwaitInNonAsyncMethod5()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                void Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            class Program
            {
                async void Test()
                {
                    await Task.Delay(1);
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task BadAwaitInNonAsyncMethod6()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                Task Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            class Program
            {
                async Task Test()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task BadAwaitInNonAsyncMethod7()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                Task<int> Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            class Program
            {
                async Task<int> Test()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact]
    public Task BadAwaitInNonAsyncMethod8()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                int Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> TestAsync()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact]
    public Task BadAwaitInNonAsyncMethod9()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                Program Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System.Threading.Tasks;

            class Program
            {
                async Task<Program> TestAsync()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact]
    public Task BadAwaitInNonAsyncMethod10()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                asdf Test()
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            class Program
            {
                async System.Threading.Tasks.Task<asdf> TestAsync()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact]
    public async Task BadAwaitInEnumerableMethod()
    {
        var initial =
            """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                IEnumerable<int> Test()
                {
                    yield return 1;
                    [|await Task.Delay(1);|]
                }
            }
            """ + IAsyncEnumerable;

        var expected =
            """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                async IAsyncEnumerable<int> TestAsync()
                {
                    yield return 1;
                    await Task.Delay(1);
                }
            }
            """ + IAsyncEnumerable;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public Task BadAwaitInEnumerableMethodMissingIAsyncEnumerableType()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                IEnumerable<int> Test()
                {
                    yield return 1;
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                async IAsyncEnumerable<int> TestAsync()
                {
                    yield return 1;
                    await Task.Delay(1);
                }
            }
            """);

    [Fact]
    public Task BadAwaitInEnumerableMethodWithReturn()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                IEnumerable<int> Test()
                {
                    [|await Task.Delay(1);|]
                    return null;
                }
            }
            """, """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                async Task<IEnumerable<int>> TestAsync()
                {
                    await Task.Delay(1);
                    return null;
                }
            }
            """);

    [Fact]
    public Task BadAwaitInEnumerableMethodWithYieldInsideLocalFunction()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                IEnumerable<int> Test()
                {
                    [|await Task.Delay(1);|]
                    return local();

                    IEnumerable<int> local()
                    {
                        yield return 1;
                    }
                }
            }
            """, """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                async Task<IEnumerable<int>> TestAsync()
                {
                    await Task.Delay(1);
                    return local();

                    IEnumerable<int> local()
                    {
                        yield return 1;
                    }
                }
            }
            """);

    [Fact]
    public Task BadAwaitInEnumeratorMethodWithReturn()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                IEnumerator<int> Test()
                {
                    [|await Task.Delay(1);|]
                    return null;
                }
            }
            """, """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                async Task<IEnumerator<int>> TestAsync()
                {
                    await Task.Delay(1);
                    return null;
                }
            }
            """);

    [Fact]
    public async Task BadAwaitInEnumeratorMethod()
    {
        var initial =
            """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                IEnumerator<int> Test()
                {
                    yield return 1;
                    [|await Task.Delay(1);|]
                }
            }
            """ + IAsyncEnumerable;

        var expected =
            """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                async IAsyncEnumerator<int> TestAsync()
                {
                    yield return 1;
                    await Task.Delay(1);
                }
            }
            """ + IAsyncEnumerable;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task BadAwaitInEnumeratorLocalFunction()
    {
        var initial =
            """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                void M()
                {
                    IEnumerator<int> Test()
                    {
                        yield return 1;
                        [|await Task.Delay(1);|]
                    }
                }
            }
            """ + IAsyncEnumerable;

        var expected =
            """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                void M()
                {
                    async IAsyncEnumerator<int> TestAsync()
                    {
                        yield return 1;
                        await Task.Delay(1);
                    }
                }
            }
            """ + IAsyncEnumerable;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public async Task BadAwaitInIAsyncEnumerableMethod()
    {
        var initial =
            """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                IAsyncEnumerable<int> Test()
                {
                    yield return 1;
                    [|await Task.Delay(1);|]
                }
            }
            """ + IAsyncEnumerable;

        var expected =
            """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                async IAsyncEnumerable<int> Test()
                {
                    yield return 1;
                    await Task.Delay(1);
                }
            }
            """ + IAsyncEnumerable;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public async Task BadAwaitInIAsyncEnumeratorMethod()
    {
        var initial =
            """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                IAsyncEnumerator<int> Test()
                {
                    yield return 1;
                    [|await Task.Delay(1);|]
                }
            }
            """ + IAsyncEnumerable;

        var expected =
            """
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                async IAsyncEnumerator<int> Test()
                {
                    yield return 1;
                    await Task.Delay(1);
                }
            }
            """ + IAsyncEnumerable;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public Task AwaitInMember()
        => TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            class Program
            {
                var x = [|await Task.Delay(3)|];
            }
            """);

    [Fact]
    public Task AddAsyncInDelegate()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                private async void method()
                {
                    string content = await Task<String>.Run(delegate () {
                        [|await Task.Delay(1000)|];
                        return "Test";
                    });
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                private async void method()
                {
                    string content = await Task<String>.Run(async delegate () {
                        await Task.Delay(1000);
                        return "Test";
                    });
                }
            }
            """);

    [Fact]
    public Task AddAsyncInDelegate2()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                private void method()
                {
                    string content = await Task<String>.Run(delegate () {
                        [|await Task.Delay(1000)|];
                        return "Test";
                    });
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                private void method()
                {
                    string content = await Task<String>.Run(async delegate () {
                        await Task.Delay(1000);
                        return "Test";
                    });
                }
            }
            """);

    [Fact]
    public Task AddAsyncInDelegate3()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                private void method()
                {
                    string content = await Task<String>.Run(delegate () {
                        [|await Task.Delay(1000)|];
                        return "Test";
                    });
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                private void method()
                {
                    string content = await Task<String>.Run(async delegate () {
                        await Task.Delay(1000);
                        return "Test";
                    });
                }
            }
            """);

    [Fact, WorkItem(6477, @"https://github.com/dotnet/roslyn/issues/6477")]
    public Task NullNodeCrash()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                static async void Main()
                {
                    try
                    {
                        [|await|] await Task.Delay(100);
                    }
                    finally
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/17470")]
    public Task AwaitInValueTaskMethod()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            namespace System.Threading.Tasks {
                struct ValueTask<T>
                {
                }
            }

            class Program 
            {
                ValueTask<int> Test() 
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            namespace System.Threading.Tasks {
                struct ValueTask<T>
                {
                }
            }

            class Program 
            {
                async ValueTask<int> Test() 
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact]
    public Task AwaitInValueTaskWithoutGenericMethod()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            namespace System.Threading.Tasks {
                struct ValueTask
                {
                }
            }

            class Program 
            {
                ValueTask Test() 
                {
                    [|await Task.Delay(1);|]
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            namespace System.Threading.Tasks {
                struct ValueTask
                {
                }
            }

            class Program 
            {
                async ValueTask Test() 
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14133")]
    public Task AddAsyncInLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                public void M1()
                {
                    void M2()
                    {
                        [|await M3Async();|]
                    }
                }

                async Task<int> M3Async()
                {
                    return 1;
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                public void M1()
                {
                    async Task M2Async()
                    {
                        await M3Async();
                    }
                }

                async Task<int> M3Async()
                {
                    return 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14133")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task AddAsyncInLocalFunctionKeepVoidReturn()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                public void M1()
                {
                    void M2()
                    {
                        [|await M3Async();|]
                    }
                }

                async Task<int> M3Async()
                {
                    return 1;
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                public void M1()
                {
                    async void M2()
                    {
                        await M3Async();
                    }
                }

                async Task<int> M3Async()
                {
                    return 1;
                }
            }
            """,
            index: 1);

    [Theory]
    [InlineData(0, "void", "Task", "M2Async")]
    [InlineData(1, "void", "void", "M2")]
    [InlineData(0, "int", "Task<int>", "M2Async")]
    [InlineData(0, "Task", "Task", "M2")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18307")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task AddAsyncInLocalFunctionKeepsTrivia(int codeFixIndex, string initialReturn, string expectedReturn, string expectedName)
        => TestInRegularAndScriptAsync(
            $$"""
            using System.Threading.Tasks;

            class C
            {
                public void M1()
                {
                    // Leading trivia
                    /*1*/ {{initialReturn}} /*2*/ M2/*3*/() /*4*/
                    {
                        [|await M3Async();|]
                    }
                }

                async Task<int> M3Async()
                {
                    return 1;
                }
            }
            """,
            $$"""
            using System.Threading.Tasks;

            class C
            {
                public void M1()
                {
                    // Leading trivia
                    /*1*/ async {{expectedReturn}} /*2*/ {{expectedName}}/*3*/() /*4*/
                    {
                        await M3Async();
                    }
                }

                async Task<int> M3Async()
                {
                    return 1;
                }
            }
            """,
            index: codeFixIndex);

    [Theory]
    [InlineData("", 0, "Task", "M2Async")]
    [InlineData("", 1, "void", "M2")]
    [InlineData("public", 0, "Task", "M2Async")]
    [InlineData("public", 1, "void", "M2")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18307")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33082")]
    public Task AddAsyncKeepsTrivia(string modifiers, int codeFixIndex, string expectedReturn, string expectedName)
        => TestInRegularAndScriptAsync(
            $$"""
            using System.Threading.Tasks;

            class C
            {
                // Leading trivia
                {{modifiers}}/*1*/ void /*2*/ M2/*3*/() /*4*/
                {
                    [|await M3Async();|]
                }

                async Task<int> M3Async()
                {
                    return 1;
                }
            }
            """,
            $$"""
            using System.Threading.Tasks;

            class C
            {
                // Leading trivia
                {{modifiers}}/*1*/ async {{expectedReturn}} /*2*/ {{expectedName}}/*3*/() /*4*/
                {
                    await M3Async();
                }

                async Task<int> M3Async()
                {
                    return 1;
                }
            }
            """,
            index: codeFixIndex);

    [Fact]
    public Task MethodWithAwaitUsing()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|await using (var x = new object())|]
                    {
                    }
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                async Task MAsync()
                {
                    await using (var x = new object())
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MethodWithRegularUsing()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|using (var x = new object())|]
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MethodWithAwaitForEach()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|await foreach (var n in new int[] { })|]
                    {
                    }
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                async Task MAsync()
                {
                    await foreach (var n in new int[] { })
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MethodWithRegularForEach()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|foreach (var n in new int[] { })|]
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MethodWithAwaitForEachVariable()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|await foreach (var (a, b) in new(int, int)[] { })|]
                    {
                    }
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                async Task MAsync()
                {
                    await foreach (var (a, b) in new(int, int)[] { })
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MethodWithRegularForEachVariable()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|foreach (var (a, b) in new(int, int)[] { })|]
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task MethodWithNullableReturn()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable
            using System.Threading.Tasks;
            class C
            {
                string? M()
                {
                    [|await Task.Delay(1);|]
                    return null;
                }
            }
            """,
            """
            #nullable enable
            using System.Threading.Tasks;
            class C
            {
                async Task<string?> MAsync()
                {
                    await Task.Delay(1);
                    return null;
                }
            }
            """);

    [Fact]
    public async Task EnumerableMethodWithNullableType()
    {
        var initial =
            """
            #nullable enable
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                IEnumerable<string?> Test()
                {
                    yield return string.Empty;
                    [|await Task.Delay(1);|]
                }
            }
            """ + IAsyncEnumerable;

        var expected =
            """
            #nullable enable
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                async IAsyncEnumerable<string?> TestAsync()
                {
                    yield return string.Empty;
                    await Task.Delay(1);
                }
            }
            """ + IAsyncEnumerable;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task EnumeratorMethodWithNullableType()
    {
        var initial =
            """
            #nullable enable
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                IEnumerator<string?> Test()
                {
                    yield return string.Empty;
                    [|await Task.Delay(1);|]
                }
            }
            """ + IAsyncEnumerable;

        var expected =
            """
            #nullable enable
            using System.Threading.Tasks;
            using System.Collections.Generic;
            class Program
            {
                async IAsyncEnumerator<string?> TestAsync()
                {
                    yield return string.Empty;
                    await Task.Delay(1);
                }
            }
            """ + IAsyncEnumerable;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25446")]
    public Task TestOnAwaitParsedAsType()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Task task = null;
                    [|await|] task;
                }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                async Task MAsync()
                {
                    Task task = null;
                    await task;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63404")]
    public Task PartialMethod1()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            public partial class C
            {
                partial void M();
            }

            public partial class C
            {
                partial void M()
                {
                    [|await|] Task.Delay(1);
                }
            }
            """, """
            using System.Threading.Tasks;
            
            public partial class C
            {
                partial Task MAsync();
            }
            
            public partial class C
            {
                async partial Task MAsync()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63404")]
    public Task PartialMethod2()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            public partial class C
            {
                public partial void M();
            }

            public partial class C
            {
                public partial void M()
                {
                    [|await|] Task.Delay(1);
                }
            }
            """, """
            using System.Threading.Tasks;
            
            public partial class C
            {
                public partial Task MAsync();
            }
            
            public partial class C
            {
                public async partial Task MAsync()
                {
                    await Task.Delay(1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63404")]
    public Task PartialMethod3()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            public partial class C
            {
                partial void M();
            }

            public partial class C
            {
                partial void M()
                {
                    [|await|] Task.Delay(1);
                }
            }
            """, """
            using System.Threading.Tasks;
            
            public partial class C
            {
                partial void M();
            }
            
            public partial class C
            {
                async partial void M()
                {
                    await Task.Delay(1);
                }
            }
            """, index: 1);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63404")]
    public Task PartialMethod4()
        => TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            public partial class C
            {
                public partial void M();
            }

            public partial class C
            {
                public partial void M()
                {
                    [|await|] Task.Delay(1);
                }
            }
            """, """
            using System.Threading.Tasks;
            
            public partial class C
            {
                public partial void M();
            }
            
            public partial class C
            {
                public async partial void M()
                {
                    await Task.Delay(1);
                }
            }
            """, index: 1);
}
