// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders.Snippets
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class CSharpForEachSnippetCompletionProviderTests : AbstractCSharpSnippetCompletionProviderTests
    {
        protected override string ItemToCommit => "foreach";

        private const string IAsyncEnumerable = """
            namespace System
            {
                public interface IAsyncDisposable
                {
                    System.Threading.Tasks.ValueTask DisposeAsync();
                }
            }

            namespace System.Runtime.CompilerServices
            {
                using System.Threading.Tasks;

                public sealed class AsyncMethodBuilderAttribute : Attribute
                {
                    public AsyncMethodBuilderAttribute(Type builderType) { }
                    public Type BuilderType { get; }
                }

                public struct AsyncValueTaskMethodBuilder
                {
                    public ValueTask Task => default;

                    public static AsyncValueTaskMethodBuilder Create() => default;
                    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                        where TAwaiter : INotifyCompletion
                        where TStateMachine : IAsyncStateMachine {}

                    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                        where TAwaiter : ICriticalNotifyCompletion
                        where TStateMachine : IAsyncStateMachine {}
                    public void SetException(Exception exception) {}
                    public void SetResult() {}
                    public void SetStateMachine(IAsyncStateMachine stateMachine) {}
                    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine {}
                }

                public readonly struct ValueTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
                {
                    public bool IsCompleted => default;

                    public void GetResult() { }
                    public void OnCompleted(Action continuation) { }
                    public void UnsafeOnCompleted(Action continuation) { }
                }

                public readonly struct ValueTaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
                {
                    public bool IsCompleted => default;
                    public TResult GetResult() => default;
                    public void OnCompleted(Action continuation) { }
                    public void UnsafeOnCompleted(Action continuation) { }
                }
            }

            namespace System.Threading.Tasks
            {
                using System.Runtime.CompilerServices;

                [AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder))]
                public readonly struct ValueTask : IEquatable<ValueTask>
                {
                    public ValueTask(Task task) {}
                    public ValueTask(IValueTaskSource source, short token) {}

                    public bool IsCompleted => default;
                    public bool IsCompletedSuccessfully => default;
                    public bool IsFaulted => default;
                    public bool IsCanceled => default;

                    public Task AsTask() => default;
                    public ConfiguredValueTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) => default;
                    public override bool Equals(object obj) => default;
                    public bool Equals(ValueTask other) => default;
                    public ValueTaskAwaiter GetAwaiter() => default;
                    public override int GetHashCode() => default;
                    public ValueTask Preserve() => default;

                    public static bool operator ==(ValueTask left, ValueTask right) => default;
                    public static bool operator !=(ValueTask left, ValueTask right) => default;
                }

                [AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder<>))]
                public readonly struct ValueTask<TResult> : IEquatable<ValueTask<TResult>>
                {
                    public ValueTask(TResult result) {}
                    public ValueTask(Task<TResult> task) {}
                    public ValueTask(IValueTaskSource<TResult> source, short token) {}

                    public bool IsFaulted => default;
                    public bool IsCompletedSuccessfully => default;
                    public bool IsCompleted => default;
                    public bool IsCanceled => default;
                    public TResult Result => default;

                    public Task<TResult> AsTask() => default;
                    public ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) => default;

                    public bool Equals(ValueTask<TResult> other) => default;
                    public override bool Equals(object obj) => default;
                    public ValueTaskAwaiter<TResult> GetAwaiter() => default;
                    public override int GetHashCode() => default;
                    public ValueTask<TResult> Preserve() => default;
                    public override string ToString() => default;
                    public static bool operator ==(ValueTask<TResult> left, ValueTask<TResult> right) => default;
                    public static bool operator !=(ValueTask<TResult> left, ValueTask<TResult> right) => default;
                }
            }

            namespace System.Collections.Generic
            {
                public interface IAsyncEnumerable<out T>
                {
                    IAsyncEnumerator<T> GetAsyncEnumerator();
                }

                public interface IAsyncEnumerator<out T> : IAsyncDisposable
                {
                    System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
                    T Current { get; }
                }
            }
            """;

        [WpfFact]
        public async Task InsertForEachSnippetInMethodTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        Ins$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        foreach (var item in collection)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInMethodItemUsedTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var item = 5;
        Ins$$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        var item = 5;
        foreach (var item1 in collection)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInGlobalContextTest()
        {
            var markupBeforeCommit =
@"Ins$$
";

            var expectedCodeAfterCommit =
@"foreach (var item in collection)
{
    $$
}
";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInConstructorTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public Program()
    {
        $$
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public Program()
    {
        foreach (var item in collection)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetWithCollectionTest()
        {
            var markupBeforeCommit =
@"using System;
using System.Collections.Generic;

class Program
{
    public Program()
    {
        var list = new List<int> { 1, 2, 3 };
        $$
    }
}";

            var expectedCodeAfterCommit =
@"using System;
using System.Collections.Generic;

class Program
{
    public Program()
    {
        var list = new List<int> { 1, 2, 3 };
        foreach (var item in list)
        {
            $$
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInLocalFunctionTest()
        {
            var markupBeforeCommit =
@"class Program
{
    public void Method()
    {
        var x = 5;
        void LocalMethod()
        {
            $$
        }
    }
}";

            var expectedCodeAfterCommit =
@"class Program
{
    public void Method()
    {
        var x = 5;
        void LocalMethod()
        {
            foreach (var item in collection)
            {
                $$
            }
        }
    }
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInAnonymousFunctionTest()
        {
            var markupBeforeCommit =
@"public delegate void Print(int value);
static void Main(string[] args)
{
    Print print = delegate(int val) {
        $$
    };
}";

            var expectedCodeAfterCommit =
@"public delegate void Print(int value);
static void Main(string[] args)
{
    Print print = delegate(int val) {
        foreach (var item in args)
        {
            $$
        }
    };
}";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInParenthesizedLambdaExpressionRegularTest()
        {
            var markupBeforeCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    $$
    return x == y;
};";

            var expectedCodeAfterCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    foreach (var item in args)
    {
        $$
    }
    return x == y;
};";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WpfFact]
        public async Task InsertForEachSnippetInParenthesizedLambdaExpressionScriptTest()
        {
            var markupBeforeCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    $$
    return x == y;
};";

            var expectedCodeAfterCommit =
@"Func<int, int, bool> testForEquality = (x, y) =>
{
    foreach (var item in collection)
    {
        $$
    }
    return x == y;
};";
            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit, sourceCodeKind: SourceCodeKind.Script);
        }

        [WpfFact]
        public async Task InsertInlineForEachSnippetForCorrectTypeTest()
        {
            var markupBeforeCommit = """
                using System.Collections.Generic;

                class C
                {
                    void M(List<int> list)
                    {
                        list.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = """
                using System.Collections.Generic;
                
                class C
                {
                    void M(List<int> list)
                    {
                        foreach (var item in list)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoInlineForEachSnippetForIncorrectTypeTest()
        {
            var markupBeforeCommit = """
                class Program
                {
                    void M(int arg)
                    {
                        arg.$$
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfFact]
        public async Task NoInlineForEachSnippetWhenNotDirectlyExpressionStatementTest()
        {
            var markupBeforeCommit = """
                using System;
                using System.Collections.Generic;

                class Program
                {
                    void M(List<int> list)
                    {
                        Console.WriteLine(list.$$);
                    }
                }
                """;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("// comment")]
        [InlineData("/* comment */")]
        [InlineData("#region test")]
        public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInMethodTest1(string trivia)
        {
            var markupBeforeCommit = $$"""
                class Program
                {
                    void M(int[] arr)
                    {
                        {{trivia}}
                        arr.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    void M(int[] arr)
                    {
                        {{trivia}}
                        foreach (var item in arr)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("#if true")]
        [InlineData("#pragma warning disable CS0108")]
        [InlineData("#nullable enable")]
        public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInMethodTest2(string trivia)
        {
            var markupBeforeCommit = $$"""
                class Program
                {
                    void M(int[] arr)
                    {
                {{trivia}}
                        arr.$$
                    }
                }
                """;

            var expectedCodeAfterCommit = $$"""
                class Program
                {
                    void M(int[] arr)
                    {
                {{trivia}}
                        foreach (var item in arr)
                        {
                            $$
                        }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("// comment")]
        [InlineData("/* comment */")]
        public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest1(string trivia)
        {
            var markupBeforeCommit = $$"""
                {{trivia}}
                (new int[10]).$$
                """;

            var expectedCodeAfterCommit = $$"""
                {{trivia}}
                foreach (var item in new int[10])
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("#region test")]
        [InlineData("#if true")]
        [InlineData("#pragma warning disable CS0108")]
        [InlineData("#nullable enable")]
        public async Task CorrectlyDealWithLeadingTriviaInInlineSnippetInGlobalStatementTest2(string trivia)
        {
            var markupBeforeCommit = $$"""
                {{trivia}}
                (new int[10]).$$
                """;

            var expectedCodeAfterCommit = $$"""

                {{trivia}}
                foreach (var item in new int[10])
                {
                    $$
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("")]
        [InlineData("async ")]
        public async Task InsertForEachSnippetAfterSingleAwaitKeywordInMethodBodyTest(string asyncKeyword)
        {
            var markupBeforeCommit = $$"""
                class C
                {
                    {{asyncKeyword}}void M()
                    {
                        await $$
                    }
                }
                """ + IAsyncEnumerable;

            var expectedCodeAfterCommit = $$"""
                class C
                {
                    {{asyncKeyword}}void M()
                    {
                        await foreach (var item in collection)
                        {
                            $$
                        }
                    }
                }
                """ + IAsyncEnumerable;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task InsertForEachSnippetAfterSingleAwaitKeywordInGlobalStatementTest()
        {
            var markupBeforeCommit = """
                await $$

                """ + IAsyncEnumerable;

            var expectedCodeAfterCommit = """
                await foreach (var item in collection)
                {
                    $$
                }

                """ + IAsyncEnumerable;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfFact]
        public async Task NoForEachStatementAfterAwaitKeywordWhenWontResultInStatementTest()
        {
            var markupBeforeCommit = """
                var result = await $$
                """ + IAsyncEnumerable;

            await VerifyItemIsAbsentAsync(markupBeforeCommit, ItemToCommit);
        }

        [WpfTheory]
        [InlineData("")]
        [InlineData("async ")]
        public async Task PreferAsyncEnumerableVariableInScopeForAwaitForEachTest(string asyncKeyword)
        {
            var markupBeforeCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    {{asyncKeyword}}void M()
                    {
                        IEnumerable<int> enumerable;
                        IAsyncEnumerable<int> asyncEnumerable;

                        await $$
                    }
                }
                """ + IAsyncEnumerable;

            var expectedCodeAfterCommit = $$"""
                using System.Collections.Generic;
                
                class C
                {
                    {{asyncKeyword}}void M()
                    {
                        IEnumerable<int> enumerable;
                        IAsyncEnumerable<int> asyncEnumerable;
                
                        await foreach (var item in asyncEnumerable)
                        {
                            $$
                        }
                    }
                }
                """ + IAsyncEnumerable;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }

        [WpfTheory]
        [InlineData("")]
        [InlineData("async ")]
        public async Task InsertAwaitForEachSnippetForPostfixAsyncEnumerableTest(string asyncKeyword)
        {
            var markupBeforeCommit = $$"""
                using System.Collections.Generic;

                class C
                {
                    {{asyncKeyword}}void M(IAsyncEnumerable<int> asyncEnumerable)
                    {
                        asyncEnumerable.$$
                    }
                }
                """ + IAsyncEnumerable;

            var expectedCodeAfterCommit = $$"""
                using System.Collections.Generic;
                
                class C
                {
                    {{asyncKeyword}}void M(IAsyncEnumerable<int> asyncEnumerable)
                    {
                        await foreach (var item in asyncEnumerable)
                        {
                            $$
                        }
                    }
                }
                """ + IAsyncEnumerable;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, ItemToCommit, expectedCodeAfterCommit);
        }
    }
}
