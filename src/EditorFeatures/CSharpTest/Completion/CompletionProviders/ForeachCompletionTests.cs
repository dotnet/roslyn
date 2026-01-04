// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class ForeachCompletionTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(SymbolCompletionProvider);

    private const string AsyncEnumerableMarkup = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;

        namespace System.Runtime.CompilerServices
        {
            public struct ValueTaskAwaiter<T> : INotifyCompletion
            {
                public bool IsCompleted => throw null;
                public T GetResult() => throw null;
                public void OnCompleted(Action continuation) => throw null;
            }

            public interface INotifyCompletion
            {
                void OnCompleted(Action continuation);
            }
        }

        namespace System.Threading.Tasks
        {
            public struct ValueTask<T>
            {
                public System.Runtime.CompilerServices.ValueTaskAwaiter<T> GetAwaiter() => throw null;
            }
        }

        namespace System.Collections.Generic
        {
            public interface IAsyncEnumerable<out T>
            {
                IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default);
            }

            public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
            {
                ValueTask<bool> MoveNextAsync();
                T Current { get; }
            }
        }

        namespace System
        {
            public interface IAsyncDisposable
            {
                ValueTask<bool> DisposeAsync();
            }
        }
        """;

    [Fact]
    public async Task PreselectIEnumerablePropertyInForeach_Var()
    {
        const string Markup = """
            using System.Collections.Generic;
            public class C
            {
                public object Prop { get; set; }
                public IEnumerable<int> PropWithEnumerable { get; set; }

                public void M(C c)
                {
                    foreach (var x in c.$$
                }
            }
            """;
        await VerifyItemExistsAsync(Markup, "PropWithEnumerable", matchPriority: SymbolMatchPriority.PreferFieldOrProperty);
    }

    [Fact]
    public async Task PreselectIEnumerablePropertyInForeach_SpecifiedType()
    {
        const string Markup = """
            using System.Collections.Generic;
            public class C
            {
                public object Prop { get; set; }
                public IEnumerable<int> PropWithEnumerable { get; set; }
                public IEnumerable<string> PropWithStringEnumerable { get; set; }

                public void M(C c)
                {
                    foreach (int x in c.$$
                }
            }
            """;
        await VerifyItemExistsAsync(Markup, "PropWithEnumerable", matchPriority: SymbolMatchPriority.PreferFieldOrProperty);
        await VerifyItemExistsAsync(Markup, "PropWithStringEnumerable", matchPriority: null);
    }

    [Fact]
    public async Task PreselectIAsyncEnumerablePropertyInAwaitForEach_Var_ReferenceType()
    {
        const string Markup = AsyncEnumerableMarkup + """
            public class C
            {
                public object Prop { get; set; }
                public IAsyncEnumerable<string> PropWithAsyncEnumerable { get; set; }

                public async Task M(C c)
                {
                    await foreach (var x in c.$$
                }
            }
            """;
        await VerifyItemExistsAsync(Markup, "PropWithAsyncEnumerable", matchPriority: SymbolMatchPriority.PreferFieldOrProperty);
    }

    [Fact]
    public async Task PreselectIAsyncEnumerablePropertyInAwaitForEach_Var_ValueType()
    {
        const string Markup = AsyncEnumerableMarkup + """
            public class C
            {
                public object Prop { get; set; }
                public IAsyncEnumerable<int> PropWithAsyncEnumerable { get; set; }

                public async Task M(C c)
                {
                    await foreach (var x in c.$$
                }
            }
            """;
        await VerifyItemExistsAsync(Markup, "PropWithAsyncEnumerable", matchPriority: SymbolMatchPriority.PreferFieldOrProperty);
    }

    [Fact]
    public async Task PreselectIAsyncEnumerablePropertyInAwaitForEach_SpecifiedType()
    {
        const string Markup = AsyncEnumerableMarkup + """
            public class C
            {
                public object Prop { get; set; }
                public IAsyncEnumerable<int> PropWithAsyncEnumerable { get; set; }

                public async Task M(C c)
                {
                    await foreach (int x in c.$$
                }
            }
            """;
        await VerifyItemExistsAsync(Markup, "PropWithAsyncEnumerable", matchPriority: SymbolMatchPriority.PreferFieldOrProperty);
    }

    [Fact]
    public async Task PreselectSubtypeInAssignment()
    {
        const string Markup = """
            using System.Collections.Generic;
            public class C
            {
                public List<int> PropWithList { get; set; }

                public void M(C c)
                {
                    IEnumerable<int> x = c.$$
                }
            }
            """;
        await VerifyItemExistsAsync(Markup, "PropWithList", matchPriority: SymbolMatchPriority.PreferFieldOrProperty);
    }
}
