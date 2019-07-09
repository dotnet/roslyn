// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    public class UseExpressionBodyForLambdasAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new UseExpressionBodyForLambdaDiagnosticAnalyzer(), new UseExpressionBodyForLambdaCodeFixProvider());

        private IDictionary<OptionKey, object> UseExpressionBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement);

        private IDictionary<OptionKey, object> UseBlockBody =>
            this.Option(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyInMethod()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x [|=>|]
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
}", options: UseExpressionBody);
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
        Func<int, string> f = x [|=>|] x.ToString();
    }
}", new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyInMethod()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x [|=>|] x.ToString();
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
}", options: UseBlockBody);
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
        Func<int, string> f = x [|=>|] { return x.ToString(); };
    }
}", new TestParameters(options: UseBlockBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyInArgument()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void Goo()
    {
        TargetMethod(x => x.ToString());
    }

    void TargetMethod(Func<int, string> targetParam) { }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyInArgument()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        TargetMethod(x [|=>|] x.ToString());
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyFromReturnKeyword()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x =>
        {
            [|return|] x.ToString();
        };
    }
}", new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyFromLambdaOpeningBrace()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x =>
        [|{|]
            return x.ToString();
        };
    }
}", new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyFromLambdaClosingBrace()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x =>
        {
            return x.ToString();
        [|}|];
    }
}", new TestParameters(options: UseExpressionBody));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x [|=>|]
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
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, string> f = x [|=>|] throw null;
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithVoidReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = x [|=>|]
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
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithVoidReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = x [|=>|]
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
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithVoidReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = x [|=>|] x.ToString();
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithVoidReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = x [|=>|] throw null;
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncVoidReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = async x [|=>|]
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
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncVoidReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = async x [|=>|]
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
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncVoidReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = async x [|=>|] x.ToString();
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncVoidReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Action<int> f = async x [|=>|] throw null;
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithTaskReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
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
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = () => Task.CompletedTask;
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithTaskReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
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
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = () => throw null;
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithTaskReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = () [|=>|] Task.CompletedTask;
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithTaskReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = () [|=>|] throw null;
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncTaskReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
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
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = async () => await Task.CompletedTask;
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncTaskReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
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
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = async () => throw null;
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncTaskReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = async () [|=>|] await Task.CompletedTask;
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncTaskReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<Task> f = async () [|=>|] throw null;
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithTaskTReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
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
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = x => Task.FromResult(x.ToString());
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithTaskTReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
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
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = x => throw null;
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithTaskTReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = x [|=>|] Task.FromResult(x.ToString());
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithTaskTReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = x [|=>|] throw null;
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncTaskTReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
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
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = async x => await Task.FromResult(x.ToString());
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithAsyncTaskTReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
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
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = async x => throw null;
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncTaskTReturn()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = async x [|=>|] await Task.FromResult(x.ToString());
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithAsyncTaskTReturnThrowing()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, Task<string>> f = async x [|=>|] throw null;
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithPrecedingComment()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
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
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyWithEndingComment()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
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
}",
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, string> f = x => x.ToString();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyWithEndingComment()
        {
            await TestInRegular73AndScriptAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        Func<int, string> f = x [|=>|] x.ToString(); // Comment
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyInMethod_FixAll1()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = x => y => (x + y).ToString();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseExpressionBodyInMethod_FixAll2()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

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
}",
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = x => y => (x + y).ToString();
    }
}", options: UseExpressionBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyInMethod_FixAll1()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = x {|FixAllInDocument:=>|} y => (x + y).ToString();
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
}", options: UseBlockBody);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
        public async Task UseBlockBodyInMethod_FixAll2()
        {
            await TestInRegular73AndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Func<int, Func<int, string>> f = x => y {|FixAllInDocument:=>|} (x + y).ToString();
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
}", options: UseBlockBody);
        }
    }
}
