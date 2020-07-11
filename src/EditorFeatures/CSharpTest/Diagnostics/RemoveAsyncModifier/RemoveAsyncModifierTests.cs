// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.RemoveAsyncModifier
{
    using VerifyCS = Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
        EmptyDiagnosticAnalyzer,
        CodeAnalysis.CSharp.RemoveAsyncModifier.CSharpRemoveAsyncModifierCodeFixProvider>;

    public class RemoveAsyncModifierTests : CodeAnalysis.CSharp.Test.Utilities.CSharpTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task Method_Task_BlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task {|CS1998:Goo|}()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            return;
        }
    }
}",
@"
using System.Threading.Tasks;

class C
{
    Task Goo()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task Method_Task_BlockBody_Throws()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task {|CS1998:Goo|}()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            return;
        }

        throw new System.ApplicationException();
    }
}",
@"
using System.Threading.Tasks;

class C
{
    Task Goo()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            return Task.CompletedTask;
        }

        throw new System.ApplicationException();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task Method_Task_BlockBody_WithLocalFunction()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task {|CS1998:Goo|}()
    {
        if (GetTicks() > 0)
        {
            return;
        }

        long GetTicks()
        {
            return System.DateTime.Now.Ticks;
        }
    }
}",
@"
using System.Threading.Tasks;

class C
{
    Task Goo()
    {
        if (GetTicks() > 0)
        {
            return Task.CompletedTask;
        }

        long GetTicks()
        {
            return System.DateTime.Now.Ticks;
        }

        return Task.CompletedTask;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task Method_Task_BlockBody_WithLambda()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task {|CS1998:Goo|}()
    {
        System.Func<long> getTicks = () => {
            return System.DateTime.Now.Ticks;
        };

        if (getTicks() > 0)
        {
            return;
        }

    }
}",
@"
using System.Threading.Tasks;

class C
{
    Task Goo()
    {
        System.Func<long> getTicks = () => {
            return System.DateTime.Now.Ticks;
        };

        if (getTicks() > 0)
        {
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task Method_TaskOfT_BlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task<int> {|CS1998:Goo|}()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            return 2;
        }

        return 3;
    }
}",
@"
using System.Threading.Tasks;

class C
{
    Task<int> Goo()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            return Task.FromResult(2);
        }

        return Task.FromResult(3);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task Method_TaskOfT_ExpressionBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task<int> {|CS1998:Goo|}() => 2;
}",
@"
using System.Threading.Tasks;

