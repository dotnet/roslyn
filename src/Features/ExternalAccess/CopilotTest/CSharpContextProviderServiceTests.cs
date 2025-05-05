// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Completion;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Copilot.Roslyn.Tests;

#pragma warning disable CS0618 // Type or member is obsolete

[UseExportProvider]
public class CSharpContextProviderServiceTests
{
    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> asyncEnumerable)
    {
        var builder = new List<T>();
        await foreach (var item in asyncEnumerable.ConfigureAwait(false))
        {
            builder.Add(item);
        }

        return builder;
    }

    [Fact]
    public async Task TestGetContextItems()
    {
        var solution = CreateSolution();
        var document = solution.Projects.First().Documents.Single();

        var providers = new List<IContextProvider>();
        var total = 0;
        for (var i = 1; i < 10; ++i)
        {
            providers.Add(new TestTraitProvider(total, i));
            total += i;
        }

        var service = new CSharpContextProviderService(providers);

        var items = (await ToListAsync(service.GetContextItemsAsync(document, 0, ImmutableDictionary<string, object>.Empty, CancellationToken.None)).ConfigureAwait(false)).OrderBy(GetValueAsInteger).ToImmutableArray();
        Assert.Equal(total, items.Length);

        for (var i = 0; i < items.Length; ++i)
        {
            Assert.Equal(i, GetValueAsInteger(items[i]));
        }
#pragma warning restore IDE0007 // Use implicit type
    }

    [Fact]
    public async Task TestProvidersThrowException()
    {
        var solution = CreateSolution();
        var document = solution.Projects.First().Documents.Single();

        var providers = new List<IContextProvider>()
        {
            new TestExceptionProvider1(),
            new TestExceptionProvider2(),
            new TestTraitProvider(0, 1),
            new TestTraitProvider(1, 1),
            new TestTraitProvider(2, 1),
        };

        var service = new CSharpContextProviderService(providers);

        var items = (await ToListAsync(service.GetContextItemsAsync(document, 0, ImmutableDictionary<string, object>.Empty, CancellationToken.None)).ConfigureAwait(false)).OrderBy(GetValueAsInteger).ToImmutableArray();
        Assert.Equal(3, items.Length);

        for (var i = 0; i < items.Length; ++i)
        {
            Assert.Equal(i, GetValueAsInteger(items[i]));
        }
    }

    [Fact]
    public async Task TestPartialResultsWithCancellation()
    {
        var solution = CreateSolution();
        var document = solution.Projects.First().Documents.Single();

        var providerCount = 3;
        var semaphore = new SemaphoreSlim(0);
        var providers = Enumerable.Range(0, providerCount).Select(i => new TestProviderWithWait(i)).ToImmutableArray();
        var service = new CSharpContextProviderService(providers);

        var cancellationTokenSource = new CancellationTokenSource();

        var items = new List<IContextItem>();
        var cancelled = false;
        try
        {
            // we should stream back as many items as each individual provider provides by the time the request is cancelled
            await foreach (var item in service.GetContextItemsAsync(document, 0, ImmutableDictionary<string, object>.Empty, cancellationTokenSource.Token).ConfigureAwait(false))
            {
                items.Add(item);
                if (items.Count == providerCount)
                {
                    cancellationTokenSource.Cancel();
                    foreach (var provider in providers)
                    {
                        provider.WaitSemaphore.Release();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }

        Assert.True(cancelled);
        Assert.Equal(providerCount, items.Count);
        var orderedItems = items.OrderBy(GetValueAsInteger).ToList();

        for (var i = 0; i < orderedItems.Count; ++i)
        {
            Assert.Equal(i, GetValueAsInteger(orderedItems[i]));
        }
    }

    private class TestProviderWithWait : IContextProvider
    {
        private readonly string i;

        public TestProviderWithWait(int i)
        {
            this.i = i.ToString();
        }

        public SemaphoreSlim WaitSemaphore { get; } = new SemaphoreSlim(0);

        public async ValueTask ProvideContextItemsAsync(Document document, int position, IReadOnlyDictionary<string, object> activeExperiments, Func<ImmutableArray<IContextItem>, CancellationToken, ValueTask> callback, CancellationToken cancellationToken)
        {
            await callback([new TraitItem(this.i, this.i)], cancellationToken);

            await this.WaitSemaphore.WaitAsync(cancellationToken);

            await callback([new TraitItem(this.i, this.i)], cancellationToken);
            await callback([new TraitItem(this.i, this.i)], cancellationToken);
        }
    }

    private class TestExceptionProvider1 : IContextProvider
    {
        public ValueTask ProvideContextItemsAsync(Document document, int position, IReadOnlyDictionary<string, object> activeExperiments, Func<ImmutableArray<IContextItem>, CancellationToken, ValueTask> callback, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private class TestExceptionProvider2 : IContextProvider
    {
        public async ValueTask ProvideContextItemsAsync(Document document, int position, IReadOnlyDictionary<string, object> activeExperiments, Func<ImmutableArray<IContextItem>, CancellationToken, ValueTask> callback, CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new NotImplementedException();
        }
    }

    private abstract class AbstractTestProvider : IContextProvider
    {
        public int Start { get; }

        public int Count { get; }

        public AbstractTestProvider(int start, int count)
        {
            this.Start = start;
            this.Count = count;
        }

        protected abstract IContextItem CreateItem(int value);

        public async ValueTask ProvideContextItemsAsync(Document document, int position, IReadOnlyDictionary<string, object> activeExperiments, Func<ImmutableArray<IContextItem>, CancellationToken, ValueTask> callback, CancellationToken cancellationToken)
        {
            for (var i = 0; i < this.Count; i++)
            {
                var value = this.Start + i;
                await callback([this.CreateItem(value)], cancellationToken);
            }
        }
    }

    private class TestTraitProvider : AbstractTestProvider
    {
        public TestTraitProvider(int start, int count)
            : base(start, count)
        {
        }

        protected override IContextItem CreateItem(int value)
        {
            return new TraitItem(value.ToString(), value.ToString());
        }
    }

    private class TestCodeSnippetProvider : AbstractTestProvider
    {
        public TestCodeSnippetProvider(int start, int count)
            : base(start, count)
        {
        }

        protected override IContextItem CreateItem(int value)
        {
            return new CodeSnippetItem(value.ToString(), value.ToString());
        }
    }

    private static int GetValueAsInteger(IContextItem x)
    {
        return int.Parse(x is TraitItem traitItem1 ? traitItem1.Value : ((CodeSnippetItem)x).Value);
    }

    private static Solution CreateSolution()
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        using var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "ProjectName", "AssemblyName", LanguageNames.CSharp)
            .AddDocument(documentId, "DocumentName", "");

        return solution;
    }
}
