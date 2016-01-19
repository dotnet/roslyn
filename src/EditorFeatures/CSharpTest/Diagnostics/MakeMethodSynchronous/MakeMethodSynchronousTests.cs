// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MakeMethodSynchronous;
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
        public async Task TestTrailingTrivia()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    async // comment
    Task [|Foo|]()
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
        public async Task TestLambdaWithExpressionBody()
        {
            await TestAsync(
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<string, Task> f =
            async [|a|] => 1;
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<string, Task> f =
            a => 1;
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestFixAll()
        {
            await TestAsync(
@"using System.Threading.Tasks;

public class Class1
{
    {|FixAllInDocument:async Task FooAsync()
    {
        BarAsync();
    }

    async Task<int> BarAsync()
    {
        FooAsync();
    }|}
}",
@"using System.Threading.Tasks;

public class Class1
{
    void Foo()
    {
        Bar();
    }

    int Bar()
    {
        Foo();
    }
}", compareTokens: false, fixAllActionEquivalenceKey: AbstractMakeMethodSynchronousCodeFixProvider.EquivalenceKey);
        }
    }
}