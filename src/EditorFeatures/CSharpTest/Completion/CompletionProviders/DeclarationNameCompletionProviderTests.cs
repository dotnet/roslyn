// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources;

using static SymbolSpecification;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class DeclarationNameCompletionProviderTests : AbstractCSharpCompletionProviderTests
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
    public Task TreatRecordPositionalParameterAsProperty(string record)
        => VerifyItemExistsAsync($$"""
            public class MyClass
            {
            }

            public {{record}} R(MyClass $$
            """, "MyClass", glyph: Glyph.PropertyPublic);

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public Task DoNotTreatPrimaryConstructorParameterAsProperty(string record)
        => VerifyItemIsAbsentAsync($$"""
            public class MyClass
            {
            }

            public {{record}} R(MyClass $$
            """, "MyClass");

    [Fact]
    public async Task NameWithOnlyType1()
    {
        var markup = """
            public class MyClass
            {
                MyClass $$
            }
            """;
        await VerifyItemExistsAsync(markup, "myClass", glyph: Glyph.FieldPublic);
        await VerifyItemExistsAsync(markup, "MyClass", glyph: Glyph.PropertyPublic);
        await VerifyItemExistsAsync(markup, "GetMyClass", glyph: Glyph.MethodPublic);
    }

    [Fact]
    public Task AsyncTaskOfT()
        => VerifyItemExistsAsync("""
            using System.Threading.Tasks;
            public class C
            {
                async Task<C> $$
            }
            """, "GetCAsync");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17989")]
    public Task NonAsyncTaskOfT()
        => VerifyItemExistsAsync("""
            using System.Threading.Tasks;
            public class C
            {
                Task<C> $$
            }
            """, "GetCAsync");

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
    public Task WordBreaking2()
        => VerifyItemExistsAsync("""
            interface I {}
            public class C
            {
                I $$
            }
            """, "GetI");

    [Fact]
    public Task WordBreaking3()
        => VerifyItemExistsAsync("""
            interface II {}
            public class C
            {
                II $$
            }
            """, "GetI");

    [Fact]
    public Task WordBreaking4()
        => VerifyItemExistsAsync("""
            interface IGoo {}
            public class C
            {
                IGoo $$
            }
            """, "Goo");

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
    public Task Parameter1()
        => VerifyItemExistsAsync("""
            using System.Threading;
            public class C
            {
                void Goo(CancellationToken $$
            }
            """, "cancellationToken", glyph: Glyph.Parameter);

    [Fact]
    public Task Parameter2()
        => VerifyItemExistsAsync("""
            using System.Threading;
            public class C
            {
                void Goo(int x, CancellationToken c$$
            }
            """, "cancellationToken", glyph: Glyph.Parameter);

    [Fact]
    public Task Parameter3()
        => VerifyItemExistsAsync("""
            using System.Threading;
            public class C
            {
                void Goo(CancellationToken c$$) {}
            }
            """, "cancellationToken", glyph: Glyph.Parameter);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
    public Task Parameter4()
        => VerifyItemExistsAsync("""
            using System.Threading;
            public class C
            {
                void Other(CancellationToken cancellationToken) {}
                void Goo(CancellationToken c$$) {}
            }
            """, "cancellationToken", glyph: Glyph.Parameter);

    [Fact]
    public Task Parameter5()
        => VerifyItemExistsAsync("""
            using System.Threading;
            public class C
            {
                void Goo(CancellationToken cancellationToken, CancellationToken c$$) {}
            }
            """, "cancellationToken1", glyph: Glyph.Parameter);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
    public Task Parameter6()
        => VerifyItemExistsAsync("""
            using System.Threading;

            void Other(CancellationToken cancellationToken) {}
            void Goo(CancellationToken c$$) {}
            """, "cancellationToken", glyph: Glyph.Parameter);

    [Fact]
    public Task Parameter7()
        => VerifyItemExistsAsync("""
            using System.Threading;

            void Goo(CancellationToken cancellationToken, CancellationToken c$$) {}
            """, "cancellationToken1", glyph: Glyph.Parameter);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
    public Task Parameter8()
        => VerifyItemExistsAsync("""
            using System.Threading;
            public class C
            {
                int this[CancellationToken cancellationToken] => throw null;
                int this[CancellationToken c$$] => throw null;
            }
            """, "cancellationToken", glyph: Glyph.Parameter);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45492")]
    public Task Parameter9()
        => VerifyItemExistsAsync("""
            using System.Threading;
            public class C
            {
                int this[CancellationToken cancellationToken] => throw null;
                int this[CancellationToken cancellationToken, CancellationToken c$$] => throw null;
            }
            """, "cancellationToken1", glyph: Glyph.Parameter);

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
        await VerifyItemExistsAsync(markup, "dbContext", glyph: Glyph.Parameter);
        await VerifyItemExistsAsync(markup, "db", glyph: Glyph.Parameter);

        if (languageVersion.MapSpecifiedToEffectiveVersion() >= LanguageVersion.CSharp8)
        {
            await VerifyItemExistsAsync(markup, "context", glyph: Glyph.Parameter);
        }
        else
        {
            await VerifyItemExistsAsync(markup, "context1", glyph: Glyph.Parameter);
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
        await VerifyItemExistsAsync(markup, "dbContext", glyph: Glyph.Parameter);
        await VerifyItemExistsAsync(markup, "db", glyph: Glyph.Parameter);

        if (languageVersion.MapSpecifiedToEffectiveVersion() >= LanguageVersion.CSharp8)
        {
            await VerifyItemExistsAsync(markup, "context", glyph: Glyph.Parameter);
        }
        else
        {
            await VerifyItemExistsAsync(markup, "context1", glyph: Glyph.Parameter);
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
        await VerifyItemExistsAsync(markup, "dbContext", glyph: Glyph.Parameter);
        await VerifyItemExistsAsync(markup, "db", glyph: Glyph.Parameter);

        if (languageVersion.MapSpecifiedToEffectiveVersion() >= LanguageVersion.CSharp8)
        {
            await VerifyItemExistsAsync(markup, "context", glyph: Glyph.Parameter);
        }
        else
        {
            await VerifyItemExistsAsync(markup, "context1", glyph: Glyph.Parameter);
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36248")]
    public async Task Parameter13()
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        var workspace = workspaceFixture.Target.GetWorkspace(GetComposition());
        workspace.SetAnalyzerFallbackOptions(new OptionsCollection(LanguageNames.CSharp)
        {
            { NamingStyleOptions.NamingPreferences, ParameterCamelCaseWithPascalCaseFallback() }
        });

        var markup = """
            using System.Threading;
            public class C
            {
                void Goo(CancellationToken $$
            }
            """;
        await VerifyItemExistsAsync(markup, "cancellationToken", glyph: Glyph.Parameter);
        await VerifyItemIsAbsentAsync(markup, "CancellationToken");
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
        await VerifyItemExistsAsync(markup, "myTok", glyph: Glyph.Parameter);
        await VerifyItemExistsAsync(markup, "cancellationToken", glyph: Glyph.Parameter);
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
        await VerifyItemExistsAsync(markup, "middleName", glyph: Glyph.Parameter);
        await VerifyItemExistsAsync(markup, "lastName", glyph: Glyph.Parameter);
        await VerifyItemIsAbsentAsync(markup, "firstName");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52534")]
    public Task DoNotSuggestParameterNamesFromTheSameOverload()
        => VerifyItemIsAbsentAsync("""
            public class C
            {
                void M(string name, string $$) { }
            }
            """, "name");

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
        await VerifyItemExistsAsync(markup, "cancellationToken", glyph: Glyph.Parameter);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52534")]
    public Task DoNotSuggestInGenericType()
        => VerifyNoItemsExistAsync("""
            using System.Collections.Generic;
            public class C
            {
                void M(IEnumerable<int> numbers) { }

                void M(List<$$>) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52534")]
    public Task DoNotSuggestInOptionalParameterDefaultValue()
        => VerifyNoItemsExistAsync("""
            using System.Collections.Generic;
            public class C
            {
                private const int ZERO = 0;
                void M(int num = ZERO) { }

                void M(int x, int num = $$) { }
            }
            """);

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
        await VerifyItemExistsAsync(markup, "stringBuilder", glyph: Glyph.Parameter);
        await VerifyItemExistsAsync(markup, "@string", glyph: Glyph.Parameter);
        await VerifyItemExistsAsync(markup, "builder", glyph: Glyph.Parameter);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19260")]
    public Task EscapeKeywords2()
        => VerifyItemExistsAsync("""
            class For { }
            public class C
            {
                void Goo(For $$) {}
            }
            """, "@for", glyph: Glyph.Parameter);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19260")]
    public Task EscapeKeywords3()
        => VerifyItemExistsAsync("""
            class For { }
            public class C
            {
                void goo()
                {
                    For $$
                }
            }
            """, "@for");

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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42721")]
    public async Task FuncOfT()
    {
        var markup = """
            using System;

            public class C
            {
                Func<string> $$
            }
            """;
        // Verify original Func-based suggestions still work
        await VerifyItemExistsAsync(markup, "Func");
        await VerifyItemExistsAsync(markup, "func");
        // Verify new Func-specific suggestions
        await VerifyItemExistsAsync(markup, "factory");
        await VerifyItemExistsAsync(markup, "stringFactory");
        await VerifyItemExistsAsync(markup, "selector");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42721")]
    public async Task FuncOfTwoArguments()
    {
        var markup = """
            using System;

            public class C
            {
                Func<int, string> $$
            }
            """;
        // Verify original Func-based suggestions still work
        await VerifyItemExistsAsync(markup, "Func");
        await VerifyItemExistsAsync(markup, "func");
        // Verify new Func-specific suggestions
        await VerifyItemExistsAsync(markup, "factory");
        await VerifyItemExistsAsync(markup, "stringFactory");
        await VerifyItemExistsAsync(markup, "selector");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42721")]
    public async Task FuncOfThreeArguments()
    {
        var markup = """
            using System;

            public class C
            {
                Func<int, bool, Customer> $$
            }

            public class Customer { }
            """;
        // Verify original Func-based suggestions still work
        await VerifyItemExistsAsync(markup, "Func");
        await VerifyItemExistsAsync(markup, "func");
        // Verify new Func-specific suggestions
        await VerifyItemExistsAsync(markup, "factory");
        await VerifyItemExistsAsync(markup, "customerFactory");
        await VerifyItemExistsAsync(markup, "selector");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42721")]
    public async Task FuncAsParameter()
    {
        var markup = """
            using System;

            public class C
            {
                void M(Func<Item> $$) { }
            }

            public class Item { }
            """;
        // Verify original Func-based suggestions still work
        await VerifyItemExistsAsync(markup, "func", glyph: Glyph.Parameter);
        // Verify new Func-specific suggestions
        await VerifyItemExistsAsync(markup, "factory", glyph: Glyph.Parameter);
        await VerifyItemExistsAsync(markup, "itemFactory", glyph: Glyph.Parameter);
        await VerifyItemExistsAsync(markup, "selector", glyph: Glyph.Parameter);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42721")]
    public async Task FuncAsLocalVariable()
    {
        var markup = """
            using System;

            public class C
            {
                void M()
                {
                    Func<Result> $$
                }
            }

            public class Result { }
            """;
        // Verify original Func-based suggestions still work
        await VerifyItemExistsAsync(markup, "func");
        // Verify new Func-specific suggestions
        await VerifyItemExistsAsync(markup, "factory");
        await VerifyItemExistsAsync(markup, "resultFactory");
        await VerifyItemExistsAsync(markup, "selector");
    }

    [Fact]
    public Task NoSuggestionsForInt()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                int $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForLong()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                long $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForDouble()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                double $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForFloat()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                float $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForSbyte()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                sbyte $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForShort()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                short $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForUint()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                uint $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForUlong()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                ulong $$
            }
            """);

    [Fact]
    public Task SuggestionsForUShort()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                ushort $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForBool()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                bool $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForByte()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                byte $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForChar()
        => VerifyNoItemsExistAsync("""
            using System.Threading;
            public class C
            {
                char $$
            }
            """);

    [Fact]
    public Task NoSuggestionsForString()
        => VerifyNoItemsExistAsync("""
            public class C
            {
                string $$
            }
            """);

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
    public Task NotTriggeredByVar()
        => VerifyNoItemsExistAsync("""
            public class C
            {
                var $$
            }
            """);

    [Fact]
    public Task NotAfterVoid()
        => VerifyNoItemsExistAsync("""
            public class C
            {
                void $$
            }
            """);

    [Fact]
    public Task AfterGeneric()
        => VerifyItemExistsAsync("""
            public class C
            {
                System.Collections.Generic.IEnumerable<C> $$
            }
            """, "GetCs");

    [Fact]
    public Task NothingAfterVar()
        => VerifyNoItemsExistAsync("""
            public class C
            {
                void goo()
                {
                    var $$
                }
            }
            """);

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
            ["myClass", "my", "@class", "MyClass", "My", "Class", "GetMyClass", "GetMy", "GetClass"],
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
        await VerifyItemExistsAsync(markup, "myClass", glyph: Glyph.FieldPublic, expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        await VerifyItemExistsAsync(markup, "MyClass", glyph: Glyph.PropertyPublic, expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        await VerifyItemExistsAsync(markup, "GetMyClass", glyph: Glyph.MethodPublic, expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
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
        await VerifyItemExistsAsync(markup, "myClass", glyph: Glyph.Local, expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
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
    public Task NotAfterExistingName1()
        => VerifyNoItemsExistAsync("""
            using IMyType = System.String;
            public class C
            {
                MyType myType $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20016")]
    public Task NotAfterExistingName2()
        => VerifyNoItemsExistAsync("""
            using IMyType = System.String;
            public class C
            {
                MyType myType, MyType $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19409")]
    public Task OutVarArgument()
        => VerifyItemExistsAsync("""
            class Test
            {
                void Do(out Test goo)
                {
                    Do(out var $$
                }
            }
            """, "test");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19409")]
    public Task OutArgument()
        => VerifyItemExistsAsync("""
            class Test
            {
                void Do(out Test goo)
                {
                    Do(out Test $$
                }
            }
            """, "test");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19409")]
    public Task OutGenericArgument()
        => VerifyItemExistsAsync("""
            class Test
            {
                void Do<T>(out T goo)
                {
                    Do(out Test $$
                }
            }
            """, "test");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleExpressionDeclaration1()
        => VerifyItemExistsAsync("""
            class Test
            {
                void Do()
                {
                    (System.Array array, System.Action $$ 
                }
            }
            """, "action");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleExpressionDeclaration2()
        => VerifyItemIsAbsentAsync("""
            class Test
            {
                void Do()
                {
                    (array, action $$
                }
            }
            """, "action");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleExpressionDeclaration_NestedTuples()
        => VerifyItemExistsAsync("""
            class Test
            {
                void Do()
                {
                    ((int i1, int i2), (System.Array array, System.Action $$
                }
            }
            """, "action");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleExpressionDeclaration_NestedTuples_CompletionInTheMiddle()
        => VerifyItemExistsAsync("""
            class Test
            {
                void Do()
                {
                    ((System.Array array, System.Action $$), (int i1, int i2))
                }
            }
            """, "action");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementDefinition1()
        => VerifyItemExistsAsync("""
            class Test
            {
                void Do()
                {
                    (System.Array $$
                }
            }
            """, "array");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementDefinition2()
        => VerifyItemExistsAsync("""
            class Test
            {
                (System.Array $$) Test() => default;
            }
            """, "array");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementDefinition3()
        => VerifyItemExistsAsync("""
            class Test
            {
                (System.Array array, System.Action $$) Test() => default;
            }
            """, "action");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementDefinition4()
        => VerifyItemExistsAsync("""
            class Test
            {
                (System.Array $$
            }
            """, "array");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementDefinition5()
        => VerifyItemExistsAsync("""
            class Test
            {
                void M((System.Array $$
            }
            """, "array");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementDefinition_NestedTuples()
        => VerifyItemExistsAsync("""
            class Test
            {
                void M(((int, int), (int, System.Array $$
            }
            """, "array");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementDefinition_InMiddleOfTuple()
        => VerifyItemExistsAsync("""
            class Test
            {
                void M((int, System.Array $$),int)
            }
            """, "array");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementTypeInference()
        => VerifyItemIsAbsentAsync("""
            class Test
            {
                void Do()
                {
                    (var accessViolationException, var $$) = (new AccessViolationException(), new Action(() => { }));
                }
            }
            """, "action");

    [Fact(Skip = "Not yet supported")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementInGenericTypeArgument()
        => VerifyItemExistsAsync("""
            class Test
            {
                void Do()
                {
                    System.Func<(System.Action $$
                }
            }
            """, "action");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementInvocationInsideTuple()
        => VerifyNoItemsExistAsync("""
            class Test
            {
                void Do()
                {
                        int M(int i1, int i2) => i1;
                        var t=(e1: 1, e2: M(1, $$));
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementIncompleteParenthesizedTuple()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;
            class Person { }
            class Test
            {
                void Do()
                {
                    (List<Person> $$
                }
            }
            """, "people");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementIncompleteParenthesizedTuple_PredefinedType()
        => VerifyNoItemsExistAsync("""
            class Test
            {
                void Do()
                {
                    (int $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementIncompleteParenthesizedTuple_QualifiedName()
        => VerifyItemExistsAsync("""
            class Test
            {
                void Do()
                {
                    (System.Action $$
                }
            }
            """, "action");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementIncompleteParenthesizedTuple_WithCloseParen()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;
            class Person { }
            class Test
            {
                void Do()
                {
                    (List<Person> $$)
                }
            }
            """, "people");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementIncompleteParenthesizedTuple_PredefinedType_WithCloseParen()
        => VerifyNoItemsExistAsync("""
            class Test
            {
                void Do()
                {
                    (int $$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementIncompleteParenthesizedTuple_QualifiedName_WithCloseParen()
        => VerifyItemExistsAsync("""
            class Test
            {
                void Do()
                {
                    (System.Action $$)
                }
            }
            """, "action");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementIncompleteParenthesizedTuple_FullyQualifiedGenericType()
        => VerifyItemExistsAsync("""
            class Person { }
            class Test
            {
                void Do()
                {
                    (System.Collections.Generic.List<Person> $$
                }
            }
            """, "people");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementIncompleteParenthesizedTuple_FullyQualifiedGenericType_WithCloseParen()
        => VerifyItemExistsAsync("""
            class Person { }
            class Test
            {
                void Do()
                {
                    (System.Collections.Generic.List<Person> $$)
                }
            }
            """, "people");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementIncompleteParenthesizedTuple_ArrayType()
        => VerifyItemExistsAsync("""
            class Person { }
            class Test
            {
                void Do()
                {
                    (Person[] $$
                }
            }
            """, "people");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22342")]
    public Task TupleElementIncompleteParenthesizedTuple_ArrayType_WithCloseParen()
        => VerifyItemExistsAsync("""
            class Person { }
            class Test
            {
                void Do()
                {
                    (Person[] $$)
                }
            }
            """, "people");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17987")]
    public Task Pluralize1()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;
            class Index
            {
                IEnumerable<Index> $$
            }
            """, "Indices");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17987")]
    public Task Pluralize2()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;
            class Test
            {
                IEnumerable<IEnumerable<Test>> $$
            }
            """, "tests");

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
    public Task PluralizeValidGetEnumerator()
        => VerifyItemExistsAsync("""
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
            """, "myClasses");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37366")]
    public Task PluralizeValidGetAsyncEnumerator()
        => VerifyItemExistsAsync("""
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
            """, "myClasses");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37366")]
    public Task PluralizeForUnimplementedIEnumerable()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            class MyClass
            {
                public void M(MyOwnCollection<MyClass> $$) { }
            }


            class MyOwnCollection<T> : IEnumerable<T>
            {
            }
            """, "myClasses");

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
    public Task InUsingStatement1()
        => VerifyItemExistsAsync("""
            using System.IO;

            class C
            {
                void M()
                {
                    using (StreamReader s$$
                }
            }
            """, "streamReader");

    [Fact]
    public Task InUsingStatement2()
        => VerifyItemExistsAsync("""
            using System.IO;

            class C
            {
                void M()
                {
                    using (StreamReader s1, $$
                }
            }
            """, "streamReader");

    [Fact]
    public Task InUsingStatement_Var()
        => VerifyItemExistsAsync("""
            using System.IO;

            class C
            {
                void M()
                {
                    using (var m$$ = new MemoryStream())
                }
            }
            """, "memoryStream");

    [Fact]
    public Task InForStatement1()
        => VerifyItemExistsAsync("""
            using System.IO;

            class C
            {
                void M()
                {
                    for (StreamReader s$$
                }
            }
            """, "streamReader");

    [Fact]
    public Task InForStatement2()
        => VerifyItemIsAbsentAsync("""
            using System.IO;

            class C
            {
                void M()
                {
                    for (StreamReader s1, $$
                }
            }
            """, "streamReader");

    [Fact]
    public Task InForStatement_Var()
        => VerifyItemExistsAsync("""
            using System.IO;

            class C
            {
                void M()
                {
                    for (var m$$ = new MemoryStream();
                }
            }
            """, "memoryStream");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26021")]
    public Task InForEachStatement()
        => VerifyItemExistsAsync("""
            using System.IO;

            class C
            {
                void M()
                {
                    foreach (StreamReader $$
                }
            }
            """, "streamReader");

    [Fact]
    public Task InForEachStatement_Var()
        => VerifyItemExistsAsync("""
            using System.IO;

            class C
            {
                void M()
                {
                    foreach (var m$$ in new[] { new MemoryStream() })
                }
            }
            """, "memoryStream");

    [Fact]
    public async Task DisabledByOption()
    {
        ShowNameSuggestions = false;
        await VerifyNoItemsExistAsync("""
            class Test
            {
                Test $$
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
    public Task TypeImplementsIEnumerableOfType()
        => VerifyItemExistsAsync("""
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
            """, "container");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
    public Task TypeImplementsIEnumerableOfType2()
        => VerifyItemExistsAsync("""
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
            """, "container");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
    public Task TypeImplementsIEnumerableOfType3()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            public class Class1
            {
              public void Method()
              {
                 Container $$
              }
            }

            public class Container : IEnumerable<Container> { }
            """, "container");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
    public Task TypeImplementsIEnumerableOfType4()
        => VerifyItemExistsAsync("""
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
            """, "taskType");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
    public Task TypeImplementsTaskOfType()
        => VerifyItemExistsAsync("""
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
            """, "container");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
    public Task TypeImplementsTaskOfType2()
        => VerifyItemExistsAsync("""
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
            """, "container");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
    public Task TypeImplementsTaskOfType3()
        => VerifyItemExistsAsync("""
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
            """, "taskTypes");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23590")]
    public Task TypeIsNullableOfNullable()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            public class Class1
            {
              public void Method()
              {
                  // This code isn't legal, but we want to ensure we don't crash in this broken code scenario
                  IEnumerable<Nullable<int?>> $$
              }
            }
            """, "nullables");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
    public Task TypeIsNullableStructInLocalWithNullableTypeName()
        => VerifyItemExistsAsync("""
            using System;

            public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> { }

            public class Class1
            {
              public void Method()
              {
                  Nullable<ImmutableArray<int>> $$
              }
            }
            """, "ints");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
    public Task TypeIsNullableStructInLocalWithQuestionMark()
        => VerifyItemExistsAsync("""
            using System.Collections.Immutable;

            public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> { }

            public class Class1
            {
              public void Method()
              {
                  ImmutableArray<int>? $$
              }
            }
            """, "ints");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
    public Task TypeIsNullableReferenceInLocal()
        => VerifyItemExistsAsync("""
            #nullable enable

            using System.Collections.Generic;

            public class Class1
            {
              public void Method()
              {
                  IEnumerable<int>? $$
              }
            }
            """, "ints");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
    public Task TypeIsNullableStructInParameterWithNullableTypeName()
        => VerifyItemExistsAsync("""
            using System;

            public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> { }

            public class Class1
            {
              public void Method(Nullable<ImmutableArray<int>> $$)
              {
              }
            }
            """, "ints");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
    public Task TypeIsNullableStructInParameterWithQuestionMark()
        => VerifyItemExistsAsync("""
            public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> { }

            public class Class1
            {
              public void Method(ImmutableArray<int>? $$)
              {
              }
            }
            """, "ints");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    [WorkItem("https://developercommunity2.visualstudio.com/t/Regression-from-1675-Suggested-varia/1220195")]
    public Task TypeIsNullableReferenceInParameter()
        => VerifyItemExistsAsync("""
            #nullable enable

            using System.Collections.Generic;

            public class Class1
            {
              public void Method(IEnumerable<int>? $$)
              {
              }
            }
            """, "ints");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    public Task EnumerableParameterOfUnmanagedType()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            public class Class1
            {
              public void Method(IEnumerable<int> $$)
              {
              }
            }
            """, "ints");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    public Task EnumerableParameterOfObject()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            public class Class1
            {
              public void Method(IEnumerable<object> $$)
              {
              }
            }
            """, "objects");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    public Task EnumerableParameterOfString()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            public class Class1
            {
              public void Method(IEnumerable<string> $$)
              {
              }
            }
            """, "strings");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    public Task EnumerableGenericTParameter()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            public class Class1
            {
              public void Method<T>(IEnumerable<T> $$)
              {
              }
            }
            """, "values");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    public Task EnumerableGenericTNameParameter()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            public class Class1
            {
              public void Method<TResult>(IEnumerable<TResult> $$)
              {
              }
            }
            """, "results");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    public Task EnumerableGenericUnexpectedlyNamedParameter()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            public class Class1
            {
              public void Method<Arg>(IEnumerable<Arg> $$)
              {
              }
            }
            """, "args");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36364")]
    public Task EnumerableGenericUnexpectedlyNamedParameterBeginsWithT()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            public class Class1
            {
              public void Method<Type>(IEnumerable<Type> $$)
              {
              }
            }
            """, "types");

    [Fact]
    public async Task CustomNamingStyleInsideClass()
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        var workspace = workspaceFixture.Target.GetWorkspace(GetComposition());

        workspace.SetAnalyzerFallbackOptions(new OptionsCollection(LanguageNames.CSharp)
        {
            { NamingStyleOptions.NamingPreferences, NamesEndWithSuffixPreferences() }
        });

        var markup = """
            class Configuration
            {
                Configuration $$
            }
            """;
        await VerifyItemExistsAsync(markup, "ConfigurationField", glyph: Glyph.FieldPublic,
            expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        await VerifyItemExistsAsync(markup, "ConfigurationProperty", glyph: Glyph.PropertyPublic,
            expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        await VerifyItemExistsAsync(markup, "ConfigurationMethod", glyph: Glyph.MethodPublic,
            expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        await VerifyItemIsAbsentAsync(markup, "ConfigurationLocal");
        await VerifyItemIsAbsentAsync(markup, "ConfigurationLocalFunction");
    }

    [Fact]
    public async Task CustomNamingStyleInsideMethod()
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        var workspace = workspaceFixture.Target.GetWorkspace(GetComposition());

        workspace.SetAnalyzerFallbackOptions(new OptionsCollection(LanguageNames.CSharp)
        {
            { NamingStyleOptions.NamingPreferences, NamesEndWithSuffixPreferences() }
        });

        var markup = """
            class Configuration
            {
                void M()
                {
                    Configuration $$
                }
            }
            """;
        await VerifyItemExistsAsync(markup, "ConfigurationLocal", glyph: Glyph.Local,
            expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        await VerifyItemExistsAsync(markup, "ConfigurationLocalFunction", glyph: Glyph.MethodPublic,
            expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
        await VerifyItemIsAbsentAsync(markup, "ConfigurationField");
        await VerifyItemIsAbsentAsync(markup, "ConfigurationMethod");
        await VerifyItemIsAbsentAsync(markup, "ConfigurationProperty");
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
        await VerifyItemExistsAsync(markup, "classB1", glyph: Glyph.Local,
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
        await VerifyItemExistsAsync(markup, "classB1", glyph: Glyph.Local,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
    public Task TestCompletionCanUsePropertyName()
        => VerifyItemExistsAsync("""
            class ClassA
            {
                class ClassB { }

                ClassB classB { get; set; }

                void M()
                {
                    ClassB $$
                }
            }
            """, "classB", glyph: Glyph.Local,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
    public Task TestCompletionCanUseFieldName()
        => VerifyItemExistsAsync("""
            class ClassA
            {
                class ClassB { }

                ClassB classB;

                void M()
                {
                    ClassB $$
                }
            }
            """, "classB", glyph: Glyph.Local,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);

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
        await VerifyItemExistsAsync(markup, "classB1", glyph: Glyph.Local,
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
        await VerifyItemExistsAsync(markup, "classB2", glyph: Glyph.Local,
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
        await VerifyItemExistsAsync(markup, "classB1", glyph: Glyph.Local,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
    public Task TestCompletionCanUseClassName()
        => VerifyItemExistsAsync("""
            class classA
            {
                void M()
                {
                    classA $$
                }
            }
            """, "classA", glyph: Glyph.Local,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31304")]
    public Task TestCompletionCanUseLocalInDifferentScope()
        => VerifyItemExistsAsync("""
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
            """, "classB", glyph: Glyph.Local,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);

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
            await VerifyItemExistsAsync(markup, "classB", glyph: Glyph.Parameter,
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
    public Task TestCompletionDoesNotUseLocalInNestedLocalFunction()
        => VerifyItemIsAbsentAsync("""
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
            """, "classB");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
    public Task TestCompletionDoesNotUseLocalFunctionParameterInNestedLocalFunction()
        => VerifyItemIsAbsentAsync("""
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
            """, "classB");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
    public Task TestCompletionCanUseLocalFunctionParameterAsParameter()
        => VerifyItemExistsAsync("""
            class ClassA
            {
                class ClassB { }
                void M()
                {
                    void LocalM1(ClassB classB) { }
                    void LocalM2(ClassB $$) { }
                }
            }
            """, "classB", glyph: Glyph.Parameter,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
    public Task TestCompletionCanUseLocalFunctionVariableAsParameter()
        => VerifyItemExistsAsync("""
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
            """, "classB", glyph: Glyph.Parameter,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
    public Task TestCompletionCanUseLocalFunctionParameterAsVariable()
        => VerifyItemExistsAsync("""
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
            """, "classB", glyph: Glyph.Local,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35891")]
    public Task TestCompletionCanUseLocalFunctionVariableAsVariable()
        => VerifyItemExistsAsync("""
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
            """, "classB", glyph: Glyph.Local,
                expectedDescriptionOrNull: CSharpFeaturesResources.Suggested_name);

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

        workspace.SetAnalyzerFallbackOptions(new OptionsCollection(LanguageNames.CSharp)
        {
            { NamingStyleOptions.NamingPreferences, MultipleCamelCaseLocalRules() }
        });
        await VerifyItemExistsAsync("""
            public class MyClass
            {
                void M()
                {
                    MyClass myClass;
                    MyClass $$
                }
            }
            """, "myClass1", glyph: Glyph.Local);
    }

    [Fact]
    public Task TestNotForNonTypeSymbol()
        => VerifyItemIsAbsentAsync("""
            using System;
            class C
            {
                Console.BackgroundColor $$
            }
            """, "consoleColor");

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
    public Task TestForOutParam2()
        => VerifyItemExistsAsync("""
            class C
            {
                void Main()
                {
                    int.TryParse("", out var $$)
                }
            }
            """, "result");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49791")]
    public Task TestForErrorType1()
        => VerifyNoItemsExistAsync("""
            class C
            {
                void Main(string _rootPath)
                {
                    _rootPath $$
                    _rootPath = null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49791")]
    public Task TestForErrorType2()
        => VerifyItemExistsAsync("""
            class C
            {
                void Main()
                {
                    Goo $$
                    Goo = null;
                }
            }
            """, "goo");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36352")]
    public Task InferCollectionInErrorCase1()
        => VerifyItemExistsAsync("""
            class Customer { }

            class V
            {
                void M(IEnumerable<Customer> $$)
                {
                }
            }
            """, "customers");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63943")]
    public Task InferOffOfGenericNameInPattern()
        => VerifyItemExistsAsync("""
            using System.Collections.Generic;

            class Customer { }

            class V
            {
                void M(object o)
                {
                    if (o is List<Customer> $$
                }
            }
            """, "customers");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79417")]
    public Task TestNestedParameter1()
        => VerifyItemExistsAsync("""
            class C
            {
                void M(MyWidget myWidget)
                {
                    void LocalFunction(MyWidget $$) { }
                }
            }
            """, "myWidget");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79417")]
    public Task TestNestedParameter2()
        => VerifyItemExistsAsync("""
            class MyWidget { }
            class C(MyWidget myWidget)
            {
                class D(MyWidget $$) { }
            }
            """, "myWidget");

#if  false

#endif

    private static NamingStylePreferences MultipleCamelCaseLocalRules()
    {
        var styles = new[]
        {
            SpecificationStyle(new SymbolKindOrTypeKind(SymbolKind.Local), name: "Local1"),
            SpecificationStyle(new SymbolKindOrTypeKind(SymbolKind.Local), name: "Local1"),
        };

        return new NamingStylePreferences(
            [.. styles.Select(t => t.specification)],
            [.. styles.Select(t => t.style)],
            [.. styles.Select(t => new NamingRule(t.specification, t.style, ReportDiagnostic.Error))]);

        // Local functions

        static (SymbolSpecification specification, NamingStyle style) SpecificationStyle(SymbolKindOrTypeKind kind, string name)
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                name,
                [kind]);

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
            [.. specificationStyles.Select(t => t.specification)],
            [.. specificationStyles.Select(t => t.style)],
            [.. specificationStyles.Select(t => new NamingRule(t.specification, t.style, ReportDiagnostic.Error))]);

        // Local functions

        static (SymbolSpecification specification, NamingStyle style) SpecificationStyle(SymbolKindOrTypeKind kind, string suffix)
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                name: suffix,
                [kind],
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
                [new SymbolKindOrTypeKind(SymbolKind.Parameter)],
                accessibilityList: default,
                modifiers: default),
            new SymbolSpecification(
                id: Guid.NewGuid(),
                name: "fallback",
                [new SymbolKindOrTypeKind(SymbolKind.Parameter), new SymbolKindOrTypeKind(SymbolKind.Local)],
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
            namingRules: [new NamingRule(symbolSpecifications[0], namingStyles[0], ReportDiagnostic.Error), new NamingRule(symbolSpecifications[1], namingStyles[1], ReportDiagnostic.Error)]);
    }
}
