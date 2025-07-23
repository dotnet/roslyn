// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
public sealed class UseExpressionBodyForLambdasAnalyzerTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new UseExpressionBodyForLambdaDiagnosticAnalyzer(), new UseExpressionBodyForLambdaCodeFixProvider());

    private OptionsCollection UseExpressionBody
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement);

    private OptionsCollection UseBlockBody
        => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.NeverWithSuggestionEnforcement);

    [Fact]
    public Task UseExpressionBodyInMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x [|=>|]
                    {
                        return x.ToString();
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x => x.ToString();
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task TestMissingWhenAlreadyAndExpressionBody()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x [|=>|] x.ToString();
                }
            }
            """, new TestParameters(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyInMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x [|=>|] x.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x =>
                    {
                        return x.ToString();
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task MissingWhenAlreadyHasBlockBody()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x [|=>|] { return x.ToString(); };
                }
            }
            """, new TestParameters(options: UseBlockBody));

    [Fact]
    public Task UseExpressionBodyInArgument()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    TargetMethod(x [|=>|]
                    {
                        return x.ToString();
                    });
                }

                void TargetMethod(Func<int, string> targetParam) { }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    TargetMethod(x => x.ToString());
                }

                void TargetMethod(Func<int, string> targetParam) { }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyInArgument()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    TargetMethod(x [|=>|] x.ToString());
                }

                void TargetMethod(Func<int, string> targetParam) { }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    TargetMethod(x =>
                    {
                        return x.ToString();
                    });
                }

                void TargetMethod(Func<int, string> targetParam) { }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseExpressionBodyFromReturnKeyword()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x =>
                    {
                        [|return|] x.ToString();
                    };
                }
            }
            """, new TestParameters(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyFromLambdaOpeningBrace()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x =>
                    [|{|]
                        return x.ToString();
                    };
                }
            }
            """, new TestParameters(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyFromLambdaClosingBrace()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x =>
                    {
                        return x.ToString();
                    [|}|];
                }
            }
            """, new TestParameters(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x [|=>|]
                    {
                        throw null;
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x => throw null;
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x [|=>|] throw null;
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x =>
                    {
                        throw null;
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseExpressionBodyWithVoidReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = x [|=>|]
                    {
                        x.ToString();
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = x => x.ToString();
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyWithVoidReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = x [|=>|]
                    {
                        throw null;
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = x => throw null;
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyWithVoidReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = x [|=>|] x.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = x =>
                    {
                        x.ToString();
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseBlockBodyWithVoidReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = x [|=>|] throw null;
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = x =>
                    {
                        throw null;
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseExpressionBodyWithAsyncVoidReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = async x [|=>|]
                    {
                        x.ToString();
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = async x => x.ToString();
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyWithAsyncVoidReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = async x [|=>|]
                    {
                        throw null;
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = async x => throw null;
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyWithAsyncVoidReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = async x [|=>|] x.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = async x =>
                    {
                        x.ToString();
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseBlockBodyWithAsyncVoidReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = async x [|=>|] throw null;
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action<int> f = async x =>
                    {
                        throw null;
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseExpressionBodyWithTaskReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = () [|=>|]
                    {
                        return Task.CompletedTask;
                    };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = () => Task.CompletedTask;
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyWithTaskReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = () [|=>|]
                    {
                        throw null;
                    };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = () => throw null;
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyWithTaskReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = () [|=>|] Task.CompletedTask;
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = () =>
                    {
                        return Task.CompletedTask;
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseBlockBodyWithTaskReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = () [|=>|] throw null;
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = () =>
                    {
                        throw null;
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseExpressionBodyWithAsyncTaskReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = async () [|=>|]
                    {
                        await Task.CompletedTask;
                    };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = async () => await Task.CompletedTask;
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyWithAsyncTaskReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = async () [|=>|]
                    {
                        throw null;
                    };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = async () => throw null;
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyWithAsyncTaskReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = async () [|=>|] await Task.CompletedTask;
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = async () =>
                    {
                        await Task.CompletedTask;
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseBlockBodyWithAsyncTaskReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = async () [|=>|] throw null;
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<Task> f = async () =>
                    {
                        throw null;
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseExpressionBodyWithTaskTReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = x [|=>|]
                    {
                        return Task.FromResult(x.ToString());
                    };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = x => Task.FromResult(x.ToString());
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyWithTaskTReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = x [|=>|]
                    {
                        throw null;
                    };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = x => throw null;
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyWithTaskTReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = x [|=>|] Task.FromResult(x.ToString());
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = x =>
                    {
                        return Task.FromResult(x.ToString());
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseBlockBodyWithTaskTReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = x [|=>|] throw null;
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = x =>
                    {
                        throw null;
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseExpressionBodyWithAsyncTaskTReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = async x [|=>|]
                    {
                        return await Task.FromResult(x.ToString());
                    };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = async x => await Task.FromResult(x.ToString());
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyWithAsyncTaskTReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = async x [|=>|]
                    {
                        throw null;
                    };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = async x => throw null;
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyWithAsyncTaskTReturn()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = async x [|=>|] await Task.FromResult(x.ToString());
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = async x =>
                    {
                        return await Task.FromResult(x.ToString());
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseBlockBodyWithAsyncTaskTReturnThrowing()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = async x [|=>|] throw null;
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, Task<string>> f = async x =>
                    {
                        throw null;
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseExpressionBodyWithPrecedingComment()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x [|=>|]
                    {
                        // Comment
                        return x.ToString();
                    };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x =>
                        // Comment
                        x.ToString();
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyWithEndingComment()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x [|=>|]
                    {
                        return x.ToString(); // Comment
                    };
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x => x.ToString();
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyWithEndingComment()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x [|=>|] x.ToString(); // Comment
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Func<int, string> f = x =>
                    {
                        return x.ToString();
                    }; // Comment
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseExpressionBodyInMethod_FixAll1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = x {|FixAllInDocument:=>|}
                    {
                        return y =>
                        {
                            return (x + y).ToString();
                        };
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = x => y => (x + y).ToString();
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseExpressionBodyInMethod_FixAll2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = x =>
                    {
                        return y {|FixAllInDocument:=>|}
                        {
                            return (x + y).ToString();
                        };
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = x => y => (x + y).ToString();
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task UseBlockBodyInMethod_FixAll1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = x {|FixAllInDocument:=>|} y => (x + y).ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = x =>
                    {
                        return y =>
                        {
                            return (x + y).ToString();
                        };
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task UseBlockBodyInMethod_FixAll2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = x => y {|FixAllInDocument:=>|} (x + y).ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = x =>
                    {
                        return y =>
                        {
                            return (x + y).ToString();
                        };
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task FixAllNested1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = a {|FixAllInDocument:=>|}
                    {
                        return b =>
                        {
                            return b.ToString();
                        };
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = a => b => b.ToString();
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task FixAllNested2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = a =>
                    {
                        return b {|FixAllInDocument:=>|}
                        {
                            return b.ToString();
                        };
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = a => b => b.ToString();
                }
            }
            """, new(options: UseExpressionBody));

    [Fact]
    public Task FixAllNested3()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = a {|FixAllInDocument:=>|} b => b.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = a =>
                    {
                        return b =>
                        {
                            return b.ToString();
                        };
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact]
    public Task FixAllNested4()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = a => b {|FixAllInDocument:=>|} b.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Func<int, Func<int, string>> f = a =>
                    {
                        return b =>
                        {
                            return b.ToString();
                        };
                    };
                }
            }
            """, new(options: UseBlockBody));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76645")]
    public Task TestMethodOverloadResolutionChange()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo(int result, Func<int> function)
                {
                    Execute(() [|=>|]
                    {
                        result = function();
                    });
                }

                void Execute(Action action) { }
                void Execute(Func<int> function) { }
            }
            """, new(options: UseExpressionBody));
}
