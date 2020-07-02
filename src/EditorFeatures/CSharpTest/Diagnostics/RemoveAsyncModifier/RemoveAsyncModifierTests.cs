// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.RemoveAsyncModifier.CSharpRemoveAsyncModifierCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.RemoveAsyncModifier
{
    public class RemoveAsyncModifierTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task TestTaskReturnType()
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

        return;
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
        public async Task TestTaskOfTReturnType()
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
        public async Task TestExpressionBodiedMethod()
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
        public async Task TestLocalFunction()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System.Threading.Tasks;

class C
{
    public void M1()
    {
        async Task {|CS1998:Goo|}()
        {
            return;
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
            return Task.CompletedTask;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)]
        public async Task TestExpressionBodiedLocalFunction()
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
        public async Task TestExpressionBodiedLambda()
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
        public async Task TestLambda()
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
        public async Task TestFullyQualifiedTaskReturnType()
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

        return;
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
        public async Task TestFullyQualifiedTaskOfTReturnType()
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
    }
}
