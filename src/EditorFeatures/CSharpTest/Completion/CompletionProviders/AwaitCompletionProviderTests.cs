// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

/// <summary>
/// The <see cref="AwaitCompletionProvider"/> adds async modifier if the return type is Task or ValueTask. The tests
/// here are only checking whether the completion item is provided or not. Tests for checking adding async modifier are
/// in: src/EditorFeatures/Test2/IntelliSense/CSharpCompletionCommandHandlerTests_AwaitCompletion.vb
/// </summary>
[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class AwaitCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType() => typeof(AwaitCompletionProvider);

    private const string CompletionDisplayTextAwait = "await";
    private const string CompletionDisplayTextAwaitAndConfigureAwait = "awaitf";

    private async Task VerifyAbsenceAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code)
    {
        await VerifyItemIsAbsentAsync(code, CompletionDisplayTextAwait);
        await VerifyItemIsAbsentAsync(code, CompletionDisplayTextAwaitAndConfigureAwait);
    }

    private async Task VerifyAbsenceAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code, LanguageVersion languageVersion = LanguageVersion.Default)
    {
        await VerifyItemIsAbsentAsync(GetMarkup(code, languageVersion), CompletionDisplayTextAwait);
        await VerifyItemIsAbsentAsync(GetMarkup(code, languageVersion), CompletionDisplayTextAwaitAndConfigureAwait);
    }

    private async Task VerifyKeywordAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code, LanguageVersion languageVersion = LanguageVersion.Default, string? inlineDescription = null, bool dotAwait = false, bool dotAwaitf = false)
    {
        var expectedDescription = dotAwait
            ? GetDescription(CompletionDisplayTextAwait, FeaturesResources.Await_the_preceding_expression)
            : GetDescription(CompletionDisplayTextAwait, FeaturesResources.Asynchronously_waits_for_the_task_to_finish);
        await VerifyItemExistsAsync(GetMarkup(code, languageVersion), CompletionDisplayTextAwait, glyph: Glyph.Keyword, expectedDescriptionOrNull: expectedDescription, inlineDescription: inlineDescription);

        if (dotAwaitf)
        {
            expectedDescription = string.Format(FeaturesResources.Await_the_preceding_expression_and_add_ConfigureAwait_0, "false");
            await VerifyItemExistsAsync(GetMarkup(code, languageVersion), CompletionDisplayTextAwaitAndConfigureAwait, glyph: Glyph.Keyword, expectedDescriptionOrNull: expectedDescription, inlineDescription: inlineDescription);
        }
        else
        {
            await VerifyItemIsAbsentAsync(GetMarkup(code, languageVersion), CompletionDisplayTextAwaitAndConfigureAwait);
        }

        static string GetDescription(string keyword, string tooltip)
            => $"{string.Format(FeaturesResources._0_Keyword, keyword)}\r\n{tooltip}";
    }

    [Fact]
    public Task TestNotInTypeContext()
        => VerifyAbsenceAsync("""
            class Program
            {
                $$
            }
            """);

    [Fact]
    public Task TestStatementInMethod()
        => VerifyKeywordAsync("""
            class C
            {
              void F()
              {
                $$  }
            }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestStatementInMethod_Async()
        => VerifyKeywordAsync("""
            class C
            {
              async Task F()
              {
                $$  }
            }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestStatementInMethod_TopLevel()
        => VerifyKeywordAsync("$$", LanguageVersion.CSharp9);

    [Fact]
    public Task TestExpressionInAsyncMethod()
        => VerifyKeywordAsync("""
            class C
            {
              async Task F()
              {
                var z = $$  }
            }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestExpressionInNonAsyncMethodWithTaskReturn()
        => VerifyKeywordAsync("""
            class C
            {
              Task F()
              {
                var z = $$  }
            }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestExpressionInAsyncMethod_TopLevel()
        => VerifyKeywordAsync("var z = $$", LanguageVersion.CSharp9);

    [Fact]
    public Task TestUsingStatement()
        => VerifyAbsenceAsync("""
            class C
            {
              async Task F()
              {
                using $$  }
            }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestUsingStatement_TopLevel()
        => VerifyAbsenceAsync("using $$", LanguageVersion.CSharp9);

    [Fact]
    public async Task TestUsingDirective()
        => await VerifyAbsenceAsync("using $$");

    [Fact]
    public async Task TestGlobalUsingDirective()
        => await VerifyAbsenceAsync("global using $$");

    [Fact]
    public Task TestForeachStatement()
        => VerifyAbsenceAsync("""
            class C
            {
              async Task F()
              {
                foreach $$  }
            }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestForeachStatement_TopLevel()
        => VerifyAbsenceAsync("foreach $$", LanguageVersion.CSharp9);

    [Fact]
    public Task TestNotInQuery()
        => VerifyAbsenceAsync("""
            class C
            {
              async Task F()
              {
                var z = from a in "char"
                      select $$  }
                }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestNotInQuery_TopLevel()
        => VerifyAbsenceAsync(
            """
            var z = from a in "char"
                      select $$
            """, LanguageVersion.CSharp9);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
    public Task TestInFinally()
        => VerifyKeywordAsync("""
            class C
            {
              async Task F()
              {
                try { }
            finally { $$ }  }
            }
            """, LanguageVersion.CSharp9);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
    public Task TestInFinally_TopLevel()
        => VerifyKeywordAsync(
            """
            try { }
            finally { $$ }
            """, LanguageVersion.CSharp9);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
    public Task TestInCatch()
        => VerifyKeywordAsync("""
            class C
            {
              async Task F()
              {
                try { }
            catch { $$ }  }
            }
            """, LanguageVersion.CSharp9);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
    public Task TestInCatch_TopLevel()
        => VerifyKeywordAsync(
            """
            try { }
            catch { $$ }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestNotInLock()
        => VerifyAbsenceAsync("""
            class C
            {
              async Task F()
              {
                lock(this) { $$ }  }
            }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestNotInLock_TopLevel()
        => VerifyAbsenceAsync("lock (this) { $$ }", LanguageVersion.CSharp9);

    [Fact]
    public Task TestInAsyncLambdaInCatch()
        => VerifyKeywordAsync("""
            class C
            {
              async Task F()
              {
                try { }
            catch { var z = async () => $$ }  }
            }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestInAsyncLambdaInCatch_TopLevel()
        => VerifyKeywordAsync(
            """
            try { }
            catch { var z = async () => $$ }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestAwaitInLock()
        => VerifyKeywordAsync("""
            class C
            {
              async Task F()
              {
                lock($$  }
            }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestAwaitInLock_TopLevel()
        => VerifyKeywordAsync("lock($$", LanguageVersion.CSharp9);

    [Fact]
    public Task TestDotAwaitSuggestAfterDotOnTask()
        => VerifyKeywordAsync("""
            using System.Threading.Tasks;

            class C
            {
              async Task F(Task someTask)
              {
                someTask.$$
              }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Fact]
    public Task TestDotAwaitSuggestAfterDotOnTaskOfT()
        => VerifyKeywordAsync("""
            using System.Threading.Tasks;

            class C
            {
              async Task F(Task<int> someTask)
              {
                someTask.$$
              }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Fact]
    public async Task TestDotAwaitSuggestAfterDotOnValueTask()
    {
        var valueTaskAssembly = typeof(ValueTask).Assembly.Location;
        var markup = $$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <MetadataReference>{{valueTaskAssembly}}</MetadataReference>
                    <Document FilePath="Test2.cs">
            using System.Threading.Tasks;

            class C
            {
              async Task F(ValueTask someTask)
              {
                someTask.$$
              }
            }
                    </Document>
                </Project>
            </Workspace>
            """;
        await VerifyItemExistsAsync(markup, "await");
        await VerifyItemExistsAsync(markup, "awaitf");
    }

    [Fact]
    public Task TestDotAwaitSuggestAfterDotOnCustomAwaitable()
        => VerifyKeywordAsync("""
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            public class DummyAwaiter: INotifyCompletion {
                public bool IsCompleted => true;
                public void OnCompleted(Action continuation) => continuation();
                public void GetResult() {}
            }

            public class CustomAwaitable
            {
                public DummyAwaiter GetAwaiter() => new DummyAwaiter();
            }

            static class Program
            {
                static async Task Main()
                {
                    var awaitable = new CustomAwaitable();
                    awaitable.$$;
                }
            }
            """, dotAwait: true);

    [Fact]
    public Task TestDotAwaitSuggestAfterDotOnCustomAwaitableButNotConfigureAwaitEvenIfPresent()
        => VerifyKeywordAsync("""
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            public class DummyAwaiter: INotifyCompletion {
                public bool IsCompleted => true;
                public void OnCompleted(Action continuation) => continuation();
                public void GetResult() {}
            }

            public class CustomAwaitable
            {
                public DummyAwaiter GetAwaiter() => new DummyAwaiter();
                public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) => default;
            }

            static class Program
            {
                static async Task Main()
                {
                    var awaitable = new CustomAwaitable();
                    awaitable.$$;
                }
            }
            """, dotAwait: true, dotAwaitf: false);

    [Fact]
    public Task TestDotAwaitSuggestAfterDotDot()
        => VerifyKeywordAsync("""
            using System.Threading.Tasks;

            static class Program
            {
                static async Task Main(Task someTask)
                {
                    someTask.$$.;
                }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Fact]
    public Task TestDotAwaitSuggestAfterDotBeforeType()
        => VerifyKeywordAsync("""
            using System;
            using System.Threading.Tasks;

            static class Program
            {
                static async Task Main(Task someTask)
                {
                    someTask.$$
                    Int32 i = 0;
                }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Fact]
    public Task TestDotAwaitSuggestAfterDotBeforeAnotherAwait()
        => VerifyKeywordAsync("""
            using System;
            using System.Threading.Tasks;

            static class Program
            {
                static async Task Main(Task someTask)
                {
                    someTask.$$
                    await Test();
                }

                async Task Test() { }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Theory]
    [InlineData("")]
    [InlineData("X.Y Test();")]
    [InlineData("var x;")]
    [InlineData("int x;")]
    [InlineData("System.Int32 x;")]
    [InlineData("if (true) { }")]
    [InlineData("System.Int32 Test() => 0;")]
    [InlineData("async Task<System.Int32> Test() => await Task.FromResult(1);")]
    public Task TestDotAwaitSuggestAfterDotBeforeDifferentStatements(string statement)
        => VerifyKeywordAsync($$"""
            using System;
            using System.Threading.Tasks;

            static class Program
            {
                static async Task Main(Task someTask)
                {
                    someTask.$$
                    {{statement}}
                }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Theory]
    // static
    [InlineData("StaticField.$$")]
    [InlineData("StaticProperty.$$")]
    [InlineData("StaticMethod().$$")]

    // parameters, locals and local function
    [InlineData("local.$$")]
    [InlineData("parameter.$$")]
    [InlineData("LocalFunction().$$")]

    // members
    [InlineData("c.Field.$$")]
    [InlineData("c.Property.$$")]
    [InlineData("c.Method().$$")]
    [InlineData("c.Self.Field.$$")]
    [InlineData("c.Self.Property.$$")]
    [InlineData("c.Self.Method().$$")]
    [InlineData("c.Function()().$$")]

    // indexer, operator, conversion
    [InlineData("c[0].$$")]
    [InlineData("(c + c).$$")]
    [InlineData("((Task)c).$$")]
    [InlineData("(c as Task).$$")]

    // parenthesized
    [InlineData("(parameter).$$")]
    [InlineData("((parameter)).$$")]
    [InlineData("(true ? parameter : parameter).$$")]
    [InlineData("(null ?? Task.CompletedTask).$$")]
    public Task TestDotAwaitSuggestAfterDifferentExpressions(string expression)
        => VerifyKeywordAsync($$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                public C Self => this;
                public Task Field = Task.CompletedTask;
                public Task Method() => Task.CompletedTask;
                public Task Property => Task.CompletedTask;
                public Task this[int i] => Task.CompletedTask;
                public Func<Task> Function() => () => Task.CompletedTask;
                public static Task operator +(C left, C right) => Task.CompletedTask;
                public static explicit operator Task(C c) => Task.CompletedTask;
            }

            static class Program
            {
                static Task StaticField = Task.CompletedTask;
                static Task StaticProperty => Task.CompletedTask;
                static Task StaticMethod() => Task.CompletedTask;

                static async Task Main(Task parameter)
                {
                    Task local = Task.CompletedTask;
                    var c = new C();

                    {{expression}}

                    Task LocalFunction() => Task.CompletedTask;
                }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Fact(Skip = "Fails because speculative binding can't figure out that local is a Task.")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/56245")]
    public Task TestDotAwaitSuggestBeforeLocalFunction()
        => VerifyKeywordAsync("""
            using System.Threading.Tasks;

            static class Program
            {
                static async Task Main()
                {
                    var local = Task.CompletedTask;
                    local.a$$

                    Task LocalFunction() => Task.CompletedTask;
                }
            }
            """);

    [Theory]
    [InlineData("await Task.Run(async () => Task.CompletedTask.$$")]
    [InlineData("await Task.Run(async () => someTask.$$")]
    [InlineData("await Task.Run(async () => someTask.$$);")]
    [InlineData("await Task.Run(async () => { someTask.$$ }")]
    [InlineData("await Task.Run(async () => { someTask.$$ });")]

    [InlineData("Task.Run(async () => await someTask).$$")]

    [InlineData("await Task.Run(() => someTask.$$")]
    public Task TestDotAwaitSuggestInLambdas(string lambda)
        => VerifyKeywordAsync($$"""
            using System.Threading.Tasks;

            static class Program
            {
                static async Task Main()
                {
                    var someTask = Task.CompletedTask;
                    {{lambda}}
                }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Fact]
    public Task TestDotAwaitNotAfterDotOnTaskIfAlreadyAwaited()
        => VerifyAbsenceAsync("""
            using System.Threading.Tasks;

            class C
            {
              async Task F(Task someTask)
              {
                await someTask.$$
              }
            }
            """);

    [Fact]
    public Task TestDotAwaitNotAfterTaskType()
        => VerifyAbsenceAsync("""
            using System.Threading.Tasks;

            class C
            {
              async Task F()
              {
                Task.$$
              }
            }
            """);

    [Fact]
    public Task TestDotAwaitNotInLock()
        => VerifyAbsenceAsync("""
            using System.Threading.Tasks;

            class C
            {
              async Task F(Task someTask)
              {
                lock(this) { someTask.$$ }
              }
            }
            """);

    [Fact]
    public Task TestDotAwaitNotInLock_TopLevel()
        => VerifyAbsenceAsync("""
            using System.Threading.Tasks;

            lock(this) { Task.CompletedTask.$$ }
            """);

    [Fact]
    public Task TestDotAwaitQueryNotInSelect()
        => VerifyAbsenceAsync("""
            using System.Linq;
            using System.Threading.Tasks;

            class C
            {
              async Task F()
              {
                var z = from t in new[] { Task.CompletedTask }
                        select t.$$
              }
            }
            """);

    [Fact]
    public Task TestDotAwaitQueryInFirstFromClause()
        => VerifyKeywordAsync("""
            using System.Linq;
            using System.Threading.Tasks;

            class C
            {
                async Task F()
                {
                    var arrayTask2 = Task.FromResult(new int[0]);
                    var z = from t in arrayTask2.$$
                            select t;
                }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Fact]
    public Task TestDotAwaitQueryNotInSecondFromClause()
        => VerifyNoItemsExistAsync("""
            using System.Linq;
            using System.Threading.Tasks;

            class C
            {
                async Task F()
                {
                    var array1 = new int[0];
                    var arrayTask2 = Task.FromResult(new int[0]);
                    var z = from i1 in array1
                            from i2 in arrayTask2.$$
                            select i2;
                }
            }
            """);

    [Fact]
    public Task TestDotAwaitQueryNotInContinuation()
        => VerifyNoItemsExistAsync("""
            using System.Linq;
            using System.Threading.Tasks;

            class C
            {
                async Task F()
                {
                    var array1 = new int[0];
                    var arrayTask2 = Task.FromResult(new int[0]);
                    var z = from i1 in array1
                            select i1 into c
                            from i2 in arrayTask2.$$
                            select i2;
                }
            }
            """);

    [Fact]
    public Task TestDotAwaitQueryInJoinClause()
        => VerifyKeywordAsync("""
            using System.Linq;
            using System.Threading.Tasks;

            class C
            {
                async Task F()
                {
                    var array1 = new int[0];
                    var arrayTask2 = Task.FromResult(new int[0]);
                    var z = from i1 in array1
                            join i2 in arrayTask2.$$ on i1 equals i2
                            select i1;
                }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Fact]
    public Task TestDotAwaitQueryInJoinIntoClause()
        => VerifyKeywordAsync("""
            using System.Linq;
            using System.Threading.Tasks;

            class C
            {
                async Task F()
                {
                    var array1 = new int[0];
                    var arrayTask2 = Task.FromResult(new int[0]);
                    var z = from i1 in array1
                            join i2 in arrayTask2.$$ on i1 equals i2 into g
                            select g;
                }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Fact]
    public Task TestDotAwaitNotAfterConditionalAccessOfTaskMembers()
        => VerifyAbsenceAsync("""
            using System.Threading.Tasks;

            class C
            {
              async Task F(Task someTask)
              {
                someTask?.$$
              }
            }
            """);

    [Theory]
    [InlineData("c?.SomeTask.$$")]

    [InlineData("c.M()?.SomeTask.$$")]
    [InlineData("c.Pro?.SomeTask.$$")]

    [InlineData("c?.M().SomeTask.$$")]
    [InlineData("c?.Pro.SomeTask.$$")]

    [InlineData("c?.M()?.SomeTask.$$")]
    [InlineData("c?.Pro?.SomeTask.$$")]

    [InlineData("c.M()?.Pro.SomeTask.$$")]
    [InlineData("c.Pro?.M().SomeTask.$$")]

    [InlineData("c.M()?.M().M()?.M().SomeTask.$$")]
    [InlineData("new C().M()?.Pro.M()?.M().SomeTask.$$")]
    public Task TestDotAwaitNotAfterDotInConditionalAccessChain(string conditionalAccess)
        => VerifyAbsenceAsync($$"""
            using System.Threading.Tasks;
            public class C
            {
                public Task SomeTask => Task.CompletedTask;

                public C Pro => this;
                public C M() => this;
            }

            static class Program
            {
                public static async Task Main()
                {
                    var c = new C();
                    {{conditionalAccess}}
                }
            }
            """);

    [Theory]
    [InlineData("c!.SomeTask.$$")]
    [InlineData("c.SomeTask!.$$")]

    [InlineData("c.M()!.SomeTask.$$")]
    [InlineData("c.Pro!.SomeTask.$$")]

    [InlineData("c!.M().SomeTask.$$")]
    [InlineData("c!.Pro.SomeTask.$$")]

    [InlineData("c!.M()!.SomeTask.$$")]
    [InlineData("c!.Pro!.SomeTask.$$")]

    [InlineData("c.M()!.Pro.SomeTask.$$")]
    [InlineData("c.Pro!.M().SomeTask.$$")]

    [InlineData("c.M()!.M().M()!.M().SomeTask.$$")]
    [InlineData("new C().M()!.Pro.M()!.M().SomeTask.$$")]
    public Task TestDotAwaitAfterNullForgivingOperatorAccessChain(string nullForgivingAccess)
        => VerifyKeywordAsync($$"""
            #nullable enable

            using System.Threading.Tasks;
            public class C
            {
                public Task? SomeTask => Task.CompletedTask;

                public C? Pro => this;
                public C? M() => this;
            }

            static class Program
            {
                public static async Task Main(params string[] args)
                {
                    var c =  args[1] == string.Empty ? new C() : null;
                    {{nullForgivingAccess}}
                }
            }
            """, dotAwait: true, dotAwaitf: true);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/58921")]
    public async Task TestInCastExpressionThatMightBeParenthesizedExpression(bool hasNewline)
    {
        var code = $$"""
            class C
            {
                void M()
                {
                    var data = (n$$) {{(hasNewline ? Environment.NewLine : string.Empty)}} M();
                }
            }
            """;
        if (hasNewline)
            await VerifyKeywordAsync(code);
        else
            await VerifyAbsenceAsync(code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77848")]
    public Task TestEventHandlerMethod_DoesNotChangeVoidToTask()
        => VerifyKeywordAsync("""
            using System;

            class C
            {
                public event EventHandler MyEvent;

                public C()
                {
                    MyEvent += OnMyEvent;
                }

                private void OnMyEvent(object sender, EventArgs e)
                {
                    $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77848")]
    public Task TestEventHandlerMethod_WithDifferentEventType()
        => VerifyKeywordAsync("""
            using System;

            delegate void CustomEventHandler(object sender, EventArgs e);

            class C
            {
                public event CustomEventHandler MyEvent;

                public C()
                {
                    MyEvent += HandleMyEvent;
                }

                private void HandleMyEvent(object sender, EventArgs e)
                {
                    $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77848")]
    public Task TestEventHandlerMethod_InDifferentMethod()
        => VerifyKeywordAsync("""
            using System;

            class C
            {
                public event EventHandler MyEvent;

                public void RegisterHandler()
                {
                    MyEvent += OnMyEvent;
                }

                private void OnMyEvent(object sender, EventArgs e)
                {
                    $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77848")]
    public Task TestEventHandlerMethod_WithMinusEquals()
        => VerifyKeywordAsync("""
            using System;

            class C
            {
                public event EventHandler MyEvent;

                public void UnregisterHandler()
                {
                    MyEvent -= OnMyEvent;
                }

                private void OnMyEvent(object sender, EventArgs e)
                {
                    $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77848")]
    public Task TestNonEventHandlerMethod_ChangesVoidToTask()
        => VerifyKeywordAsync("""
            using System;

            class C
            {
                private void RegularMethod()
                {
                    $$
                }
            }
            """);
}
