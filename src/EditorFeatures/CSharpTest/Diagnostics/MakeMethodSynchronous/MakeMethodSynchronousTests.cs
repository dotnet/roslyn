using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.MakeMethodSynchronous
{
    public class MakeMethodSynchronousTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(null, new CSharpMakeMethodSynchronousCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestTaskReturnType()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task [|Foo|]()
    {
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestTaskOfTReturnType()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task<int> [|Foo|]()
    {
    }
}",
@"
using System.Threading.Tasks;

class C
{
    int Foo()
    {
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestSecondModifier()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    public async Task [|Foo|]()
    {
    }
}",
@"
using System.Threading.Tasks;

class C
{
    public void Foo()
    {
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestFirstModifier()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    async public Task [|Foo|]()
    {
    }
}",
@"
using System.Threading.Tasks;

class C
{
    public void Foo()
    {
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestRenameMethod()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task [|FooAsync|]()
    {
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestRenameMethod1()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task [|FooAsync|]()
    {
    }

    void Bar()
    {
        FooAsync();
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
    }

    void Bar()
    {
        Foo();
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestParenthesizedLambda()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<Task> f =
            async [|()|] => { };
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<Task> f =
            () => { };
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestSimpleLambda()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<string, Task> f =
            async [|a|] => { };
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<string, Task> f =
            a => { };
    }
}",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestAnonymousMethod()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<Task> f =
            async [|delegate|] { };
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<Task> f =
            delegate { };
    }
}",
compareTokens: false);
        }
    }
}