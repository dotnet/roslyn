// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddAwait;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.AddAwait;

[Trait(Traits.Feature, Traits.Features.AddAwait)]
[Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
public class AddAwaitTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpAddAwaitCodeRefactoringProvider();

    [Fact]
    public async Task Simple()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = GetNumberAsync()[||];
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync();
                }
            }
            """);
    }

    [Fact]
    public async Task SimpleWithConfigureAwait()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = GetNumberAsync()[||];
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync().ConfigureAwait(false);
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task InArgument()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync(int argument)
                {
                    var x = GetNumberAsync(arg[||]ument);
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync(int argument)
                {
                    var x = await GetNumberAsync(argument);
                }
            }
            """);
    }

    [Fact]
    public async Task InvocationInArgument()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    M(GetNumberAsync()[||]);
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    M(await GetNumberAsync());
                }
            }
            """);
    }

    [Fact]
    public async Task InvocationInArgumentWithConfigureAwait()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    M(GetNumberAsync()[||]);
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    M(await GetNumberAsync().ConfigureAwait(false));
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task AlreadyAwaited()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync()[||];
                }
            }
            """);
    }

    [Fact]
    public async Task AlreadyAwaitedAndConfigured()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync()[||].ConfigureAwait(false);
                }
            }
            """);
    }

    [Fact]
    public async Task AlreadyAwaitedAndConfigured2()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync().ConfigureAwait(false)[||];
                }
            }
            """);
    }

    [Fact]
    public async Task SimpleWithTrivia()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = // comment
                        GetNumberAsync()[||] /* comment */
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = // comment
                        await GetNumberAsync()[||] /* comment */
                }
            }
            """);
    }

    [Fact]
    public async Task SimpleWithTrivia2()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = /* comment */ GetNumberAsync()[||] // comment
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = /* comment */ await GetNumberAsync()[||] // comment
                }
            }
            """);
    }

    [Fact]
    public async Task SimpleWithTriviaWithConfigureAwait()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = // comment
                        GetNumberAsync()[||] /* comment */
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = // comment
                        await GetNumberAsync().ConfigureAwait(false) /* comment */
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task SimpleWithTrivia2WithConfigureAwait()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = /* comment */ GetNumberAsync()[||] // comment
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = /* comment */ await GetNumberAsync().ConfigureAwait(false) // comment
                }
            }
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task OnSemiColon()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = GetNumberAsync();[||]
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task Selection()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = [|GetNumberAsync()|];
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public async Task Selection2()
    {
        await TestInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    [|var x = GetNumberAsync();|]
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync();
                }
            }
            """);
    }

    [Fact]
    public async Task ChainedInvocation()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                Task<int> GetNumberAsync() => throw null;
                async void M()
                {
                    var x = GetNumberAsync()[||].ToString();
                }
            }
            """);
    }

    [Fact]
    public async Task ChainedInvocation_ExpressionOfInvalidInvocation()
    {
        await TestInRegularAndScript1Async("""
            using System.Threading.Tasks;
            class Program
            {
                Task<int> GetNumberAsync() => throw null;
                async void M()
                {
                    var x = GetNumberAsync()[||].Invalid();
                }
            }
            """, """
            using System.Threading.Tasks;
            class Program
            {
                Task<int> GetNumberAsync() => throw null;
                async void M()
                {
                    var x = (await GetNumberAsync()).Invalid();
                }
            }
            """);
    }

    [Fact]
    public async Task BadAsyncReturnOperand1()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test()
                {
                    return 3;
                }

                async Task<int> Test2()
                {
                    return [|Test()|];
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test()
                {
                    return 3;
                }

                async Task<int> Test2()
                {
                    return await Test();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task BadAsyncReturnOperand_WithLeadingTrivia1()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test()
                {
                    return 3;
                }

                async Task<int> Test2()
                {
                    return
                    // Useful comment
                    [|Test()|];
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test()
                {
                    return 3;
                }

                async Task<int> Test2()
                {
                    return
                    // Useful comment
                    await Test();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task BadAsyncReturnOperand_ConditionalExpressionWithTrailingTrivia_SingleLine()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test() => 3;

                async Task<int> Test2()
                {
                    return [|true ? Test() /* true */ : Test()|] /* false */;
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test() => 3;

                async Task<int> Test2()
                {
                    return await (true ? Test() /* true */ : Test()) /* false */;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task BadAsyncReturnOperand_ConditionalExpressionWithTrailingTrivia_Multiline()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test() => 3;

                async Task<int> Test2()
                {
                    return [|true ? Test() // aaa
                                : Test()|] // bbb
                                ;
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test() => 3;

                async Task<int> Test2()
                {
                    return await (true ? Test() // aaa
                                : Test()) // bbb
                                ;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task BadAsyncReturnOperand_NullCoalescingExpressionWithTrailingTrivia_SingleLine()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test() => 3;

                async Task<int> Test2()
                {
                    return [|null /* 0 */ ?? Test()|] /* 1 */;
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test() => 3;

                async Task<int> Test2()
                {
                    return await (null /* 0 */ ?? Test()) /* 1 */;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task BadAsyncReturnOperand_NullCoalescingExpressionWithTrailingTrivia_Multiline()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test() => 3;

                async Task<int> Test2()
                {
                    return [|null   // aaa
                        ?? Test()|] // bbb
                        ;
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test() => 3;

                async Task<int> Test2()
                {
                    return await (null   // aaa
                        ?? Test()) // bbb
                        ;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task BadAsyncReturnOperand_AsExpressionWithTrailingTrivia_SingleLine()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test2()
                {
                    return [|null /* 0 */ as Task<int>|] /* 1 */;
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test2()
                {
                    return await (null /* 0 */ as Task<int>) /* 1 */;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task BadAsyncReturnOperand_AsExpressionWithTrailingTrivia_Multiline()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test() => 3;

                async Task<int> Test2()
                {
                    return [|null      // aaa
                        as Task<int>|] // bbb
                        ;
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test() => 3;

                async Task<int> Test2()
                {
                    return await (null      // aaa
                        as Task<int>) // bbb
                        ;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TaskNotAwaited()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                async void Test()
                {
                    [|Task.Delay(3)|];
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                async void Test()
                {
                    await Task.Delay(3);
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TaskNotAwaited_WithLeadingTrivia()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                async void Test()
                {

                    // Useful comment
                    [|Task.Delay(3)|];
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                async void Test()
                {

                    // Useful comment
                    await Task.Delay(3);
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task FunctionNotAwaited()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                Task AwaitableFunction()
                {
                    return Task.FromResult(true);
                }

                async void Test()
                {
                    [|AwaitableFunction()|];
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                Task AwaitableFunction()
                {
                    return Task.FromResult(true);
                }

                async void Test()
                {
                    await AwaitableFunction();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task FunctionNotAwaited_WithLeadingTrivia()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                Task AwaitableFunction()
                {
                    return Task.FromResult(true);
                }

                async void Test()
                {

                    // Useful comment
                    [|AwaitableFunction()|];
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                Task AwaitableFunction()
                {
                    return Task.FromResult(true);
                }

                async void Test()
                {

                    // Useful comment
                    await AwaitableFunction();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task FunctionNotAwaited_WithLeadingTrivia1()
    {
        var initial =
            """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                Task AwaitableFunction()
                {
                    return Task.FromResult(true);
                }

                async void Test()
                {
                    var i = 0;

                    [|AwaitableFunction()|];
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                Task AwaitableFunction()
                {
                    return Task.FromResult(true);
                }

                async void Test()
                {
                    var i = 0;

                    await AwaitableFunction();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestAssignmentExpression()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    int myInt = [|MyIntMethodAsync()|];
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    int myInt = await MyIntMethodAsync();
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpressionWithConversion()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    long myInt = [|MyIntMethodAsync()|];
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    long myInt = await MyIntMethodAsync();
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpressionWithConversionInNonAsyncFunction()
    {

        await TestMissingAsync("""
            using System.Threading.Tasks;

            class TestClass
            {
                private Task MyTestMethod1Async()
                {
                    long myInt = [|MyIntMethodAsync()|];
                    return Task.CompletedTask;
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpressionWithConversionInAsyncFunction()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    long myInt = [|MyIntMethodAsync()|];
                }

                private Task<object> MyIntMethodAsync()
                {
                    return Task.FromResult(new object());
                }
            }
            """,
            """
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    long myInt = [|await MyIntMethodAsync()|];
                }

                private Task<object> MyIntMethodAsync()
                {
                    return Task.FromResult(new object());
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Action lambda = async () => {
                        int myInt = [|MyIntMethodAsync()|];
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Action lambda = async () => {
                        int myInt = await MyIntMethodAsync();
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Func<Task> lambda = async () => {
                        int myInt = [|MyIntMethodAsync()|];
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Func<Task> lambda = async () => {
                        int myInt = await MyIntMethodAsync();
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression3()
    {
        await TestMissingAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Func<Task> lambda = () => {
                        int myInt = [|MyIntMethodAsync()|];
                        return Task.CompletedTask;
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression3_1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Func<Task> lambda = async () => {
                        int myInt = [|MyIntMethodAsync()|];
                        return Task.CompletedTask;
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Func<Task> lambda = async () => {
                        int myInt = await MyIntMethodAsync();
                        return Task.CompletedTask;
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression4()
    {
        await TestMissingAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Action lambda = () => {
                        int myInt = [|MyIntMethodAsync()|];
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression4_1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Action lambda = async () => {
                        int myInt = [|MyIntMethodAsync()|];
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Action lambda = async () => {
                        int myInt = await MyIntMethodAsync();
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Action @delegate = async delegate {
                        int myInt = [|MyIntMethodAsync()|];
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Action @delegate = async delegate {
                        int myInt = await MyIntMethodAsync();
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression6()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Func<Task> @delegate = async delegate {
                        int myInt = [|MyIntMethodAsync()|];
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Func<Task> @delegate = async delegate {
                        int myInt = await MyIntMethodAsync();
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression7()
    {
        await TestMissingAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Action @delegate = delegate {
                        int myInt = [|MyIntMethodAsync()|];
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression7_1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Action @delegate = async delegate {
                        int myInt = [|MyIntMethodAsync()|];
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Action @delegate = async delegate {
                        int myInt = await MyIntMethodAsync();
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression8()
    {
        await TestMissingAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Func<Task> @delegate = delegate {
                        int myInt = [|MyIntMethodAsync()|];
                        return Task.CompletedTask;
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestAssignmentExpression8_1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Func<Task> @delegate = async delegate {
                        int myInt = [|MyIntMethodAsync()|];
                        return Task.CompletedTask;
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async()
                {
                    Func<Task> @delegate = async delegate {
                        int myInt = [|await MyIntMethodAsync()|];
                        return Task.CompletedTask;
                    };
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestTernaryOperator()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> A()
                {
                    return [|true ? Task.FromResult(0) : Task.FromResult(1)|];
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> A()
                {
                    return await (true ? Task.FromResult(0) : Task.FromResult(1));
                }
            }
            """);
    }

    [Fact]
    public async Task TestNullCoalescingOperator()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> A()
                {
                    return [|null ?? Task.FromResult(1)|]; }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> A()
                {
                    return await (null ?? Task.FromResult(1)); }
            }
            """);
    }

    [Fact]
    public async Task TestAsExpression()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> A()
                {
                    return [|null as Task<int>|]; }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> A()
                {
                    return await (null as Task<int>); }
            }
            """);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1345322")]
    public async Task TestOnTaskTypeItself()
    {
        await TestMissingAsync(
            """
            using System.Threading.Tasks;

            class Program
            {
                static async [||]Task Main(string[] args)
                {
                }
            }
            """);
    }
}
