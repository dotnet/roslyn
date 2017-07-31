// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpMakeMethodSynchronousCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestTaskReturnType()
        {
            await TestInRegularAndScriptAsync(
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestTaskOfTReturnType()
        {
            await TestInRegularAndScriptAsync(
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestSecondModifier()
        {
            await TestInRegularAndScriptAsync(
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestFirstModifier()
        {
            await TestInRegularAndScriptAsync(
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestTrailingTrivia()
        {
            await TestInRegularAndScriptAsync(
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestRenameMethod()
        {
            await TestInRegularAndScriptAsync(
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestRenameMethod1()
        {
            await TestInRegularAndScriptAsync(
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestParenthesizedLambda()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<Task> f =
            async () [|=>|] { };
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestSimpleLambda()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<string, Task> f =
            async a [|=>|] { };
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestLambdaWithExpressionBody()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Threading.Tasks;

class C
{
    void Foo()
    {
        Func<string, Task> f =
            async a [|=>|] 1;
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestAnonymousMethod()
        {
            await TestInRegularAndScriptAsync(
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
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestFixAll()
        {
            await TestInRegularAndScriptAsync(
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
}", ignoreTrivia: false, fixAllActionEquivalenceKey: AbstractMakeMethodSynchronousCodeFixProvider.EquivalenceKey);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCaller1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

public class Class1
{
    async Task [|FooAsync|]()
    {
    }

    async void BarAsync()
    {
        await FooAsync();
    }
}",
@"using System.Threading.Tasks;

public class Class1
{
    void Foo()
    {
    }

    async void BarAsync()
    {
        Foo();
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCaller2()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

public class Class1
{
    async Task [|FooAsync|]()
    {
    }

    async void BarAsync()
    {
        await FooAsync().ConfigureAwait(false);
    }
}",
@"using System.Threading.Tasks;

public class Class1
{
    void Foo()
    {
    }

    async void BarAsync()
    {
        Foo();
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCaller3()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

public class Class1
{
    async Task [|FooAsync|]()
    {
    }

    async void BarAsync()
    {
        await this.FooAsync();
    }
}",
@"using System.Threading.Tasks;

public class Class1
{
    void Foo()
    {
    }

    async void BarAsync()
    {
        this.Foo();
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCaller4()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

public class Class1
{
    async Task [|FooAsync|]()
    {
    }

    async void BarAsync()
    {
        await this.FooAsync().ConfigureAwait(false);
    }
}",
@"using System.Threading.Tasks;

public class Class1
{
    void Foo()
    {
    }

    async void BarAsync()
    {
        this.Foo();
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCallerNested1()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

public class Class1
{
    async Task<int> [|FooAsync|](int i)
    {
    }

    async void BarAsync()
    {
        await this.FooAsync(await this.FooAsync(0));
    }
}",
@"using System.Threading.Tasks;

public class Class1
{
    int Foo(int i)
    {
    }

    async void BarAsync()
    {
        this.Foo(this.Foo(0));
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCallerNested()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

public class Class1
{
    async Task<int> [|FooAsync|](int i)
    {
    }

    async void BarAsync()
    {
        await this.FooAsync(await this.FooAsync(0).ConfigureAwait(false)).ConfigureAwait(false);
    }
}",
@"using System.Threading.Tasks;

public class Class1
{
    int Foo(int i)
    {
    }

    async void BarAsync()
    {
        this.Foo(this.Foo(0));
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodAsynchronous)]
        [WorkItem(14133, "https://github.com/dotnet/roslyn/issues/14133")]
        public async Task RemoveAsyncInLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class C
{
    public void M1()
    {
        async Task [|M2Async|]()
        {
        }
    }
}",
@"using System.Threading.Tasks;

class C
{
    public void M1()
    {
        void M2()
        {
        }
    }
}");
        }

        [Theory]
        [InlineData("Task<C>", "C")]
        [InlineData("Task<int>", "int")]
        [InlineData("Task", "void")]
        [InlineData("void", "void")]
        [Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodAsynchronous)]
        [WorkItem(18307, "https://github.com/dotnet/roslyn/issues/18307")]
        public async Task RemoveAsyncInLocalFunctionKeepsTrivia(string asyncReturn, string expectedReturn)
        {
            await TestInRegularAndScriptAsync(
$@"using System;
using System.Threading.Tasks;

class C
{{
    public void M1()
    {{
        // Leading trivia
        /*1*/ async {asyncReturn} /*2*/ [|M2Async|]/*3*/() /*4*/
        {{
            throw new NotImplementedException();
        }}
    }}
}}",
$@"using System;
using System.Threading.Tasks;

class C
{{
    public void M1()
    {{
        // Leading trivia
        /*1*/ {expectedReturn} /*2*/ M2/*3*/() /*4*/
        {{
            throw new NotImplementedException();
        }}
    }}
}}");
        }

        [Theory]
        [InlineData("", "Task<C>", "C")]
        [InlineData("", "Task<int>", "int")]
        [InlineData("", "Task", "void")]
        [InlineData("", "void", "void")]
        [InlineData("public", "Task<C>", "C")]
        [InlineData("public", "Task<int>", "int")]
        [InlineData("public", "Task", "void")]
        [InlineData("public", "void", "void")]
        [Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodAsynchronous)]
        [WorkItem(18307, "https://github.com/dotnet/roslyn/issues/18307")]
        public async Task RemoveAsyncKeepsTrivia(string modifiers, string asyncReturn, string expectedReturn)
        {
            await TestInRegularAndScriptAsync(
$@"using System;
using System.Threading.Tasks;

class C
{{
    // Leading trivia
    {modifiers}/*1*/ async {asyncReturn} /*2*/ [|M2Async|]/*3*/() /*4*/
    {{
        throw new NotImplementedException();
    }}
}}",
$@"using System;
using System.Threading.Tasks;

class C
{{
    // Leading trivia
    {modifiers}/*1*/ {expectedReturn} /*2*/ M2/*3*/() /*4*/
    {{
        throw new NotImplementedException();
    }}
}}");
        }
    }
}
