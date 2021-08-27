// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    /// <summary>
    /// The <see cref="AwaitCompletionProvider"/> adds async modifier if the return type is Task or ValueTask.
    /// The tests here are only checking whether the completion item is provided or not.
    /// Tests for checking adding async modifier are in:
    /// src/EditorFeatures/Test2/IntelliSense/CSharpCompletionCommandHandlerTests_AwaitCompletion.vb
    /// </summary>
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class AwaitCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType() => typeof(AwaitCompletionProvider);

        private const string CompletionDisplayTextAwait = "await";
        private const string CompletionDisplayTextAwaitAndConfigureAwait = "awaitf";

        private async Task VerifyAbsenceAsync(string code)
        {
            await VerifyItemIsAbsentAsync(code, CompletionDisplayTextAwait);
            await VerifyItemIsAbsentAsync(code, CompletionDisplayTextAwaitAndConfigureAwait);
        }

        private async Task VerifyAbsenceAsync(string code, LanguageVersion languageVersion)
        {
            await VerifyItemIsAbsentAsync(GetMarkup(code, languageVersion), CompletionDisplayTextAwait);
            await VerifyItemIsAbsentAsync(GetMarkup(code, languageVersion), CompletionDisplayTextAwaitAndConfigureAwait);
        }

        private async Task VerifyKeywordAsync(string code, LanguageVersion languageVersion, string? inlineDescription = null, bool includeingAwaitf = false)
        {
            await VerifyItemExistsAsync(GetMarkup(code, languageVersion), CompletionDisplayTextAwait, glyph: (int)Glyph.Keyword, inlineDescription: inlineDescription);
            if (includeingAwaitf)
            {
                await VerifyItemExistsAsync(GetMarkup(code, languageVersion), CompletionDisplayTextAwaitAndConfigureAwait, glyph: (int)Glyph.Keyword, inlineDescription: inlineDescription);
            }
            else
            {
                await VerifyItemIsAbsentAsync(GetMarkup(code, languageVersion), CompletionDisplayTextAwaitAndConfigureAwait);
            }
        }

        [Fact]
        public async Task TestNotInTypeContext()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    $$
}");
        }

        [Fact]
        public async Task TestStatementInMethod()
        {
            await VerifyKeywordAsync(@"
class C
{
  void F()
  {
    $$  }
}", LanguageVersion.CSharp9, FeaturesResources.Make_containing_scope_async);
        }

        [Fact]
        public async Task TestStatementInMethod_Async()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    $$  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestStatementInMethod_TopLevel()
        {
            await VerifyKeywordAsync("$$", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestExpressionInAsyncMethod()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    var z = $$  }
}
", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestExpressionInNonAsyncMethodWithTaskReturn()
        {
            await VerifyKeywordAsync(@"
class C
{
  Task F()
  {
    var z = $$  }
}
", LanguageVersion.CSharp9, FeaturesResources.Make_containing_scope_async);
        }

        [Fact]
        public async Task TestExpressionInAsyncMethod_TopLevel()
        {
            await VerifyKeywordAsync("var z = $$", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestUsingStatement()
        {
            await VerifyAbsenceAsync(@"
class C
{
  async Task F()
  {
    using $$  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestUsingStatement_TopLevel()
        {
            await VerifyAbsenceAsync("using $$", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestUsingDirective()
            => await VerifyAbsenceAsync("using $$");

        [Fact]
        public async Task TestGlobalUsingDirective()
            => await VerifyAbsenceAsync("global using $$");

        [Fact]
        public async Task TestForeachStatement()
        {
            await VerifyAbsenceAsync(@"
class C
{
  async Task F()
  {
    foreach $$  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestForeachStatement_TopLevel()
        {
            await VerifyAbsenceAsync("foreach $$", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestNotInQuery()
        {
            await VerifyAbsenceAsync(@"
class C
{
  async Task F()
  {
    var z = from a in ""char""
          select $$  }
    }
", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestNotInQuery_TopLevel()
        {
            await VerifyAbsenceAsync(
@"var z = from a in ""char""
          select $$", LanguageVersion.CSharp9);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Fact]
        public async Task TestInFinally()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    try { }
finally { $$ }  }
}", LanguageVersion.CSharp9);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Fact]
        public async Task TestInFinally_TopLevel()
        {
            await VerifyKeywordAsync(
@"try { }
finally { $$ }", LanguageVersion.CSharp9);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Fact]
        public async Task TestInCatch()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    try { }
catch { $$ }  }
}", LanguageVersion.CSharp9);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Fact]
        public async Task TestInCatch_TopLevel()
        {
            await VerifyKeywordAsync(
@"try { }
catch { $$ }", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestNotInLock()
        {
            await VerifyAbsenceAsync(@"
class C
{
  async Task F()
  {
    lock(this) { $$ }  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestNotInLock_TopLevel()
        {
            await VerifyAbsenceAsync("lock (this) { $$ }", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestInAsyncLambdaInCatch()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    try { }
catch { var z = async () => $$ }  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestInAsyncLambdaInCatch_TopLevel()
        {
            await VerifyKeywordAsync(
@"try { }
catch { var z = async () => $$ }", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestAwaitInLock()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    lock($$  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestAwaitInLock_TopLevel()
        {
            await VerifyKeywordAsync("lock($$", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestDotAwaitSuggestAfterDotOnTask()
        {
            await VerifyKeywordAsync(@"
using System.Threading.Tasks;

class C
{
  async Task F(Task someTask)
  {
    someTask.$$
  }
}
", LanguageVersion.CSharp9, includeingAwaitf: true);
        }

        [Fact]
        public async Task TestDotAwaitSuggestAfterDotOnTaskOfT()
        {
            await VerifyKeywordAsync(@"
using System.Threading.Tasks;

class C
{
  async Task F(Task<int> someTask)
  {
    someTask.$$
  }
}
", LanguageVersion.CSharp9, includeingAwaitf: true);
        }

        [Fact]
        public async Task TestDotAwaitSuggestAfterDotOnValueTask()
        {
            var valueTaskAssembly = typeof(ValueTask).Assembly.Location;
            await VerifyItemExistsAsync(@$"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <MetadataReference>{valueTaskAssembly}</MetadataReference>
        <Document FilePath=""Test2.cs"">
using System.Threading.Tasks;

class C
{{
  async Task F(ValueTask someTask)
  {{
    someTask.$$
  }}
}}
        </Document>
    </Project>
</Workspace>
", "await");
        }

        [Fact]
        public async Task TestDotAwaitSuggestAfterDotOnCustomAwaitable()
        {
            await VerifyKeywordAsync(@"
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
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestDotAwaitSuggestAfterDotDot()
        {
            await VerifyKeywordAsync(@"
using System.Threading.Tasks;

static class Program
{
    static async Task Main(Task someTask)
    {
        someTask.$$.;
    }
}", LanguageVersion.CSharp9, includeingAwaitf: true);
        }

        [Fact]
        public async Task TestDotAwaitSuggestAfterDotBeforeType()
        {
            await VerifyKeywordAsync(@"
using System;
using System.Threading.Tasks;

static class Program
{
    static async Task Main(Task someTask)
    {
        someTask.$$
        Int32 i = 0;
    }
}", LanguageVersion.CSharp9, includeingAwaitf: true);
        }

        [Fact]
        public async Task TestDotAwaitSuggestAfterDotBeforeAnotherAwait()
        {
            await VerifyKeywordAsync(@"
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
}", LanguageVersion.CSharp9, includeingAwaitf: true);
        }

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
        public async Task TestDotAwaitSuggestAfterDifferentExpressions(string expression)
        {
            await VerifyKeywordAsync($@"
using System;
using System.Threading.Tasks;

class C
{{
    public C Self => this;
    public Task Field = Task.CompletedTask;
    public Task Method() => Task.CompletedTask;
    public Task Property => Task.CompletedTask;
    public Task this[int i] => Task.CompletedTask;
    public Func<Task> Function() => () => Task.CompletedTask;
    public static Task operator +(C left, C right) => Task.CompletedTask;
    public static explicit operator Task(C c) => Task.CompletedTask;
}}

static class Program
{{
    static Task StaticField = Task.CompletedTask;
    static Task StaticProperty => Task.CompletedTask;
    static Task StaticMethod() => Task.CompletedTask;

    static async Task Main(Task parameter)
    {{
        Task local = Task.CompletedTask;
        var c = new C();

        {expression}

        Task LocalFunction() => Task.CompletedTask;
    }}
}}", LanguageVersion.CSharp9, includeingAwaitf: true);
        }

        [Fact(Skip = "Fails because speculative binding can't figure out that local is a Task.")]
        public async Task TestDotAwaitSuggestBeforeLocalFunction()
        {
            // Speculative binding a local as expression finds the local as ILocalSymbol, but the type is ErrorType.
            // This is only the case when
            // * await is partially written (local.a),
            // * only for locals (e.g. IParameterSymbols are fine) which
            //   * are declared with var
            //   * The return type of the local function is used as first name in a MemberAccess in the declarator
            await VerifyKeywordAsync(@"
using System.Threading.Tasks;

static class Program
{
    static async Task Main()
    {
        var local = Task.CompletedTask;
        local.a$$

        Task LocalFunction() => Task.CompletedTask;
    }
}", LanguageVersion.CSharp9);
        }

        [Theory]
        [InlineData("await Task.Run(async () => Task.CompletedTask.$$", false)]
        [InlineData("await Task.Run(async () => someTask.$$", false)]
        [InlineData("await Task.Run(async () => someTask.$$);", false)]
        [InlineData("await Task.Run(async () => { someTask.$$ }", false)]
        [InlineData("await Task.Run(async () => { someTask.$$ });", false)]

        [InlineData("Task.Run(async () => await someTask).$$", false)]

        [InlineData("await Task.Run(() => someTask.$$", true)]
        public async Task TestDotAwaitSuggestInLambdas(string lambda, bool makeContainerAsync)
        {
            await VerifyKeywordAsync($@"
using System.Threading.Tasks;

static class Program
{{
    static async Task Main()
    {{
        var someTask = Task.CompletedTask;
        {lambda}
    }}
}}", LanguageVersion.CSharp9, inlineDescription: makeContainerAsync ? FeaturesResources.Make_containing_scope_async : null, includeingAwaitf: true);
        }

        [Fact]
        public async Task TestDotAwaitNotAfterDotOnTaskIfAlreadyAwaited()
        {
            await VerifyAbsenceAsync(@"
using System.Threading.Tasks;

class C
{
  async Task F(Task someTask)
  {
    await someTask.$$
  }
}
", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestDotAwaitNotAfterTaskType()
        {
            await VerifyAbsenceAsync(@"
using System.Threading.Tasks;

class C
{
  async Task F()
  {
    Task.$$
  }
}
", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestDotAwaitNotInLock()
        {
            await VerifyAbsenceAsync(@"
using System.Threading.Tasks;

class C
{
  async Task F(Task someTask)
  {
    lock(this) { someTask.$$ }
  }
}
", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestDotAwaitNotInLock_TopLevel()
        {
            await VerifyAbsenceAsync(@"
using System.Threading.Tasks;

lock(this) { Task.CompletedTask.$$ }
", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestDotAwaitNotInQuery()
        {
            await VerifyAbsenceAsync(@"
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
", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestDotAwaitNotAfterConditionalAccessOfTaskMembers()
        {
            // The conditional access suggests, that someTask can be null.
            // await on null throws at runtime, so the user should do
            // if (someTask is not null) await someTask;
            // or
            // await (someTask ?? Task.CompletedTask)
            // Completion should not offer await, because the patterns above would change to much code.
            // This decision should be revised after https://github.com/dotnet/csharplang/issues/35 
            // is implemented.
            await VerifyAbsenceAsync(@"
using System.Threading.Tasks;

class C
{
  async Task F(Task someTask)
  {
    someTask?.$$
  }
}
", LanguageVersion.CSharp9);
        }

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
        public async Task TestDotAwaitNotAfterDotInConditionalAccessChain(string conditionalAccess)
        {
            await VerifyAbsenceAsync($@"
using System.Threading.Tasks;
public class C
{{
    public Task SomeTask => Task.CompletedTask;
    
    public C Pro => this;
    public C M() => this;
}}

static class Program
{{
    public static async Task Main()
    {{
        var c = new C();
        {conditionalAccess}
    }}
}}
", LanguageVersion.CSharp9);
        }
    }
}
