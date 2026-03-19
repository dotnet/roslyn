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
public sealed class AddAwaitTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpAddAwaitCodeRefactoringProvider();

    [Fact]
    public Task Simple()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task SimpleWithConfigureAwait()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task InArgument()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task InvocationInArgument()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task InvocationInArgumentWithConfigureAwait()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task AlreadyAwaited()
        => TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync()[||];
                }
            }
            """);

    [Fact]
    public Task AlreadyAwaitedAndConfigured()
        => TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync()[||].ConfigureAwait(false);
                }
            }
            """);

    [Fact]
    public Task AlreadyAwaitedAndConfigured2()
        => TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;
            class Program
            {
                async Task<int> GetNumberAsync()
                {
                    var x = await GetNumberAsync().ConfigureAwait(false)[||];
                }
            }
            """);

    [Fact]
    public Task SimpleWithTrivia()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task SimpleWithTrivia2()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task SimpleWithTriviaWithConfigureAwait()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task SimpleWithTrivia2WithConfigureAwait()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task OnSemiColon()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task Selection()
        => TestInRegularAndScriptAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")]
    public Task Selection2()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task ChainedInvocation()
        => TestMissingInRegularAndScriptAsync("""
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

    [Fact]
    public Task ChainedInvocation_ExpressionOfInvalidInvocation()
        => TestInRegularAndScriptAsync("""
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

    [Fact]
    public Task BadAsyncReturnOperand1()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task BadAsyncReturnOperand_WithLeadingTrivia1()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task BadAsyncReturnOperand_ConditionalExpressionWithTrailingTrivia_SingleLine()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task BadAsyncReturnOperand_ConditionalExpressionWithTrailingTrivia_Multiline()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task BadAsyncReturnOperand_NullCoalescingExpressionWithTrailingTrivia_SingleLine()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task BadAsyncReturnOperand_NullCoalescingExpressionWithTrailingTrivia_Multiline()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task BadAsyncReturnOperand_AsExpressionWithTrailingTrivia_SingleLine()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test2()
                {
                    return [|null /* 0 */ as Task<int>|] /* 1 */;
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                async Task<int> Test2()
                {
                    return await (null /* 0 */ as Task<int>) /* 1 */;
                }
            }
            """);

    [Fact]
    public Task BadAsyncReturnOperand_AsExpressionWithTrailingTrivia_Multiline()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TaskNotAwaited()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Threading.Tasks;
            class Program
            {
                async void Test()
                {
                    [|Task.Delay(3)|];
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;
            class Program
            {
                async void Test()
                {
                    await Task.Delay(3);
                }
            }
            """);

    [Fact]
    public Task TaskNotAwaited_WithLeadingTrivia()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task FunctionNotAwaited()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task FunctionNotAwaited_WithLeadingTrivia()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task FunctionNotAwaited_WithLeadingTrivia1()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestAssignmentExpression()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAssignmentExpressionWithConversion()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAssignmentExpressionWithConversionInNonAsyncFunction()
        => TestMissingAsync("""
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

    [Fact]
    public Task TestAssignmentExpressionWithConversionInAsyncFunction()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAssignmentExpression1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAssignmentExpression2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAssignmentExpression3()
        => TestMissingAsync(
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

    [Fact]
    public Task TestAssignmentExpression3_1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAssignmentExpression4()
        => TestMissingAsync(
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

    [Fact]
    public Task TestAssignmentExpression4_1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAssignmentExpression5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAssignmentExpression6()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAssignmentExpression7()
        => TestMissingAsync(
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

    [Fact]
    public Task TestAssignmentExpression7_1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAssignmentExpression8()
        => TestMissingAsync(
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

    [Fact]
    public Task TestAssignmentExpression8_1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestTernaryOperator()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNullCoalescingOperator()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAsExpression()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1345322")]
    public Task TestOnTaskTypeItself()
        => TestMissingAsync(
            """
            using System.Threading.Tasks;

            class Program
            {
                static async [||]Task Main(string[] args)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66726")]
    public Task NotOnBindingExpression1()
        => TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            class TestClass
            {
                private async Task MyTestMethod1Async(TestClass c)
                {
                    _ = c?.[|MyIntMethodAsync()|];
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66726")]
    public Task NotOnBindingExpression2()
        => TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            class TestClass
            {
                private TestClass C;

                private async Task MyTestMethod1Async(TestClass c)
                {
                    _ = c?.C.[|MyIntMethodAsync()|];
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66726")]
    public Task NotOnBindingExpression3()
        => TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            class TestClass
            {
                private TestClass this[int i] => null;

                private async Task MyTestMethod1Async(TestClass c)
                {
                    _ = c?[0].[|MyIntMethodAsync()|];
                }

                private Task<int> MyIntMethodAsync()
                {
                    return Task.FromResult(result: 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66726")]
    public Task NotOnBindingExpression4()
        => TestMissingInRegularAndScriptAsync("""
            using System.Threading.Tasks;

            class TestClass
            {
                private Task<int> this[int i] => null;

                private async Task MyTestMethod1Async(TestClass c)
                {
                    _ = c?[|[0]|];
                }
            }
            """);
}
