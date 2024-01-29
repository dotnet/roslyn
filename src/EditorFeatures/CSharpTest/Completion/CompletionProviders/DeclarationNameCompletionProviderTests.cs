// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources
{
    using static SymbolSpecification;

    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class DeclarationNameCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        private const string Span = """
            namespace System
            {
                public readonly ref struct Span<T>
                {
                    private readonly T[] arr;
                    public ref T this[int i] => ref arr[i];
                    public override int GetHashCode() => 1;
                    public int Length { get; }
                    unsafe public Span(void* pointer, int length)
                    {
                        this.arr = Helpers.ToArray<T>(pointer, length);
                        this.Length = length;
                    }
                    public Span(T[] arr)
                    {
                        this.arr = arr;
                        this.Length = arr.Length;
                    }
                    public void CopyTo(Span<T> other) { }
                    /// <summary>Gets an enumerator for this span.</summary>
                    public Enumerator GetEnumerator() => new Enumerator(this);
                    /// <summary>Enumerates the elements of a <see cref="Span{T}"/>.</summary>
                    public ref struct Enumerator
                    {
                        /// <summary>The span being enumerated.</summary>
                        private readonly Span<T> _span;
                        /// <summary>The next index to yield.</summary>
                        private int _index;
                        /// <summary>Initialize the enumerator.</summary>
                        /// <param name="span">The span to enumerate.</param>
                        internal Enumerator(Span<T> span)
                        {
                            _span = span;
                            _index = -1;
                        }
                        /// <summary>Advances the enumerator to the next element of the span.</summary>
                        public bool MoveNext()
                        {
                            int index = _index + 1;
                            if (index < _span.Length)
                            {
                                _index = index;
                                return true;
                            }
                            return false;
                        }
                        /// <summary>Gets the element at the current position of the enumerator.</summary>
                        public ref T Current
                        {
                            get => ref _span[_index];
                        }
                    }
                    public static implicit operator Span<T>(T[] array) => new Span<T>(array);
                    public Span<T> Slice(int offset, int length)
                    {
                        var copy = new T[length];
                        Array.Copy(arr, offset, copy, 0, length);
                        return new Span<T>(copy);
                    }
                }
                public readonly ref struct ReadOnlySpan<T>
                {
                    private readonly T[] arr;
                    public ref readonly T this[int i] => ref arr[i];
                    public override int GetHashCode() => 2;
                    public int Length { get; }
                    unsafe public ReadOnlySpan(void* pointer, int length)
                    {
                        this.arr = Helpers.ToArray<T>(pointer, length);
                        this.Length = length;
                    }
                    public ReadOnlySpan(T[] arr)
                    {
                        this.arr = arr;
                        this.Length = arr.Length;
                    }
                    public void CopyTo(Span<T> other) { }
                    /// <summary>Gets an enumerator for this span.</summary>
                    public Enumerator GetEnumerator() => new Enumerator(this);
                    /// <summary>Enumerates the elements of a <see cref="Span{T}"/>.</summary>
                    public ref struct Enumerator
                    {
                        /// <summary>The span being enumerated.</summary>
                        private readonly ReadOnlySpan<T> _span;
                        /// <summary>The next index to yield.</summary>
                        private int _index;
                        /// <summary>Initialize the enumerator.</summary>
                        /// <param name="span">The span to enumerate.</param>
                        internal Enumerator(ReadOnlySpan<T> span)
                        {
                            _span = span;
                            _index = -1;
                        }
                        /// <summary>Advances the enumerator to the next element of the span.</summary>
                        public bool MoveNext()
                        {
                            int index = _index + 1;
                            if (index < _span.Length)
                            {
                                _index = index;
                                return true;
                            }
                            return false;
                        }
                        /// <summary>Gets the element at the current position of the enumerator.</summary>
                        public ref readonly T Current
                        {
                            get => ref _span[_index];
                        }
                    }
                    public static implicit operator ReadOnlySpan<T>(T[] array) => array == null ? default : new ReadOnlySpan<T>(array);
                    public static implicit operator ReadOnlySpan<T>(string stringValue) => string.IsNullOrEmpty(stringValue) ? default : new ReadOnlySpan<T>((T[])(object)stringValue.ToCharArray());
                    public ReadOnlySpan<T> Slice(int offset, int length)
                    {
                        var copy = new T[length];
                        Array.Copy(arr, offset, copy, 0, length);
                        return new ReadOnlySpan<T>(copy);
                    }
                }
                public readonly ref struct SpanLike<T>
                {
                    public readonly Span<T> field;
                }
                public enum Color: sbyte
                {
                    Red,
                    Green,
                    Blue
                }
                public static unsafe class Helpers
                {
                    public static T[] ToArray<T>(void* ptr, int count)
                    {
                        if (ptr == null)
                        {
                            return null;
                        }
                        if (typeof(T) == typeof(int))
                        {
                            var arr = new int[count];
                            for(int i = 0; i < count; i++)
                            {
                                arr[i] = ((int*)ptr)[i];
                            }
                            return (T[])(object)arr;
                        }
                        if (typeof(T) == typeof(byte))
                        {
                            var arr = new byte[count];
                            for(int i = 0; i < count; i++)
                            {
                                arr[i] = ((byte*)ptr)[i];
                            }
                            return (T[])(object)arr;
                        }
                        if (typeof(T) == typeof(char))
                        {
                            var arr = new char[count];
                            for(int i = 0; i < count; i++)
                            {
                                arr[i] = ((char*)ptr)[i];
                            }
                            return (T[])(object)arr;
                        }
                        if (typeof(T) == typeof(Color))
                        {
                            var arr = new Color[count];
                            for(int i = 0; i < count; i++)
                            {
                                arr[i] = ((Color*)ptr)[i];
                            }
                            return (T[])(object)arr;
                        }
                        throw new Exception("add a case for: " + typeof(T));
                    }
                }
            }
            """;

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

        internal override Type GetCompletionProviderType()
            => typeof(DeclarationNameCompletionProvider);

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/48310")]
        [InlineData("record")]
        [InlineData("record class")]
        [InlineData("record struct")]
        public async Task TreatRecordPositionalParameterAsProperty(string record)
        {
            var markup = $@"
public class MyClass
{{
}}

public {record} R(MyClass $$
";
            await VerifyItemExistsAsync(markup, "MyClass", glyph: (int)Glyph.PropertyPublic);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        public async Task DoNotTreatPrimaryConstructorParameterAsProperty(string record)
        {
            var markup = $@"
public class MyClass
{{
}}

public {record} R(MyClass $$
";
            await VerifyItemIsAbsentAsync(markup, "MyClass");
        }

        [Fact]
        public async Task NameWithOnlyType1()
        {
            var markup = """
                public class MyClass
                {
                    MyClass $$
                }
                """;
            await VerifyItemExistsAsync(markup, "myClass", glyph: (int)Glyph.FieldPublic);
            await VerifyItemExistsAsync(markup, "MyClass", glyph: (int)Glyph.PropertyPublic);
            await VerifyItemExistsAsync(markup, "GetMyClass", glyph: (int)Glyph.MethodPublic);
        }

        [Fact]
        public async Task AsyncTaskOfT()
        {
            var markup = """
                using System.Threading.Tasks;
                public class C
                {
                    async Task<C> $$
                }
                """;
            await VerifyItemExistsAsync(markup, "GetCAsync");
        }

        [Fact(Skip = "not yet implemented")]
        public async Task NonAsyncTaskOfT()
        {
            var markup = """
                public class C
                {
                    Task<C> $$
                }
                """;
            await VerifyItemExistsAsync(markup, "GetCAsync");
        }

        [Fact]
        public async Task MethodDeclaration1()
        {
            var markup = """
                public class C
                {
                    virtual C $$
                }
                """;
            await VerifyItemExistsAsync(markup, "GetC");
            await VerifyItemIsAbsentAsync(markup, "C");
            await VerifyItemIsAbsentAsync(markup, "c");
        }

        [Fact]
        public async Task WordBreaking1()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    CancellationToken $$
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken");
            await VerifyItemExistsAsync(markup, "cancellation");
            await VerifyItemExistsAsync(markup, "token");
        }

        [Fact]
        public async Task WordBreaking2()
        {
            var markup = """
                interface I {}
                public class C
                {
                    I $$
                }
                """;
            await VerifyItemExistsAsync(markup, "GetI");
        }

        [Fact]
        public async Task WordBreaking3()
        {
            var markup = """
                interface II {}
                public class C
                {
                    II $$
                }
                """;
            await VerifyItemExistsAsync(markup, "GetI");
        }

        [Fact]
        public async Task WordBreaking4()
        {
            var markup = """
                interface IGoo {}
                public class C
                {
                    IGoo $$
                }
                """;
            await VerifyItemExistsAsync(markup, "Goo");
        }

        [Fact]
        public async Task WordBreaking5()
        {
            var markup = """
                class SomeWonderfullyLongClassName {}
                public class C
                {
                    SomeWonderfullyLongClassName $$
                }
                """;
            await VerifyItemExistsAsync(markup, "Some");
            await VerifyItemExistsAsync(markup, "SomeWonderfully");
            await VerifyItemExistsAsync(markup, "SomeWonderfullyLong");
            await VerifyItemExistsAsync(markup, "SomeWonderfullyLongClass");
            await VerifyItemExistsAsync(markup, "Name");
            await VerifyItemExistsAsync(markup, "ClassName");
            await VerifyItemExistsAsync(markup, "LongClassName");
            await VerifyItemExistsAsync(markup, "WonderfullyLongClassName");
            await VerifyItemExistsAsync(markup, "SomeWonderfullyLongClassName");
        }

        [Fact]
        public async Task Parameter1()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    void Goo(CancellationToken $$
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [Fact]
        public async Task Parameter2()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    void Goo(int x, CancellationToken c$$
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [Fact]
        public async Task Parameter3()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    void Goo(CancellationToken c$$) {}
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
        public async Task Parameter4()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    void Other(CancellationToken cancellationToken) {}
                    void Goo(CancellationToken c$$) {}
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [Fact]
        public async Task Parameter5()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    void Goo(CancellationToken cancellationToken, CancellationToken c$$) {}
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken1", glyph: (int)Glyph.Parameter);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
        public async Task Parameter6()
        {
            var markup = """
                using System.Threading;

                void Other(CancellationToken cancellationToken) {}
                void Goo(CancellationToken c$$) {}
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [Fact]
        public async Task Parameter7()
        {
            var markup = """
                using System.Threading;

                void Goo(CancellationToken cancellationToken, CancellationToken c$$) {}
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken1", glyph: (int)Glyph.Parameter);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
        public async Task Parameter8()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    int this[CancellationToken cancellationToken] => throw null;
                    int this[CancellationToken c$$] => throw null;
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
        public async Task Parameter9()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    int this[CancellationToken cancellationToken] => throw null;
                    int this[CancellationToken cancellationToken, CancellationToken c$$] => throw null;
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken1", glyph: (int)Glyph.Parameter);
        }

        [InlineData(LanguageVersion.CSharp7)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.Latest)]
        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/42049")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
        public async Task Parameter10(LanguageVersion languageVersion)
        {
            var source = """
                public class DbContext { }
                public class C
                {
                    void Goo(DbContext context) {
                        void InnerGoo(DbContext $$) { }
                    }
                }
                """;
            var markup = GetMarkup(source, languageVersion);
            await VerifyItemExistsAsync(markup, "dbContext", glyph: (int)Glyph.Parameter);
            await VerifyItemExistsAsync(markup, "db", glyph: (int)Glyph.Parameter);

            if (languageVersion.MapSpecifiedToEffectiveVersion() >= LanguageVersion.CSharp8)
            {
                await VerifyItemExistsAsync(markup, "context", glyph: (int)Glyph.Parameter);
            }
            else
            {
                await VerifyItemExistsAsync(markup, "context1", glyph: (int)Glyph.Parameter);
            }
        }

        [InlineData(LanguageVersion.CSharp7)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.Latest)]
        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/42049")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
        public async Task Parameter11(LanguageVersion languageVersion)
        {
            var source = """
                public class DbContext { }
                public class C
                {
                    void Goo() {
                        DbContext context;
                        void InnerGoo(DbContext $$) { }
                    }
                }
                """;
            var markup = GetMarkup(source, languageVersion);
            await VerifyItemExistsAsync(markup, "dbContext", glyph: (int)Glyph.Parameter);
            await VerifyItemExistsAsync(markup, "db", glyph: (int)Glyph.Parameter);

            if (languageVersion.MapSpecifiedToEffectiveVersion() >= LanguageVersion.CSharp8)
            {
                await VerifyItemExistsAsync(markup, "context", glyph: (int)Glyph.Parameter);
            }
            else
            {
                await VerifyItemExistsAsync(markup, "context1", glyph: (int)Glyph.Parameter);
            }
        }

        [InlineData(LanguageVersion.CSharp7)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.Latest)]
        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/42049")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
        public async Task Parameter12(LanguageVersion languageVersion)
        {
            var source = """
                public class DbContext { }
                public class C
                {
                    DbContext dbContext;
                    void Goo(DbContext context) {
                        void InnerGoo(DbContext $$) { }
                    }
                }
                """;
            var markup = GetMarkup(source, languageVersion);
            await VerifyItemExistsAsync(markup, "dbContext", glyph: (int)Glyph.Parameter);
            await VerifyItemExistsAsync(markup, "db", glyph: (int)Glyph.Parameter);

            if (languageVersion.MapSpecifiedToEffectiveVersion() >= LanguageVersion.CSharp8)
            {
                await VerifyItemExistsAsync(markup, "context", glyph: (int)Glyph.Parameter);
            }
            else
            {
                await VerifyItemExistsAsync(markup, "context1", glyph: (int)Glyph.Parameter);
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36248")]
        public async Task Parameter13()
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(GetComposition());

            var options = new CompletionOptions()
            {
                NamingStyleFallbackOptions = ParameterCamelCaseWithPascalCaseFallback()
            };

            var markup = """
                using System.Threading;
                public class C
                {
                    void Goo(CancellationToken $$
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter, options: options);
            await VerifyItemIsAbsentAsync(markup, "CancellationToken", options: options);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52534")]
        public async Task SuggestParameterNamesFromExistingOverloads()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    void M(CancellationToken myTok) { }

                    void M(CancellationToken $$
                }
                """;
            await VerifyItemExistsAsync(markup, "myTok", glyph: (int)Glyph.Parameter);
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52534")]
        public async Task SuggestParameterNamesFromExistingOverloads_Constructor()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    public C(string firstName, string middleName, string lastName) { }

                    public C(string firstName, string $$)
                }
                """;
            await VerifyItemExistsAsync(markup, "middleName", glyph: (int)Glyph.Parameter);
            await VerifyItemExistsAsync(markup, "lastName", glyph: (int)Glyph.Parameter);
            await VerifyItemIsAbsentAsync(markup, "firstName");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52534")]
        public async Task DoNotSuggestParameterNamesFromTheSameOverload()
        {
            var markup = """
                public class C
                {
                    void M(string name, string $$) { }
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "name");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52534")]
        public async Task DoNotSuggestParameterNamesFromNonOverloads()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    void M1(CancellationToken myTok) { }

                    void M2(CancellationToken $$
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "myTok");
            await VerifyItemExistsAsync(markup, "cancellationToken", glyph: (int)Glyph.Parameter);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52534")]
        public async Task DoNotSuggestInGenericType()
        {
            var markup = """
                using System.Collections.Generic;
                public class C
                {
                    void M(IEnumerable<int> numbers) { }

                    void M(List<$$>) { }
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52534")]
        public async Task DoNotSuggestInOptionalParameterDefaultValue()
        {
            var markup = """
                using System.Collections.Generic;
                public class C
                {
                    private const int ZERO = 0;
                    void M(int num = ZERO) { }

                    void M(int x, int num = $$) { }
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19260")]
        public async Task EscapeKeywords1()
        {
            var markup = """
                using System.Text;
                public class C
                {
                    void Goo(StringBuilder $$) {}
                }
                """;
            await VerifyItemExistsAsync(markup, "stringBuilder", glyph: (int)Glyph.Parameter);
            await VerifyItemExistsAsync(markup, "@string", glyph: (int)Glyph.Parameter);
            await VerifyItemExistsAsync(markup, "builder", glyph: (int)Glyph.Parameter);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19260")]
        public async Task EscapeKeywords2()
        {
            var markup = """
                class For { }
                public class C
                {
                    void Goo(For $$) {}
                }
                """;
            await VerifyItemExistsAsync(markup, "@for", glyph: (int)Glyph.Parameter);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19260")]
        public async Task EscapeKeywords3()
        {
            var markup = """
                class For { }
                public class C
                {
                    void goo()
                    {
                        For $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "@for");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19260")]
        public async Task EscapeKeywords4()
        {
            var markup = """
                using System.Text;
                public class C
                {
                    void goo()
                    {
                        StringBuilder $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "stringBuilder");
            await VerifyItemExistsAsync(markup, "@string");
            await VerifyItemExistsAsync(markup, "builder");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25214")]
        public async Task TypeImplementsLazyOfType1()
        {
            var markup = """
                using System;
                using System.Collections.Generic;

                internal class Example
                {
                    public Lazy<Item> $$
                }

                public class Item { }
                """;
            await VerifyItemExistsAsync(markup, "item");
            await VerifyItemExistsAsync(markup, "Item");
            await VerifyItemExistsAsync(markup, "GetItem");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25214")]
        public async Task TypeImplementsLazyOfType2()
        {
            var markup = """
                using System;
                using System.Collections.Generic;

                internal class Example
                {
                    public List<Lazy<Item>> $$
                }

                public class Item { }
                """;
            await VerifyItemExistsAsync(markup, "items");
            await VerifyItemExistsAsync(markup, "Items");
            await VerifyItemExistsAsync(markup, "GetItems");
        }

        [Fact]
        public async Task NoSuggestionsForInt()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    int $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForLong()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    long $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForDouble()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    double $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForFloat()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    float $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForSbyte()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    sbyte $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForShort()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    short $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForUint()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    uint $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForUlong()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    ulong $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task SuggestionsForUShort()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    ushort $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForBool()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    bool $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForByte()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    byte $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForChar()
        {
            var markup = """
                using System.Threading;
                public class C
                {
                    char $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSuggestionsForString()
        {
            var markup = """
                public class C
                {
                    string $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NoSingleLetterClassNameSuggested()
        {
            var markup = """
                public class C
                {
                    C $$
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "C");
            await VerifyItemIsAbsentAsync(markup, "c");
        }

        [Fact]
        public async Task ArrayElementTypeSuggested()
        {
            var markup = """
                using System.Threading;
                public class MyClass
                {
                    MyClass[] $$
                }
                """;
            await VerifyItemExistsAsync(markup, "MyClasses");
            await VerifyItemIsAbsentAsync(markup, "Array");
        }

        [Fact]
        public async Task NotTriggeredByVar()
        {
            var markup = """
                public class C
                {
                    var $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task NotAfterVoid()
        {
            var markup = """
                public class C
                {
                    void $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task AfterGeneric()
        {
            var markup = """
                public class C
                {
                    System.Collections.Generic.IEnumerable<C> $$
                }
                """;
            await VerifyItemExistsAsync(markup, "GetCs");
        }

        [Fact]
        public async Task NothingAfterVar()
        {
            var markup = """
                public class C
                {
                    void goo()
                    {
                        var $$
                    }
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        public async Task TestCorrectOrder()
        {
            var markup = """
                public class MyClass
                {
                    MyClass $$
                }
                """;
            var items = await GetCompletionItemsAsync(markup, SourceCodeKind.Regular);
            Assert.Equal(
                new[] { "myClass", "my", "@class", "MyClass", "My", "Class", "GetMyClass", "GetMy", "GetClass" },
                items.Select(item => item.DisplayText));
        }

        [Fact]
        public async Task TestDescriptionInsideClass()
        {
            var markup = """
                public class MyClass
                {
                    MyClass $$
                }
                """;
            await VerifyItemExistsAsync(markup, "myClass", glyph: (int)Glyph.FieldPublic, expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
            await VerifyItemExistsAsync(markup, "MyClass", glyph: (int)Glyph.PropertyPublic, expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
            await VerifyItemExistsAsync(markup, "GetMyClass", glyph: (int)Glyph.MethodPublic, expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact]
        public async Task TestDescriptionInsideMethod()
        {
            var markup = """
                public class MyClass
                {
                    void M()
                    {
                        MyClass $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "myClass", glyph: (int)Glyph.Local, expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
            await VerifyItemIsAbsentAsync(markup, "MyClass");
            await VerifyItemIsAbsentAsync(markup, "GetMyClass");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20273")]
        public async Task Alias1()
        {
            var markup = """
                using MyType = System.String;
                public class C
                {
                    MyType $$
                }
                """;
            await VerifyItemExistsAsync(markup, "my");
            await VerifyItemExistsAsync(markup, "type");
            await VerifyItemExistsAsync(markup, "myType");
        }
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20273")]
        public async Task AliasWithInterfacePattern()
        {
            var markup = """
                using IMyType = System.String;
                public class C
                {
                    MyType $$
                }
                """;
            await VerifyItemExistsAsync(markup, "my");
            await VerifyItemExistsAsync(markup, "type");
            await VerifyItemExistsAsync(markup, "myType");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20016")]
        public async Task NotAfterExistingName1()
        {
            var markup = """
                using IMyType = System.String;
                public class C
                {
                    MyType myType $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20016")]
        public async Task NotAfterExistingName2()
        {
            var markup = """
                using IMyType = System.String;
                public class C
                {
                    MyType myType, MyType $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19409")]
        public async Task OutVarArgument()
        {
            var markup = """
                class Test
                {
                    void Do(out Test goo)
                    {
                        Do(out var $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "test");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19409")]
        public async Task OutArgument()
        {
            var markup = """
                class Test
                {
                    void Do(out Test goo)
                    {
                        Do(out Test $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "test");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19409")]
        public async Task OutGenericArgument()
        {
            var markup = """
                class Test
                {
                    void Do<T>(out T goo)
                    {
                        Do(out Test $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "test");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleExpressionDeclaration1()
        {
            var markup = """
                class Test
                {
                    void Do()
                    {
                        (System.Array array, System.Action $$ 
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "action");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleExpressionDeclaration2()
        {
            var markup = """
                class Test
                {
                    void Do()
                    {
                        (array, action $$
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "action");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleExpressionDeclaration_NestedTuples()
        {
            var markup = """
                class Test
                {
                    void Do()
                    {
                        ((int i1, int i2), (System.Array array, System.Action $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "action");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleExpressionDeclaration_NestedTuples_CompletionInTheMiddle()
        {
            var markup = """
                class Test
                {
                    void Do()
                    {
                        ((System.Array array, System.Action $$), (int i1, int i2))
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "action");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleElementDefinition1()
        {
            var markup = """
                class Test
                {
                    void Do()
                    {
                        (System.Array $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "array");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleElementDefinition2()
        {
            var markup = """
                class Test
                {
                    (System.Array $$) Test() => default;
                }
                """;
            await VerifyItemExistsAsync(markup, "array");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleElementDefinition3()
        {
            var markup = """
                class Test
                {
                    (System.Array array, System.Action $$) Test() => default;
                }
                """;
            await VerifyItemExistsAsync(markup, "action");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleElementDefinition4()
        {
            var markup = """
                class Test
                {
                    (System.Array $$
                }
                """;
            await VerifyItemExistsAsync(markup, "array");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleElementDefinition5()
        {
            var markup = """
                class Test
                {
                    void M((System.Array $$
                }
                """;
            await VerifyItemExistsAsync(markup, "array");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleElementDefinition_NestedTuples()
        {
            var markup = """
                class Test
                {
                    void M(((int, int), (int, System.Array $$
                }
                """;
            await VerifyItemExistsAsync(markup, "array");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleElementDefinition_InMiddleOfTuple()
        {
            var markup = """
                class Test
                {
                    void M((int, System.Array $$),int)
                }
                """;
            await VerifyItemExistsAsync(markup, "array");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleElementTypeInference()
        {
            var markup = """
                class Test
                {
                    void Do()
                    {
                        (var accessViolationException, var $$) = (new AccessViolationException(), new Action(() => { }));
                    }
                }
                """;
            // Currently not supported:
            await VerifyItemIsAbsentAsync(markup, "action");
            // see https://github.com/dotnet/roslyn/issues/27138
            // after the issue ist fixed we expect this to work:
            // await VerifyItemExistsAsync(markup, "action");
        }

        [Fact(Skip = "Not yet supported")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleElementInGenericTypeArgument()
        {
            var markup = """
                class Test
                {
                    void Do()
                    {
                        System.Func<(System.Action $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "action");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
        public async Task TupleElementInvocationInsideTuple()
        {
            var markup = """
                class Test
                {
                    void Do()
                    {
                            int M(int i1, int i2) => i1;
                            var t=(e1: 1, e2: M(1, $$));
                    }
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17987")]
        public async Task Pluralize1()
        {
            var markup = """
                using System.Collections.Generic;
                class Index
                {
                    IEnumerable<Index> $$
                }
                """;
            await VerifyItemExistsAsync(markup, "Indices");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17987")]
        public async Task Pluralize2()
        {
            var markup = """
                using System.Collections.Generic;
                class Test
                {
                    IEnumerable<IEnumerable<Test>> $$
                }
                """;
            await VerifyItemExistsAsync(markup, "tests");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17987")]
        public async Task Pluralize3()
        {
            var markup = """
                using System.Collections.Generic;
                using System.Threading;
                class Test
                {
                    IEnumerable<CancellationToken> $$
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationTokens");
            await VerifyItemExistsAsync(markup, "cancellations");
            await VerifyItemExistsAsync(markup, "tokens");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17987")]
        public async Task PluralizeList()
        {
            var markup = """
                using System.Collections.Generic;
                using System.Threading;
                class Test
                {
                    List<CancellationToken> $$
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationTokens");
            await VerifyItemExistsAsync(markup, "cancellations");
            await VerifyItemExistsAsync(markup, "tokens");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17987")]
        public async Task PluralizeArray()
        {
            var markup = """
                using System.Collections.Generic;
                using System.Threading;
                class Test
                {
                    CancellationToken[] $$
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationTokens");
            await VerifyItemExistsAsync(markup, "cancellations");
            await VerifyItemExistsAsync(markup, "tokens");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37366")]
        public async Task PluralizeSpan()
        {
            var markup = """
                using System;

                class Test
                {
                    void M(Span<Test> $$) { }
                }
                """ + Span;
            await VerifyItemExistsAsync(markup, "tests");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37366")]
        public async Task PluralizeValidGetEnumerator()
        {
            var markup = """
                class MyClass
                {
                    public void M(MyOwnCollection<MyClass> $$) { }
                }


                class MyOwnCollection<T>
                {
                    public MyEnumerator GetEnumerator()
                    {
                        return new MyEnumerator();
                    }

                    public class MyEnumerator
                    {
                        public T Current { get; }

                        public bool MoveNext() { return false; }
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "myClasses");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37366")]
        public async Task PluralizeValidGetAsyncEnumerator()
        {
            var markup = """
                using System.Threading.Tasks;

                class MyClass
                {
                    public void M(MyOwnCollection<MyClass> $$) { }
                }


                class MyOwnCollection<T>
                {
                    public MyEnumerator GetAsyncEnumerator()
                    {
                        return new MyEnumerator();
                    }

                    public class MyEnumerator
                    {
                        public T Current { get; }

                        public Task<bool> MoveNextAsync() { return Task.FromResult(false); }
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "myClasses");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37366")]
        public async Task PluralizeForUnimplementedIEnumerable()
        {
            var markup = """
                using System.Collections.Generic;

                class MyClass
                {
                    public void M(MyOwnCollection<MyClass> $$) { }
                }


                class MyOwnCollection<T> : IEnumerable<T>
                {
                }
                """;
            await VerifyItemExistsAsync(markup, "myClasses");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37366")]
        public async Task PluralizeForUnimplementedIAsyncEnumerable()
        {
            var markup = """
                using System.Collections.Generic;

                class MyClass
                {
                    public void M(MyOwnCollection<MyClass> $$) { }
                }


                class MyOwnCollection<T> : IAsyncEnumerable<T>
                {
                }
                """ + IAsyncEnumerable;
            await VerifyItemExistsAsync(markup, "myClasses");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23497")]
        public async Task InPatternMatching1()
        {
            var markup = """
                using System.Threading;

                public class C
                {
                    public static void Main()
                    {
                        object obj = null;
                        if (obj is CancellationToken $$) { }
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken");
            await VerifyItemExistsAsync(markup, "cancellation");
            await VerifyItemExistsAsync(markup, "token");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23497")]
        public async Task InPatternMatching2()
        {
            var markup = """
                using System.Threading;

                public class C
                {
                    public static bool Foo()
                    {
                        object obj = null;
                        return obj is CancellationToken $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken");
            await VerifyItemExistsAsync(markup, "cancellation");
            await VerifyItemExistsAsync(markup, "token");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23497")]
        public async Task InPatternMatching3()
        {
            var markup = """
                using System.Threading;

                public class C
                {
                    public static void Main()
                    {
                        object obj = null;
                        switch(obj)
                        {
                            case CancellationToken $$
                        }
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken");
            await VerifyItemExistsAsync(markup, "cancellation");
            await VerifyItemExistsAsync(markup, "token");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23497")]
        public async Task InPatternMatching4()
        {
            var markup = """
                using System.Threading;

                public class C
                {
                    public static void Main()
                    {
                        object obj = null;
                        if (obj is CancellationToken ca$$) { }
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken");
            await VerifyItemExistsAsync(markup, "cancellation");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23497")]
        public async Task InPatternMatching5()
        {
            var markup = """
                using System.Threading;

                public class C
                {
                    public static bool Foo()
                    {
                        object obj = null;
                        return obj is CancellationToken to$$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken");
            await VerifyItemExistsAsync(markup, "token");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23497")]
        public async Task InPatternMatching6()
        {
            var markup = """
                using System.Threading;

                public class C
                {
                    public static void Main()
                    {
                        object obj = null;
                        switch(obj)
                        {
                            case CancellationToken to$$
                        }
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "cancellationToken");
            await VerifyItemExistsAsync(markup, "token");
        }

        [Fact]
        public async Task InUsingStatement1()
        {
            var markup = """
                using System.IO;

                class C
                {
                    void M()
                    {
                        using (StreamReader s$$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "streamReader");
        }

        [Fact]
        public async Task InUsingStatement2()
        {
            var markup = """
                using System.IO;

                class C
                {
                    void M()
                    {
                        using (StreamReader s1, $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "streamReader");
        }

        [Fact]
        public async Task InUsingStatement_Var()
        {
            var markup = """
                using System.IO;

                class C
                {
                    void M()
                    {
                        using (var m$$ = new MemoryStream())
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "memoryStream");
        }

        [Fact]
        public async Task InForStatement1()
        {
            var markup = """
                using System.IO;

                class C
                {
                    void M()
                    {
                        for (StreamReader s$$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "streamReader");
        }

        [Fact]
        public async Task InForStatement2()
        {
            var markup = """
                using System.IO;

                class C
                {
                    void M()
                    {
                        for (StreamReader s1, $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "streamReader");
        }

        [Fact]
        public async Task InForStatement_Var()
        {
            var markup = """
                using System.IO;

                class C
                {
                    void M()
                    {
                        for (var m$$ = new MemoryStream();
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "memoryStream");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26021")]
        public async Task InForEachStatement()
        {
            var markup = """
                using System.IO;

                class C
                {
                    void M()
                    {
                        foreach (StreamReader $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "streamReader");
        }

        [Fact]
        public async Task InForEachStatement_Var()
        {
            var markup = """
                using System.IO;

                class C
                {
                    void M()
                    {
                        foreach (var m$$ in new[] { new MemoryStream() })
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "memoryStream");
        }

        [Fact]
        public async Task DisabledByOption()
        {
            ShowNameSuggestions = false;

            var markup = """
                class Test
                {
                    Test $$
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
        public async Task TypeImplementsIEnumerableOfType()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method()
                  {
                    Container $$
                  }
                }

                public class Container : ContainerBase { }
                public class ContainerBase : IEnumerable<ContainerBase> { }
                """;
            await VerifyItemExistsAsync(markup, "container");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
        public async Task TypeImplementsIEnumerableOfType2()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method()
                  {
                     Container $$
                  }
                }

                public class ContainerBase : IEnumerable<Container> { }
                public class Container : ContainerBase { }
                """;
            await VerifyItemExistsAsync(markup, "container");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
        public async Task TypeImplementsIEnumerableOfType3()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method()
                  {
                     Container $$
                  }
                }

                public class Container : IEnumerable<Container> { }
                """;
            await VerifyItemExistsAsync(markup, "container");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
        public async Task TypeImplementsIEnumerableOfType4()
        {
            var markup = """
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class Class1
                {
                  public void Method()
                  {
                     TaskType $$
                  }
                }

                public class ContainerBase : IEnumerable<Container> { }
                public class Container : ContainerBase { }
                public class TaskType : Task<Container> { }
                """;
            await VerifyItemExistsAsync(markup, "taskType");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
        public async Task TypeImplementsTaskOfType()
        {
            var markup = """
                using System.Threading.Tasks;

                public class Class1
                {
                  public void Method()
                  {
                    Container $$
                  }
                }

                public class Container : ContainerBase { }
                public class ContainerBase : Task<ContainerBase> { }
                """;
            await VerifyItemExistsAsync(markup, "container");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
        public async Task TypeImplementsTaskOfType2()
        {
            var markup = """
                using System.Threading.Tasks;

                public class Class1
                {
                  public void Method()
                  {
                     Container $$
                  }
                }

                public class Container : Task<ContainerBase> { }
                public class ContainerBase : Container { }
                """;
            await VerifyItemExistsAsync(markup, "container");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
        public async Task TypeImplementsTaskOfType3()
        {
            var markup = """
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class Class1
                {
                  public void Method()
                  {
                    EnumerableType $$
                  }
                }

                public class TaskType : TaskTypeBase { }
                public class TaskTypeBase : Task<TaskTypeBase> { }
                public class EnumerableType : IEnumerable<TaskType> { }
                """;
            await VerifyItemExistsAsync(markup, "taskTypes");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
        public async Task TypeIsNullableOfNullable()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method()
                  {
                      // This code isn't legal, but we want to ensure we don't crash in this broken code scenario
                      IEnumerable<Nullable<int?>> $$
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "nullables");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
        public async Task TypeIsNullableStructInLocalWithNullableTypeName()
        {
            var markup = """
                using System;

                public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> { }

                public class Class1
                {
                  public void Method()
                  {
                      Nullable<ImmutableArray<int>> $$
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "ints");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
        public async Task TypeIsNullableStructInLocalWithQuestionMark()
        {
            var markup = """
                using System.Collections.Immutable;

                public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> { }

                public class Class1
                {
                  public void Method()
                  {
                      ImmutableArray<int>? $$
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "ints");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
        public async Task TypeIsNullableReferenceInLocal()
        {
            var markup = """
                #nullable enable

                using System.Collections.Generic;

                public class Class1
                {
                  public void Method()
                  {
                      IEnumerable<int>? $$
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "ints");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
        public async Task TypeIsNullableStructInParameterWithNullableTypeName()
        {
            var markup = """
                using System;

                public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> { }

                public class Class1
                {
                  public void Method(Nullable<ImmutableArray<int>> $$)
                  {
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "ints");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
        public async Task TypeIsNullableStructInParameterWithQuestionMark()
        {
            var markup = """
                public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> { }

                public class Class1
                {
                  public void Method(ImmutableArray<int>? $$)
                  {
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "ints");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
        public async Task TypeIsNullableReferenceInParameter()
        {
            var markup = """
                #nullable enable

                using System.Collections.Generic;

                public class Class1
                {
                  public void Method(IEnumerable<int>? $$)
                  {
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "ints");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        public async Task EnumerableParameterOfUnmanagedType()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method(IEnumerable<int> $$)
                  {
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "ints");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        public async Task EnumerableParameterOfObject()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method(IEnumerable<object> $$)
                  {
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "objects");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        public async Task EnumerableParameterOfString()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method(IEnumerable<string> $$)
                  {
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "strings");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        public async Task EnumerableGenericTParameter()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method<T>(IEnumerable<T> $$)
                  {
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "values");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        public async Task EnumerableGenericTNameParameter()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method<TResult>(IEnumerable<TResult> $$)
                  {
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "results");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        public async Task EnumerableGenericUnexpectedlyNamedParameter()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method<Arg>(IEnumerable<Arg> $$)
                  {
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "args");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
        public async Task EnumerableGenericUnexpectedlyNamedParameterBeginsWithT()
        {
            var markup = """
                using System.Collections.Generic;

                public class Class1
                {
                  public void Method<Type>(IEnumerable<Type> $$)
                  {
                  }
                }
                """;
            await VerifyItemExistsAsync(markup, "types");
        }

        [Fact]
        public async Task CustomNamingStyleInsideClass()
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(GetComposition());

            var options = new CompletionOptions()
            {
                NamingStyleFallbackOptions = NamesEndWithSuffixPreferences()
            };

            var markup = """
                class Configuration
                {
                    Configuration $$
                }
                """;
            await VerifyItemExistsAsync(markup, "ConfigurationField", glyph: (int)Glyph.FieldPublic,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name, options: options);
            await VerifyItemExistsAsync(markup, "ConfigurationProperty", glyph: (int)Glyph.PropertyPublic,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name, options: options);
            await VerifyItemExistsAsync(markup, "ConfigurationMethod", glyph: (int)Glyph.MethodPublic,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name, options: options);
            await VerifyItemIsAbsentAsync(markup, "ConfigurationLocal", options: options);
            await VerifyItemIsAbsentAsync(markup, "ConfigurationLocalFunction", options: options);
        }

        [Fact]
        public async Task CustomNamingStyleInsideMethod()
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(GetComposition());

            var options = new CompletionOptions()
            {
                NamingStyleFallbackOptions = NamesEndWithSuffixPreferences()
            };

            var markup = """
                class Configuration
                {
                    void M()
                    {
                        Configuration $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "ConfigurationLocal", glyph: (int)Glyph.Local,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name, options: options);
            await VerifyItemExistsAsync(markup, "ConfigurationLocalFunction", glyph: (int)Glyph.MethodPublic,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name, options: options);
            await VerifyItemIsAbsentAsync(markup, "ConfigurationField", options: options);
            await VerifyItemIsAbsentAsync(markup, "ConfigurationMethod", options: options);
            await VerifyItemIsAbsentAsync(markup, "ConfigurationProperty", options: options);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
        public async Task TestCompletionDoesNotUseForeachVariableName()
        {
            var markup = """
                class ClassA
                {
                    class ClassB {}

                    readonly List<ClassB> classBList;

                    void M()
                    {
                        foreach (var classB in classBList)
                        {
                            ClassB $$
                        }
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "classB");
            await VerifyItemExistsAsync(markup, "classB1", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
        public async Task TestCompletionDoesNotUseParameterName()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }

                    void M(ClassB classB)
                    {
                        ClassB $$
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "classB");
            await VerifyItemExistsAsync(markup, "classB1", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
        public async Task TestCompletionCanUsePropertyName()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }

                    ClassB classB { get; set; }

                    void M()
                    {
                        ClassB $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "classB", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
        public async Task TestCompletionCanUseFieldName()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }

                    ClassB classB;

                    void M()
                    {
                        ClassB $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "classB", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
        public async Task TestCompletionDoesNotUseLocalName()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }

                    void M()
                    {
                        ClassB classB = new ClassB();
                        ClassB $$
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "classB");
            await VerifyItemExistsAsync(markup, "classB1", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
        public async Task TestCompletionDoesNotUseLocalNameMultiple()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }

                    void M()
                    {
                        ClassB classB = new ClassB();
                        ClassB classB1 = new ClassB();
                        ClassB $$
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "classB");
            await VerifyItemIsAbsentAsync(markup, "classB1");
            await VerifyItemExistsAsync(markup, "classB2", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
        public async Task TestCompletionDoesNotUseLocalInsideIf()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }

                    void M(bool flag)
                    {
                        ClassB $$
                        if (flag)
                        {
                            ClassB classB = new ClassB();
                        }
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "classB");
            await VerifyItemExistsAsync(markup, "classB1", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
        public async Task TestCompletionCanUseClassName()
        {
            var markup = """
                class classA
                {
                    void M()
                    {
                        classA $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "classA", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
        public async Task TestCompletionCanUseLocalInDifferentScope()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }

                    void M()
                    {
                        ClassB classB = new ClassB(); 
                    }

                    void M2()
                    {
                        ClassB $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "classB", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [InlineData(LanguageVersion.CSharp7)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.Latest)]
        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/42049")]
        public async Task TestUseLocalAsLocalFunctionParameter(LanguageVersion languageVersion)
        {
            var source = """
                class ClassA
                {
                    class ClassB { }
                    void M()
                    {
                        ClassB classB = new ClassB();
                        void LocalM1(ClassB $$) { }
                    }
                }
                """;
            var markup = GetMarkup(source, languageVersion);

            if (languageVersion.MapSpecifiedToEffectiveVersion() >= LanguageVersion.CSharp8)
            {
                await VerifyItemExistsAsync(markup, "classB", glyph: (int)Glyph.Parameter,
                        expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
            }
            else
            {
                await VerifyItemIsAbsentAsync(markup, "classB");
            }
        }

        [InlineData(LanguageVersion.CSharp7)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.Latest)]
        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/42049")]
        public async Task TestCompletionDoesNotUseLocalAsLocalFunctionVariable(LanguageVersion languageVersion)
        {
            var source = """
                class ClassA
                {
                    class ClassB { }
                    void M()
                    {
                        ClassB classB = new ClassB();
                        void LocalM1()
                        {
                            ClassB $$
                        }
                    }
                }
                """;
            var markup = GetMarkup(source, languageVersion);
            await VerifyItemIsAbsentAsync(markup, "classB");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
        public async Task TestCompletionDoesNotUseLocalInNestedLocalFunction()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }
                    void M()
                    {
                        ClassB classB = new ClassB();
                        void LocalM1()
                        {
                            void LocalM2()
                            {
                                ClassB $$
                            }
                        }
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "classB");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
        public async Task TestCompletionDoesNotUseLocalFunctionParameterInNestedLocalFunction()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }
                    void M()
                    {
                        void LocalM1(ClassB classB)
                        {
                            void LocalM2()
                            {
                                ClassB $$
                            }
                        }
                    }
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "classB");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
        public async Task TestCompletionCanUseLocalFunctionParameterAsParameter()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }
                    void M()
                    {
                        void LocalM1(ClassB classB) { }
                        void LocalM2(ClassB $$) { }
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "classB", glyph: (int)Glyph.Parameter,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
        public async Task TestCompletionCanUseLocalFunctionVariableAsParameter()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }
                    void M()
                    {
                        void LocalM1()
                        {
                            ClassB classB
                        }
                        void LocalM2(ClassB $$) { }
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "classB", glyph: (int)Glyph.Parameter,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
        public async Task TestCompletionCanUseLocalFunctionParameterAsVariable()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }
                    void M()
                    {
                        void LocalM1(ClassB classB) { }
                        void LocalM2()
                        {
                            ClassB $$
                        }
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "classB", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
        public async Task TestCompletionCanUseLocalFunctionVariableAsVariable()
        {
            var markup = """
                class ClassA
                {
                    class ClassB { }
                    void M()
                    {
                        void LocalM1()
                        {
                            ClassB classB
                        }
                        void LocalM2()
                        {
                            ClassB $$
                        }
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "classB", glyph: (int)Glyph.Local,
                    expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        }

        [Fact]
        public async Task TestNotForUnboundAsync()
        {
            var markup = """
                class C
                {
                    async $$
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "async");
            await VerifyItemIsAbsentAsync(markup, "Async");
            await VerifyItemIsAbsentAsync(markup, "GetAsync");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/43816")]
        public async Task ConflictingLocalVariable()
        {
            using var workspaceFixture = GetOrCreateWorkspaceFixture();

            var workspace = workspaceFixture.Target.GetWorkspace(GetComposition());

            var options = new CompletionOptions()
            {
                NamingStyleFallbackOptions = MultipleCamelCaseLocalRules()
            };

            var markup = """
                public class MyClass
                {
                    void M()
                    {
                        MyClass myClass;
                        MyClass $$
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "myClass1", glyph: (int)Glyph.Local, options: options);
        }

        [Fact]
        public async Task TestNotForNonTypeSymbol()
        {
            var markup = """
                using System;
                class C
                {
                    Console.BackgroundColor $$
                }
                """;
            await VerifyItemIsAbsentAsync(markup, "consoleColor");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29487")]
        public async Task TestForOutParam1()
        {
            var markup = """
                using System.Threading;

                class C
                {
                    void Main()
                    {
                        Goo(out var $$)
                    }

                    void Goo(out CancellationToken interestingName)
                    {

                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "interestingName");
            await VerifyItemExistsAsync(markup, "cancellationToken");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43602")]
        public async Task TestForOutParam2()
        {
            var markup = """
                class C
                {
                    void Main()
                    {
                        int.TryParse("", out var $$)
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "result");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49791")]
        public async Task TestForErrorType1()
        {
            var markup = """
                class C
                {
                    void Main(string _rootPath)
                    {
                        _rootPath $$
                        _rootPath = null;
                    }
                }
                """;
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49791")]
        public async Task TestForErrorType2()
        {
            var markup = """
                class C
                {
                    void Main()
                    {
                        Goo $$
                        Goo = null;
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "goo");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36352")]
        public async Task InferCollectionInErrorCase1()
        {
            var markup = """
                class Customer { }

                class V
                {
                    void M(IEnumerable<Customer> $$)
                    {
                    }
                }
                """;
            await VerifyItemExistsAsync(markup, "customers");
        }

        private static NamingStylePreferences MultipleCamelCaseLocalRules()
        {
            var styles = new[]
            {
                SpecificationStyle(new SymbolKindOrTypeKind(SymbolKind.Local), name: "Local1"),
                SpecificationStyle(new SymbolKindOrTypeKind(SymbolKind.Local), name: "Local1"),
            };

            return new NamingStylePreferences(
                styles.Select(t => t.specification).ToImmutableArray(),
                styles.Select(t => t.style).ToImmutableArray(),
                styles.Select(t => CreateRule(t.specification, t.style)).ToImmutableArray());

            // Local functions

            static (SymbolSpecification specification, NamingStyle style) SpecificationStyle(SymbolKindOrTypeKind kind, string name)
            {
                var symbolSpecification = new SymbolSpecification(
                    Guid.NewGuid(),
                    name,
                    ImmutableArray.Create(kind));

                var namingStyle = new NamingStyle(
                    Guid.NewGuid(),
                    name,
                    capitalizationScheme: Capitalization.CamelCase);

                return (symbolSpecification, namingStyle);
            }
        }

        private static NamingStylePreferences NamesEndWithSuffixPreferences()
        {
            var specificationStyles = new[]
            {
                SpecificationStyle(new SymbolKindOrTypeKind(SymbolKind.Field), "Field"),
                SpecificationStyle(new SymbolKindOrTypeKind(SymbolKind.Property), "Property"),
                SpecificationStyle(new SymbolKindOrTypeKind(MethodKind.Ordinary), "Method"),
                SpecificationStyle(new SymbolKindOrTypeKind(SymbolKind.Local), "Local"),
                SpecificationStyle(new SymbolKindOrTypeKind(MethodKind.LocalFunction), "LocalFunction"),
            };

            return new NamingStylePreferences(
                specificationStyles.Select(t => t.specification).ToImmutableArray(),
                specificationStyles.Select(t => t.style).ToImmutableArray(),
                specificationStyles.Select(t => CreateRule(t.specification, t.style)).ToImmutableArray());

            // Local functions

            static (SymbolSpecification specification, NamingStyle style) SpecificationStyle(SymbolKindOrTypeKind kind, string suffix)
            {
                var symbolSpecification = new SymbolSpecification(
                    Guid.NewGuid(),
                    name: suffix,
                    ImmutableArray.Create(kind),
                    accessibilityList: default,
                    modifiers: default);

                var namingStyle = new NamingStyle(
                    Guid.NewGuid(),
                    name: suffix,
                    capitalizationScheme: Capitalization.PascalCase,
                    prefix: "",
                    suffix: suffix,
                    wordSeparator: "");

                return (symbolSpecification, namingStyle);
            }
        }

        private static NamingStylePreferences ParameterCamelCaseWithPascalCaseFallback()
        {
            var symbolSpecifications = ImmutableArray.Create(
                new SymbolSpecification(
                    id: Guid.NewGuid(),
                    name: "parameters",
                    ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Parameter)),
                    accessibilityList: default,
                    modifiers: default),
                new SymbolSpecification(
                    id: Guid.NewGuid(),
                    name: "fallback",
                    ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Parameter), new SymbolKindOrTypeKind(SymbolKind.Local)),
                    accessibilityList: default,
                    modifiers: default));
            var namingStyles = ImmutableArray.Create(
                new NamingStyle(
                    Guid.NewGuid(),
                    name: "parameter",
                    capitalizationScheme: Capitalization.CamelCase,
                    prefix: "",
                    suffix: "",
                    wordSeparator: ""),
                new NamingStyle(
                    Guid.NewGuid(),
                    name: "any_symbol",
                    capitalizationScheme: Capitalization.PascalCase,
                    prefix: "",
                    suffix: "",
                    wordSeparator: ""));
            return new NamingStylePreferences(
                symbolSpecifications,
                namingStyles,
                namingRules: ImmutableArray.Create(
                    CreateRule(symbolSpecifications[0], namingStyles[0]),
                    CreateRule(symbolSpecifications[1], namingStyles[1])));
        }

        private static SerializableNamingRule CreateRule(SymbolSpecification specification, NamingStyle style)
            => new()
            {
                SymbolSpecificationID = specification.ID,
                NamingStyleID = style.ID,
                EnforcementLevel = ReportDiagnostic.Error
            };
    }
}