class C
{
    Task<int> Goo() => Task.FromResult(2);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task Method_Task_ExpressionBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;
using System.Threading.Tasks;

class C
{
    async Task {|CS1998:Goo|}() => Console.WriteLine(""Hello"");
}",
@"
using System;
using System.Threading.Tasks;

class C
{
    Task Goo()
    {
        Console.WriteLine(""Hello"");
        return Task.CompletedTask;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task LocalFunction_Task_BlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System.Threading.Tasks;

class C
{
    public void M1()
    {
        async Task {|CS1998:Goo|}()
        {
            if (System.DateTime.Now.Ticks > 0)
            {
                return;
            }
        }
    }
}",
@"using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Task Goo()
        {
            if (System.DateTime.Now.Ticks > 0)
            {
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task LocalFunction_Task_ExpressionBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        async Task {|CS1998:Goo|}() => Console.WriteLine(1);
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Task Goo() { Console.WriteLine(1); return Task.CompletedTask; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task LocalFunction_TaskOfT_BlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System.Threading.Tasks;

class C
{
    public void M1()
    {
        async Task<int> {|CS1998:Goo|}()
        {
            return 1;
        }
    }
}",
@"using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Task<int> Goo()
        {
            return Task.FromResult(1);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task LocalFunction_TaskOfT_ExpressionBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System.Threading.Tasks;

class C
{
    public void M1()
    {
        async Task<int> {|CS1998:Goo|}() => 1;
    }
}",
@"using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Task<int> Goo() => Task.FromResult(1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task AnonymousFunction_Task_BlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task> foo = (Func<Task>)async {|CS1998:delegate|}
        {
            if (System.DateTime.Now.Ticks > 0)
            {
                return;
            }
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task> foo = (Func<Task>)delegate
        {
            if (System.DateTime.Now.Ticks > 0)
            {
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task AnonymousFunction_TaskOfT_BlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task<int>> foo = (Func<Task<int>>)async {|CS1998:delegate|}
        {
            return 1;
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task<int>> foo = (Func<Task<int>>)delegate
        {
            return Task.FromResult(1);
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task SimpleLambda_TaskOfT_ExpressionBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<int, Task<int>> foo = async x {|CS1998:=>|} 1;
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<int, Task<int>> foo = x => Task.FromResult(1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task SimpleLambda_TaskOfT_BlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<int, Task<int>> foo = async x {|CS1998:=>|} {
            return 1;
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<int, Task<int>> foo = x => {
            return Task.FromResult(1);
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task SimpleLambda_Task_ExpressionBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<int, Task> foo = async x {|CS1998:=>|} Console.WriteLine(1);
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<int, Task> foo = x => { Console.WriteLine(1); return Task.CompletedTask; };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task SimpleLambda_Task_BlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<int, Task> foo = async x {|CS1998:=>|} {
            if (System.DateTime.Now.Ticks > 0)
            {
                return;
            }
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<int, Task> foo = x => {
            if (System.DateTime.Now.Ticks > 0)
            {
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task ParenthesisedLambda_TaskOfT_ExpressionBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task<int>> foo = async () {|CS1998:=>|} 1;
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task<int>> foo = () => Task.FromResult(1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task ParenthesisedLambda_TaskOfT_BlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task<int>> foo = async () {|CS1998:=>|} {
            return 1;
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task<int>> foo = () => {
            return Task.FromResult(1);
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task ParenthesisedLambda_Task_ExpressionBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task> foo = async () {|CS1998:=>|} Console.WriteLine(1);
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task> foo = () => { Console.WriteLine(1); return Task.CompletedTask; };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task ParenthesisedLambda_Task_BlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task> foo = async () {|CS1998:=>|} {
            if (System.DateTime.Now.Ticks > 0)
            {
                return;
            }
        };
    }
}",
@"using System;
using System.Threading.Tasks;

class C
{
    public void M1()
    {
        Func<Task> foo = () => {
            if (System.DateTime.Now.Ticks > 0)
            {
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task Method_Task_BlockBody_FullyQualified()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    async System.Threading.Tasks.Task {|CS1998:Goo|}()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            return;
        }
    }
}",
@"
class C
{
    System.Threading.Tasks.Task Goo()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        return System.Threading.Tasks.Task.CompletedTask;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task Method_TaskOfT_BlockBody_FullyQualified()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    async System.Threading.Tasks.Task<int> {|CS1998:Goo|}()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            return 1;
        }

        return 2;
    }
}",
    @"
class C
{
    System.Threading.Tasks.Task<int> Goo()
    {
        if (System.DateTime.Now.Ticks > 0)
        {
            return System.Threading.Tasks.Task.FromResult(1);
        }

        return System.Threading.Tasks.Task.FromResult(2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task IAsyncEnumerable_Missing()
        {
            var source = @"
using System.Threading.Tasks;
using System.Collections.Generic;

class C
{
    async IAsyncEnumerable<int> M()
    {
        yield return 1;
    }
}" + AsyncStreamsTypes;

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21,
                TestCode = source,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(7,33): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                    DiagnosticResult.CompilerWarning("CS1998").WithSpan(7, 33, 7, 34),
                },
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task Method_AsyncVoid_Missing()
        {
            var source = @"
using System.Threading.Tasks;

class C
{
    async void M()
    {
        System.Console.WriteLine(1);
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21,
                TestCode = source,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(6,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                    DiagnosticResult.CompilerWarning("CS1998").WithSpan(6, 16, 6, 17),
                },
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task ParenthesisedLambda_AsyncVoid_Missing()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Action a = async () => Console.WriteLine(1);
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21,
                TestCode = source,
                ExpectedDiagnostics =
                {
                     // /0/Test0.cs(9,29): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                    DiagnosticResult.CompilerWarning("CS1998").WithSpan(9, 29, 9, 31),
                },
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task SimpleLambda_AsyncVoid_Missing()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Action<int> a = async x => Console.WriteLine(x);
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21,
                TestCode = source,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(9,33): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                    DiagnosticResult.CompilerWarning("CS1998").WithSpan(9, 33, 9, 35),
                },
                FixedCode = source,
            }.RunAsync();
        }
    }
}
