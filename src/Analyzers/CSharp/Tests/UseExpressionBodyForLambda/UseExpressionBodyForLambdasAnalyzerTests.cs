// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    using VerifyCS = CSharpCodeFixVerifier<UseExpressionBodyForLambdaDiagnosticAnalyzer, UseExpressionBodyForLambdaCodeFixProvider>;

    public class UseExpressionBodyForLambdasAnalyzerTests
    {
        private static readonly CodeStyleOption2<ExpressionBodyPreference> s_preferExpressionBody = CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement;
        private static readonly CodeStyleOption2<ExpressionBodyPreference> s_preferBlockBody = CSharpCodeStyleOptions.NeverWithSilentEnforcement;

        private static async Task TestInRegularAndScriptAsync(string source, string fixedSource, CodeStyleOption2<ExpressionBodyPreference> option)
        {
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, option } },
            }.RunAsync();
        }

        private static async Task TestMissingAsync(string source, CodeStyleOption2<ExpressionBodyPreference> option)
        {
            await TestInRegularAndScriptAsync(source, source, option);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyInMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = [|x =>|]
        {
            return x.ToString();
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x => x.ToString();
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task TestMissingWhenAlreadyAndExpressionBody()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x => x.ToString();
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyInMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = [|x =>|] x.ToString();
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x =>
        {
            return x.ToString();
        };
    }
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task MissingWhenAlreadyHasBlockBody()
        {
            await TestMissingAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x => { return x.ToString(); };
    }
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyInArgument()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        TargetMethod([|x =>|]
        {
            return x.ToString();
        });
    }

    void TargetMethod(Func<int, string> targetParam) { }
}",
@"using System;

class C
{
    void Goo()
    {
        TargetMethod(x => x.ToString());
    }

    void TargetMethod(Func<int, string> targetParam) { }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyInArgument()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        TargetMethod([|x =>|] x.ToString());
    }

    void TargetMethod(Func<int, string> targetParam) { }
}",
@"using System;

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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = [|x =>|]
        {
            throw null;
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x => throw null;
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = [|x =>|] throw null;
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x =>
        {
            throw null;
        };
    }
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithVoidReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = [|x =>|]
        {
            x.ToString();
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = x => x.ToString();
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithVoidReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = [|x =>|]
        {
            throw null;
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = x => throw null;
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithVoidReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = [|x =>|] x.ToString();
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = x =>
        {
            x.ToString();
        };
    }
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithVoidReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = [|x =>|] throw null;
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = x =>
        {
            throw null;
        };
    }
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncVoidReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = [|async x =>|]
        {
            x.ToString();
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = async x => x.ToString();
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncVoidReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = [|async x =>|]
        {
            throw null;
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = async x => throw null;
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncVoidReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = [|async x =>|] x.ToString();
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = async x =>
        {
            x.ToString();
        };
    }
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncVoidReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = [|async x =>|] throw null;
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = async x =>
        {
            throw null;
        };
    }
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithTaskReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = [|() =>|]
        {
            return Task.CompletedTask;
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = () => Task.CompletedTask;
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithTaskReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = [|() =>|]
        {
            throw null;
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = () => throw null;
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithTaskReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = [|() =>|] Task.CompletedTask;
    }
}",
@"using System;
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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithTaskReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = [|() =>|] throw null;
    }
}",
@"using System;
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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncTaskReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = [|async () =>|]
        {
            await Task.CompletedTask;
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = async () => await Task.CompletedTask;
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncTaskReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = [|async () =>|]
        {
            throw null;
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = async () => throw null;
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncTaskReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = [|async () =>|] await Task.CompletedTask;
    }
}",
@"using System;
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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncTaskReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = [|async () =>|] throw null;
    }
}",
@"using System;
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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithTaskTReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = [|x =>|]
        {
            return Task.FromResult(x.ToString());
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = x => Task.FromResult(x.ToString());
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithTaskTReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = [|x =>|]
        {
            throw null;
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = x => throw null;
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithTaskTReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = [|x =>|] Task.FromResult(x.ToString());
    }
}",
@"using System;
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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithTaskTReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = [|x =>|] throw null;
    }
}",
@"using System;
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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncTaskTReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = [|async x =>|]
        {
            return await Task.FromResult(x.ToString());
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = async x => await Task.FromResult(x.ToString());
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncTaskTReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = [|async x =>|]
        {
            throw null;
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = async x => throw null;
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncTaskTReturn()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = [|async x =>|] await Task.FromResult(x.ToString());
    }
}",
@"using System;
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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncTaskTReturnThrowing()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = [|async x =>|] throw null;
    }
}",
@"using System;
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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithPrecedingComment()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, string> f = [|x =>|]
        {
            // Comment
            return x.ToString();
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, string> f = x =>
            // Comment
            x.ToString();
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithEndingComment()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, string> f = [|x =>|]
        {
            return x.ToString(); // Comment
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, string> f = x => x.ToString();
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithEndingComment()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, string> f = [|x =>|] x.ToString(); // Comment
    }
}",
@"using System;
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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyInMethod_FixAll1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = [|x =>|]
        {
            return [|y =>|]
            {
                return (x + y).ToString();
            };
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = x => y => (x + y).ToString();
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyInMethod_FixAll2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = [|x =>|]
        {
            return [|y =>|]
            {
                return (x + y).ToString();
            };
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = x => y => (x + y).ToString();
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyInMethod_FixAll1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = [|x =>|] [|y =>|] (x + y).ToString();
    }
}",
@"using System;

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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyInMethod_FixAll2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = [|x =>|] [|y =>|] (x + y).ToString();
    }
}",
@"using System;

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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task FixAllNested1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = [|a =>|]
        {
            return [|b =>|]
            {
                return b.ToString();
            };
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = a => b => b.ToString();
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task FixAllNested2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = [|a =>|]
        {
            return [|b =>|]
            {
                return b.ToString();
            };
        };
    }
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = a => b => b.ToString();
    }
}", s_preferExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task FixAllNested3()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = [|a =>|] [|b =>|] b.ToString();
    }
}",
@"using System;

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
}", s_preferBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task FixAllNested4()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = [|a =>|] [|b =>|] b.ToString();
    }
}",
@"using System;

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
}", s_preferBlockBody);
        }
    }
}
