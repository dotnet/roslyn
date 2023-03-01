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
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
    public class UseExpressionBodyForLambdasAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public UseExpressionBodyForLambdasAnalyzerTests(ITestOutputHelper logger)
            : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExpressionBodyForLambdaDiagnosticAnalyzer(), new UseExpressionBodyForLambdaCodeFixProvider());

        private OptionsCollection UseExpressionBody
            => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement);

        private OptionsCollection UseBlockBody
            => this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.NeverWithSuggestionEnforcement);

        [Fact]
        public async Task UseExpressionBodyInMethod()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task TestMissingWhenAlreadyAndExpressionBody()
        {
            await TestMissingAsync(
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
        }

        [Fact]
        public async Task UseBlockBodyInMethod()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task MissingWhenAlreadyHasBlockBody()
        {
            await TestMissingAsync(
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
        }

        [Fact]
        public async Task UseExpressionBodyInArgument()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseBlockBodyInArgument()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseExpressionBodyFromReturnKeyword()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task UseExpressionBodyFromLambdaOpeningBrace()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task UseExpressionBodyFromLambdaClosingBrace()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task UseExpressionBodyThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseBlockBodyThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithVoidReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithVoidReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseBlockBodyWithVoidReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseBlockBodyWithVoidReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithAsyncVoidReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithAsyncVoidReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseBlockBodyWithAsyncVoidReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseBlockBodyWithAsyncVoidReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithTaskReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithTaskReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseBlockBodyWithTaskReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseBlockBodyWithTaskReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithAsyncTaskReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithAsyncTaskReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseBlockBodyWithAsyncTaskReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseBlockBodyWithAsyncTaskReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithTaskTReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithTaskTReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseBlockBodyWithTaskTReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseBlockBodyWithTaskTReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithAsyncTaskTReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithAsyncTaskTReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseBlockBodyWithAsyncTaskTReturn()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseBlockBodyWithAsyncTaskTReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithPrecedingComment()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseExpressionBodyWithEndingComment()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseBlockBodyWithEndingComment()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseExpressionBodyInMethod_FixAll1()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseExpressionBodyInMethod_FixAll2()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task UseBlockBodyInMethod_FixAll1()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task UseBlockBodyInMethod_FixAll2()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task FixAllNested1()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task FixAllNested2()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseExpressionBody);
        }

        [Fact]
        public async Task FixAllNested3()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }

        [Fact]
        public async Task FixAllNested4()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: UseBlockBody);
        }
    }
}
