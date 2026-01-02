// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeMethodSynchronous;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnnecessaryAsyncModifierDiagnosticAnalyzer,
    CSharpMakeMethodSynchronousCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
public sealed class MakeMethodSynchronousTests
{
    [Fact]
    public Task TestTaskReturnType()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                async Task {|CS1998:Goo|}()
                {
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                }
            }
            """);

    [Fact]
    public Task TestTaskOfTReturnType()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> {|CS1998:Goo|}()
                {
                    return 1;
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                int {|#0:Goo|}()
                {
                    return 1;
                }
            }
            """);

    [Fact]
    public Task TestSecondModifier()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                public async Task {|CS1998:Goo|}()
                {
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                public void Goo()
                {
                }
            }
            """);

    [Fact]
    public Task TestFirstModifier()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                async public Task {|CS1998:Goo|}()
                {
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                public void Goo()
                {
                }
            }
            """);

    [Fact]
    public Task TestTrailingTrivia()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                async // comment
                Task {|CS1998:Goo|}()
                {
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                }
            }
            """);

    [Fact]
    public Task TestRenameMethod()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                async Task {|CS1998:GooAsync|}()
                {
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                }
            }
            """);

    [Fact]
    public Task TestRenameMethod1()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                async Task {|CS1998:GooAsync|}()
                {
                }

                void Bar()
                {
                    GooAsync();
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                }

                void Bar()
                {
                    Goo();
                }
            }
            """);

    [Fact]
    public async Task TestParenthesizedLambda()
    {
        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f =
                        () {|#0:=>|} { };
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f =
                        async () {|CS1998:=>|} { };
                }
            }
            """,
            FixedState =
            {
                Sources = { expected },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(10,16): error CS1643: Not all code paths return a value in lambda expression of type 'Func<Task>'
                    DiagnosticResult.CompilerError("CS1643").WithLocation(0),
                },
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TestSimpleLambda()
    {
        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<string, Task> f =
                        a {|#0:=>|} { };
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<string, Task> f =
                        async a {|CS1998:=>|} { };
                }
            }
            """,
            FixedState =
            {
                Sources = { expected },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(10,15): error CS1643: Not all code paths return a value in lambda expression of type 'Func<Task>'
                    DiagnosticResult.CompilerError("CS1643").WithLocation(0),
                },
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TestLambdaWithExpressionBody()
    {
        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<string, Task<int>> f =
                        a => {|#0:1|};
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<string, Task<int>> f =
                        async a {|CS1998:=>|} 1;
                }
            }
            """,
            FixedState =
            {
                Sources = { expected },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(10,18): error CS0029: Cannot implicitly convert type 'int' to 'System.Threading.Tasks.Task<int>'
                    DiagnosticResult.CompilerError("CS0029").WithLocation(0).WithArguments("int", "System.Threading.Tasks.Task<int>"),
                    // /0/Test0.cs(10,18): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                    DiagnosticResult.CompilerError("CS1662").WithLocation(0),
                },
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TestAnonymousMethod()
    {
        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f =
                        {|#0:delegate|} { };
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f =
                        async {|CS1998:delegate|} { };
                }
            }
            """,
            FixedState =
            {
                Sources = { expected },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(10,13): error CS1643: Not all code paths return a value in anonymous method of type 'Func<Task>'
                    DiagnosticResult.CompilerError("CS1643").WithLocation(0),
                },
            },
        }.RunAsync();
    }

    [Fact]
    public Task TestFixAll()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            public class Class1
            {
                async Task {|CS1998:GooAsync|}()
                {
                    BarAsync();
                }

                async Task<int> {|#0:{|CS1998:BarAsync|}|}()
                {
                    GooAsync();
                    return 1;
                }
            }
            """,
            """
            using System.Threading.Tasks;

            public class Class1
            {
                void Goo()
                {
                    Bar();
                }

                int {|#0:Bar|}()
                {
                    Goo();
                    return 1;
                }
            }
            """);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13961")]
    public async Task TestRemoveAwaitFromCaller1()
    {
        var expected =
            """
            using System.Threading.Tasks;

            public class Class1
            {
                void Goo()
                {
                }

                async void {|CS1998:BarAsync|}()
                {
                    Goo();
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System.Threading.Tasks;

            public class Class1
            {
                async Task {|CS1998:GooAsync|}()
                {
                }

                async void BarAsync()
                {
                    await GooAsync();
                }
            }
            """,
            FixedState =
            {
                Sources = { expected },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13961")]
    public async Task TestRemoveAwaitFromCaller2()
    {
        var expected =
            """
            using System.Threading.Tasks;

            public class Class1
            {
                void Goo()
                {
                }

                async void {|CS1998:BarAsync|}()
                {
                    Goo();
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System.Threading.Tasks;

            public class Class1
            {
                async Task {|CS1998:GooAsync|}()
                {
                }

                async void BarAsync()
                {
                    await GooAsync().ConfigureAwait(false);
                }
            }
            """,
            FixedState =
            {
                Sources = { expected },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13961")]
    public async Task TestRemoveAwaitFromCaller3()
    {
        var expected =
            """
            using System.Threading.Tasks;

            public class Class1
            {
                void Goo()
                {
                }

                async void {|CS1998:BarAsync|}()
                {
                    this.Goo();
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System.Threading.Tasks;

            public class Class1
            {
                async Task {|CS1998:GooAsync|}()
                {
                }

                async void BarAsync()
                {
                    await this.GooAsync();
                }
            }
            """,
            FixedState =
            {
                Sources = { expected },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13961")]
    public async Task TestRemoveAwaitFromCaller4()
    {
        var expected =
            """
            using System.Threading.Tasks;

            public class Class1
            {
                void Goo()
                {
                }

                async void {|CS1998:BarAsync|}()
                {
                    this.Goo();
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System.Threading.Tasks;

            public class Class1
            {
                async Task {|CS1998:GooAsync|}()
                {
                }

                async void BarAsync()
                {
                    await this.GooAsync().ConfigureAwait(false);
                }
            }
            """,
            FixedState =
            {
                Sources = { expected },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13961")]
    public async Task TestRemoveAwaitFromCallerNested1()
    {
        var expected =
            """
            using System.Threading.Tasks;

            public class Class1
            {
                int Goo(int i)
                {
                    return 1;
                }

                async void {|CS1998:BarAsync|}()
                {
                    this.Goo(this.Goo(0));
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System.Threading.Tasks;

            public class Class1
            {
                async Task<int> {|CS1998:GooAsync|}(int i)
                {
                    return 1;
                }

                async void BarAsync()
                {
                    await this.GooAsync(await this.GooAsync(0));
                }
            }
            """,
            FixedState =
            {
                Sources = { expected },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13961")]
    public async Task TestRemoveAwaitFromCallerNested()
    {
        var expected =
            """
            using System.Threading.Tasks;

            public class Class1
            {
                int Goo(int i)
                {
                    return 1;
                }

                async void {|CS1998:BarAsync|}()
                {
                    this.Goo(this.Goo(0));
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
            using System.Threading.Tasks;

            public class Class1
            {
                async Task<int> {|CS1998:GooAsync|}(int i)
                {
                    return 1;
                }

                async void BarAsync()
                {
                    await this.GooAsync(await this.GooAsync(0).ConfigureAwait(false)).ConfigureAwait(false);
                }
            }
            """,
            FixedState =
            {
                Sources = { expected },
                MarkupHandling = MarkupMode.Allow,
            },
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodAsynchronous)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/14133")]
    public Task RemoveAsyncInLocalFunction()
        => VerifyCS.VerifyCodeFixAsync(
            """
            using System.Threading.Tasks;

            class C
            {
                public void M1()
                {
                    async Task {|CS1998:M2Async|}()
                    {
                    }
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class C
            {
                public void M1()
                {
                    void M2()
                    {
                    }
                }
            }
            """);

    [Theory]
    [InlineData("Task<C>", "C")]
    [InlineData("Task<int>", "int")]
    [InlineData("Task", "void")]
    [InlineData("void", "void")]
    [Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodAsynchronous)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18307")]
    public Task RemoveAsyncInLocalFunctionKeepsTrivia(string asyncReturn, string expectedReturn)
        => VerifyCS.VerifyCodeFixAsync(
            $$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                public void M1()
                {
                    // Leading trivia
                    /*1*/ async {{asyncReturn}} /*2*/ {|CS1998:M2Async|}/*3*/() /*4*/
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """,
            $$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                public void M1()
                {
                    // Leading trivia
                    /*1*/
                    {{expectedReturn}} /*2*/ M2/*3*/() /*4*/
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """);

    [Theory]
    [InlineData("", "Task<C>", "\r\n    C")]
    [InlineData("", "Task<int>", "\r\n    int")]
    [InlineData("", "Task", "\r\n    void")]
    [InlineData("", "void", "\r\n    void")]
    [InlineData("public", "Task<C>", " C")]
    [InlineData("public", "Task<int>", " int")]
    [InlineData("public", "Task", " void")]
    [InlineData("public", "void", " void")]
    [Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodAsynchronous)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18307")]
    public Task RemoveAsyncKeepsTrivia(string modifiers, string asyncReturn, string expectedReturn)
        => VerifyCS.VerifyCodeFixAsync(
            $$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                // Leading trivia
                {{modifiers}}/*1*/ async {{asyncReturn}} /*2*/ {|CS1998:M2Async|}/*3*/() /*4*/
                {
                    throw new NotImplementedException();
                }
            }
            """,
            $$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                // Leading trivia
                {{modifiers}}/*1*/{{expectedReturn}} /*2*/ M2/*3*/() /*4*/
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public async Task MethodWithUsingAwait()
    {
        var source =
            """
            class C
            {
                async System.Threading.Tasks.Task MAsync()
                {
                    await using ({|#0:var x = new object()|})
                    {
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21,
            TestCode = source,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(5,22): error CS8410: 'object': type used in an asynchronous using statement must implement 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                DiagnosticResult.CompilerError("CS8410").WithLocation(0).WithArguments("object"),
            },
            FixedCode = source,
        }.RunAsync();
    }

    [Fact]
    public Task MethodWithUsingNoAwait()
        => VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                async System.Threading.Tasks.Task {|CS1998:MAsync|}()
                {
                    using ({|#0:var x = new object()|})
                    {
                    }
                }
            }
            """,
            // /0/Test0.cs(5,16): error CS1674: 'object': type used in a using statement must implement 'System.IDisposable'.
            DiagnosticResult.CompilerError("CS1674").WithLocation(0).WithArguments("object"),
            """
            class C
            {
                void M()
                {
                    using ({|#0:var x = new object()|})
                    {
                    }
                }
            }
            """);

    [Fact]
    public async Task MethodWithAwaitForEach()
    {
        var source =
            """
            class C
            {
                async System.Threading.Tasks.Task MAsync()
                {
                    await foreach (var n in {|#0:new int[] { }|})
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            // /0/Test0.cs(5,33): error CS1061: 'bool' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'bool' could be found (are you missing a using directive or an assembly reference?)
            DiagnosticResult.CompilerError("CS1061").WithLocation(0).WithArguments("bool", "GetAwaiter"),
            source);
    }

    [Fact]
    public Task MethodWithForEachNoAwait()
        => VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                async System.Threading.Tasks.Task {|CS1998:MAsync|}()
                {
                    foreach (var n in new int[] { })
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    foreach (var n in new int[] { })
                    {
                    }
                }
            }
            """);

    [Fact]
    public async Task MethodWithForEachVariableAwait()
    {
        var source =
            """
            class C
            {
                async System.Threading.Tasks.Task MAsync()
                {
                    await foreach (var (a, b) in {|#0:new(int, int)[] { }|})
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            // /0/Test0.cs(5,38): error CS1061: 'bool' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'bool' could be found (are you missing a using directive or an assembly reference?)
            DiagnosticResult.CompilerError("CS1061").WithLocation(0).WithArguments("bool", "GetAwaiter"),
            source);
    }

    [Fact]
    public Task MethodWithForEachVariableNoAwait()
        => VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                async System.Threading.Tasks.Task {|CS1998:MAsync|}()
                {
                    foreach (var (a, b) in new(int, int)[] { })
                    {
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    foreach (var (a, b) in new (int, int)[] { })
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestIAsyncEnumerableReturnType()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21,
            TestCode = """
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                async IAsyncEnumerable<int> {|CS1998:MAsync|}()
                {
                    yield return 1;
                }
            }
            """,
            FixedCode = """
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                IEnumerable<int> M()
                {
                    yield return 1;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestIAsyncEnumeratorReturnTypeOnLocalFunction()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21,
            TestCode = """
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                void Method()
                {
                    async IAsyncEnumerator<int> {|CS1998:MAsync|}()
                    {
                        yield return 1;
                    }
                }
            }
            """,
            FixedCode = """
            using System.Threading.Tasks;
            using System.Collections.Generic;

            class C
            {
                void Method()
                {
                    IEnumerator<int> M()
                    {
                        yield return 1;
                    }
                }
            }
            """,
        }.RunAsync();
}
