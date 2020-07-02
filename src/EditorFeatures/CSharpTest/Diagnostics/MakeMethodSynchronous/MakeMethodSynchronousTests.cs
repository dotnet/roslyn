// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.MakeMethodSynchronous.CSharpMakeMethodSynchronousCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.MakeMethodSynchronous
{
    public class MakeMethodSynchronousTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestTaskReturnType()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task {|CS1998:Goo|}()
    {
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestTaskOfTReturnType()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task<int> {|#0:{|CS1998:Goo|}|}()
    {
    }
}",
                // /0/Test0.cs(6,21): error CS0161: 'C.Goo()': not all code paths return a value
                DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("C.Goo()"),
@"
using System.Threading.Tasks;

class C
{
    int {|#0:Goo|}()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestSecondModifier()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    public async Task {|CS1998:Goo|}()
    {
    }
}",
@"
using System.Threading.Tasks;

class C
{
    public void Goo()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestFirstModifier()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async public Task {|CS1998:Goo|}()
    {
    }
}",
@"
using System.Threading.Tasks;

class C
{
    public void Goo()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestTrailingTrivia()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async // comment
    Task {|CS1998:Goo|}()
    {
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestRenameMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task {|CS1998:GooAsync|}()
    {
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestRenameMethod1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    async Task {|CS1998:GooAsync|}()
    {
    }

    void Bar()
    {
        GooAsync();
    }
}",
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
    }

    void Bar()
    {
        Goo();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestParenthesizedLambda()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        {|#0:Func<Task>|} f =
            async () {|CS1998:=>|} { };
    }
}",
                // /0/Test0.cs(8,9): error CS0246: The type or namespace name 'Func<>' could not be found (are you missing a using directive or an assembly reference?)
                DiagnosticResult.CompilerError("CS0246").WithLocation(0).WithArguments("Func<>"),
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        {|#0:Func<Task>|} f =
            () => { };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestSimpleLambda()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        {|#0:Func<string, Task>|} f =
            async a {|CS1998:=>|} { };
    }
}",
                // /0/Test0.cs(8,9): error CS0246: The type or namespace name 'Func<,>' could not be found (are you missing a using directive or an assembly reference?)
                DiagnosticResult.CompilerError("CS0246").WithLocation(0).WithArguments("Func<,>"),
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        {|#0:Func<string, Task>|} f =
            a => { };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestLambdaWithExpressionBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        {|#0:Func<string, Task>|} f =
            async a {|CS1998:=>|} 1;
    }
}",
                // /0/Test0.cs(8,9): error CS0246: The type or namespace name 'Func<,>' could not be found (are you missing a using directive or an assembly reference?)
                DiagnosticResult.CompilerError("CS0246").WithLocation(0).WithArguments("Func<,>"),
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        {|#0:Func<string, Task>|} f =
            a => 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestAnonymousMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        {|#0:Func<Task>|} f =
            async {|CS1998:delegate|} { };
    }
}",
                // /0/Test0.cs(8,9): error CS0246: The type or namespace name 'Func<>' could not be found (are you missing a using directive or an assembly reference?)
                DiagnosticResult.CompilerError("CS0246").WithLocation(0).WithArguments("Func<>"),
@"
using System.Threading.Tasks;

class C
{
    void Goo()
    {
        {|#0:Func<Task>|} f =
            delegate { };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestFixAll()
        {
            var source =
@"using System.Threading.Tasks;

public class Class1
{
    async Task {|CS1998:GooAsync|}()
    {
        BarAsync();
    }

    async Task<int> {|#0:{|CS1998:BarAsync|}|}()
    {
        GooAsync();
    }
}";
            var expected =
@"using System.Threading.Tasks;

public class Class1
{
    void Goo()
    {
        Bar();
    }

    int {|#0:Bar|}()
    {
        Goo();
    }
}";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(10,21): error CS0161: 'Class1.BarAsync()': not all code paths return a value
                        DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("Class1.BarAsync()"),
                    },
                },
                FixedState =
                {
                    Sources = { expected },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(10,9): error CS0161: 'Class1.Bar()': not all code paths return a value
                        DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("Class1.Bar()"),
                    },
                },
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCaller1()
        {
            var source =
@"using System.Threading.Tasks;

public class Class1
{
    async Task {|CS1998:GooAsync|}()
    {
    }

    async void BarAsync()
    {
        await GooAsync();
    }
}";
            var expected =
@"using System.Threading.Tasks;

public class Class1
{
    void Goo()
    {
    }

    async void {|CS1998:BarAsync|}()
    {
        Goo();
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState =
                {
                    Sources = { expected },
                    MarkupHandling = MarkupMode.Allow,
                },
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCaller2()
        {
            var source =
@"using System.Threading.Tasks;

public class Class1
{
    async Task {|CS1998:GooAsync|}()
    {
    }

    async void BarAsync()
    {
        await GooAsync().ConfigureAwait(false);
    }
}";
            var expected =
@"using System.Threading.Tasks;

public class Class1
{
    void Goo()
    {
    }

    async void {|CS1998:BarAsync|}()
    {
        Goo();
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState =
                {
                    Sources = { expected },
                    MarkupHandling = MarkupMode.Allow,
                },
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCaller3()
        {
            var source =
@"using System.Threading.Tasks;

public class Class1
{
    async Task {|CS1998:GooAsync|}()
    {
    }

    async void BarAsync()
    {
        await this.GooAsync();
    }
}";
            var expected =
@"using System.Threading.Tasks;

public class Class1
{
    void Goo()
    {
    }

    async void {|CS1998:BarAsync|}()
    {
        this.Goo();
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState =
                {
                    Sources = { expected },
                    MarkupHandling = MarkupMode.Allow,
                },
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCaller4()
        {
            var source =
@"using System.Threading.Tasks;

public class Class1
{
    async Task {|CS1998:GooAsync|}()
    {
    }

    async void BarAsync()
    {
        await this.GooAsync().ConfigureAwait(false);
    }
}";
            var expected =
@"using System.Threading.Tasks;

public class Class1
{
    void Goo()
    {
    }

    async void {|CS1998:BarAsync|}()
    {
        this.Goo();
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState =
                {
                    Sources = { expected },
                    MarkupHandling = MarkupMode.Allow,
                },
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCallerNested1()
        {
            var source =
@"using System.Threading.Tasks;

public class Class1
{
    async Task<int> {|#0:{|CS1998:GooAsync|}|}(int i)
    {
    }

    async void BarAsync()
    {
        await this.GooAsync(await this.GooAsync(0));
    }
}";
            var expected =
@"using System.Threading.Tasks;

public class Class1
{
    int {|#0:Goo|}(int i)
    {
    }

    async void {|CS1998:BarAsync|}()
    {
        this.Goo(this.Goo(0));
    }
}";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(5,21): error CS0161: 'Class1.GooAsync(int)': not all code paths return a value
                        DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("Class1.GooAsync(int)"),
                    },
                },
                FixedState =
                {
                    Sources = { expected },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(5,9): error CS0161: 'Class1.Goo(int)': not all code paths return a value
                        DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("Class1.Goo(int)"),
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        [WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")]
        public async Task TestRemoveAwaitFromCallerNested()
        {
            var source =
@"using System.Threading.Tasks;

public class Class1
{
    async Task<int> {|#0:{|CS1998:GooAsync|}|}(int i)
    {
    }

    async void BarAsync()
    {
        await this.GooAsync(await this.GooAsync(0).ConfigureAwait(false)).ConfigureAwait(false);
    }
}";
            var expected =
@"using System.Threading.Tasks;

public class Class1
{
    int {|#0:Goo|}(int i)
    {
    }

    async void {|CS1998:BarAsync|}()
    {
        this.Goo(this.Goo(0));
    }
}";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(5,21): error CS0161: 'Class1.GooAsync(int)': not all code paths return a value
                        DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("Class1.GooAsync(int)"),
                    },
                },
                FixedState =
                {
                    Sources = { expected },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(5,9): error CS0161: 'Class1.Goo(int)': not all code paths return a value
                        DiagnosticResult.CompilerError("CS0161").WithLocation(0).WithArguments("Class1.Goo(int)"),
                    },
                    MarkupHandling = MarkupMode.Allow,
                },
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodAsynchronous)]
        [WorkItem(14133, "https://github.com/dotnet/roslyn/issues/14133")]
        public async Task RemoveAsyncInLocalFunction()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System.Threading.Tasks;

class C
{
    public void M1()
    {
        async Task {|CS1998:M2Async|}()
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
            await VerifyCS.VerifyCodeFixAsync(
$@"using System;
using System.Threading.Tasks;

class C
{{
    public void M1()
    {{
        // Leading trivia
        /*1*/ async {asyncReturn} /*2*/ {{|CS1998:M2Async|}}/*3*/() /*4*/
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
        /*1*/
        {expectedReturn} /*2*/ M2/*3*/() /*4*/
        {{
            throw new NotImplementedException();
        }}
    }}
}}");
        }

        [Theory]
        [InlineData("", "Task<C>", "\r\n    C")]
        [InlineData("", "Task<int>", "\r\n    int")]
        [InlineData("", "Task", "\r\n    void")]
        [InlineData("", "void", "\r\n    void")]
        [InlineData("public", "Task<C>", " C")]
        [InlineData("public", "Task<int>", " int")]
        [InlineData("public", "Task", " void")]
        [InlineData("public", "void", " void")]
        [Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodAsynchronous)]
        [WorkItem(18307, "https://github.com/dotnet/roslyn/issues/18307")]
        public async Task RemoveAsyncKeepsTrivia(string modifiers, string asyncReturn, string expectedReturn)
        {
            await VerifyCS.VerifyCodeFixAsync(
$@"using System;
using System.Threading.Tasks;

class C
{{
    // Leading trivia
    {modifiers}/*1*/ async {asyncReturn} /*2*/ {{|CS1998:M2Async|}}/*3*/() /*4*/
    {{
        throw new NotImplementedException();
    }}
}}",
$@"using System;
using System.Threading.Tasks;

class C
{{
    // Leading trivia
    {modifiers}/*1*/{expectedReturn} /*2*/ M2/*3*/() /*4*/
    {{
        throw new NotImplementedException();
    }}
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task MethodWithUsingAwait()
        {
            var source =
@"class C
{
    async System.Threading.Tasks.Task MAsync()
    {
        await using ({|#0:var x = new object()|})
        {
        }
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21,
                TestCode = source,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(5,22): error CS8410: 'object': type used in an asynchronous using statement must be implicitly convertible to 'System.IAsyncDisposable' or implement a suitable 'DisposeAsync' method.
                    DiagnosticResult.CompilerError("CS8410").WithLocation(0).WithArguments("object"),
                },
                FixedCode = source,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task MethodWithUsingNoAwait()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    async System.Threading.Tasks.Task {|CS1998:MAsync|}()
    {
        using ({|#0:var x = new object()|})
        {
        }
    }
}",
                // /0/Test0.cs(5,16): error CS1674: 'object': type used in a using statement must be implicitly convertible to 'System.IDisposable'.
                DiagnosticResult.CompilerError("CS1674").WithLocation(0).WithArguments("object"),
@"class C
{
    void M()
    {
        using ({|#0:var x = new object()|})
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task MethodWithAwaitForEach()
        {
            var source =
@"class C
{
    async System.Threading.Tasks.Task MAsync()
    {
        await foreach (var n in {|#0:new int[] { }|})
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                // /0/Test0.cs(5,33): error CS1061: 'bool' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'bool' could be found (are you missing a using directive or an assembly reference?)
                DiagnosticResult.CompilerError("CS1061").WithLocation(0).WithArguments("bool", "GetAwaiter"),
                source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task MethodWithForEachNoAwait()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    async System.Threading.Tasks.Task {|CS1998:MAsync|}()
    {
        foreach (var n in new int[] { })
        {
        }
    }
}",
@"class C
{
    void M()
    {
        foreach (var n in new int[] { })
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task MethodWithForEachVariableAwait()
        {
            var source =
@"class C
{
    async System.Threading.Tasks.Task MAsync()
    {
        await foreach (var (a, b) in {|#0:new(int, int)[] { }|})
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                // /0/Test0.cs(5,38): error CS1061: 'bool' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'bool' could be found (are you missing a using directive or an assembly reference?)
                DiagnosticResult.CompilerError("CS1061").WithLocation(0).WithArguments("bool", "GetAwaiter"),
                source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task MethodWithForEachVariableNoAwait()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    async System.Threading.Tasks.Task {|CS1998:MAsync|}()
    {
        foreach (var (a, b) in new(int, int)[] { })
        {
        }
    }
}",
@"class C
{
    void M()
    {
        foreach (var (a, b) in new (int, int)[] { })
        {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestIAsyncEnumerableReturnType()
        {
            var source =
@"
using System.Threading.Tasks;
using System.Collections.Generic;

class C
{
    async IAsyncEnumerable<int> {|CS1998:MAsync|}()
    {
        yield return 1;
    }
}";
            var expected =
@"
using System.Threading.Tasks;
using System.Collections.Generic;

class C
{
    IEnumerable<int> M()
    {
        yield return 1;
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21,
                TestCode = source,
                FixedCode = expected,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)]
        public async Task TestIAsyncEnumeratorReturnTypeOnLocalFunction()
        {
            var source =
@"
using System.Threading.Tasks;
using System.Collections.Generic;

class C
{
    void Method()
    {
        async IAsyncEnumerator<int> {|CS1998:MAsync|}()
        {
            yield return 1;
        }
    }
}";
            var expected =
@"
using System.Threading.Tasks;
using System.Collections.Generic;

class C
{
    void Method()
    {
        IEnumerator<int> M()
        {
            yield return 1;
        }
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard21,
                TestCode = source,
                FixedCode = expected,
            }.RunAsync();
        }
    }
}
