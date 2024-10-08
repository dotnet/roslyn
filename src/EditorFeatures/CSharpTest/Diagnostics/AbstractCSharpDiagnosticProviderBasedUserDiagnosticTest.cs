// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;

public abstract partial class AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest : AbstractDiagnosticProviderBasedUserDiagnosticTest
{
    protected AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest(ITestOutputHelper logger)
       : base(logger)
    {
    }

    protected override ParseOptions GetScriptOptions() => Options.Script;

    protected internal override string GetLanguage() => LanguageNames.CSharp;

    protected const string IAsyncEnumerable = """
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

    internal OptionsCollection RequireArithmeticBinaryParenthesesForClarity => ParenthesesOptionsProvider.RequireArithmeticBinaryParenthesesForClarity;
    internal OptionsCollection RequireRelationalBinaryParenthesesForClarity => ParenthesesOptionsProvider.RequireRelationalBinaryParenthesesForClarity;
    internal OptionsCollection RequireOtherBinaryParenthesesForClarity => ParenthesesOptionsProvider.RequireOtherBinaryParenthesesForClarity;
    internal OptionsCollection IgnoreAllParentheses => ParenthesesOptionsProvider.IgnoreAllParentheses;
    internal OptionsCollection RemoveAllUnnecessaryParentheses => ParenthesesOptionsProvider.RemoveAllUnnecessaryParentheses;
    internal OptionsCollection RequireAllParenthesesForClarity => ParenthesesOptionsProvider.RequireAllParenthesesForClarity;
}
