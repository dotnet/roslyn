// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.UnitTests
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.RemoteHost)]
    public class RemoteHostClientServiceFactoryTests
    {
        private static readonly TestComposition s_composition = FeaturesTestCompositions.Features.WithTestHostParts(TestHost.OutOfProcess);

        private static AdhocWorkspace CreateWorkspace()
            => new(s_composition.GetHostServices());

        [Fact]
        public async Task UpdaterService()
        {
            using var workspace = CreateWorkspace();

            var exportProvider = workspace.Services.SolutionServices.ExportProvider;
            var listenerProvider = exportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

            var checksumUpdater = new SolutionChecksumUpdater(workspace, listenerProvider, CancellationToken.None);
            var service = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();

            // make sure client is ready
            using var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            // add solution, change document
            workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));
            var project = workspace.AddProject("proj", LanguageNames.CSharp);
            var document = workspace.AddDocument(project.Id, "doc.cs", SourceText.From("code"));

            var oldText = document.GetTextSynchronously(CancellationToken.None);
            var newText = oldText.WithChanges(new[] { new TextChange(new TextSpan(0, 1), "abc") });
            var newSolution = document.Project.Solution.WithDocumentText(document.Id, newText, PreservationMode.PreserveIdentity);

            workspace.TryApplyChanges(newSolution);

            // wait for listener
            var workspaceListener = listenerProvider.GetWaiter(FeatureAttribute.Workspace);
            await workspaceListener.ExpeditedWaitAsync();

            var listener = listenerProvider.GetWaiter(FeatureAttribute.SolutionChecksumUpdater);
            await listener.ExpeditedWaitAsync();

            // checksum should already exist
            Assert.True(workspace.CurrentSolution.State.TryGetStateChecksums(out _));

            checksumUpdater.Shutdown();
        }

        private static readonly ImmutableArray<string> s_kinds = ImmutableArray.Create(
            NavigateToItemKind.Class,
            NavigateToItemKind.Constant,
            NavigateToItemKind.Delegate,
            NavigateToItemKind.Enum,
            NavigateToItemKind.EnumItem,
            NavigateToItemKind.Event,
            NavigateToItemKind.Field,
            NavigateToItemKind.Interface,
            NavigateToItemKind.Method,
            NavigateToItemKind.Module,
            NavigateToItemKind.Property,
            NavigateToItemKind.Structure);

        [Fact]
        public async Task TestStreamingServices()
        {
            using var workspace = CreateWorkspace();

            var exportProvider = workspace.Services.SolutionServices.ExportProvider;
            var listenerProvider = exportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

            var service = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();
            using var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            // add solution, change document
            workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));
            var project = workspace.AddProject("proj", LanguageNames.CSharp);
            var document = workspace.AddDocument(project.Id, "doc.cs", SourceText.From(""));

            var assetStorage = workspace.Services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;

            var tasks = new List<Task>();

            // Kick off a ton of work in parallel to try to shake out race conditions with pinning/syncing.

            // 100 outer loops that ensure we generate the same files with the same checksums at least 10 times.
            for (var i = 0; i < 100; i++)
            {
                // 100 inner loops producing variants of the file.
                for (var j = 0; j < 100; j++)
                {
                    tasks.Add(Task.Run(async () => await PerformSearchesAsync(client, assetStorage, document, name: "Goo" + i)));
                }
            }

            await Task.WhenAll(tasks);

            Assert.Equal(0, assetStorage.GetTestAccessor().PinnedScopesCount);
        }

        private static async Task PerformSearchesAsync(RemoteHostClient client, SolutionAssetStorage storage, Document document, string name)
        {
            // Fork the document, with 100 variants of methods called 'name' in it.
            var forked = document.Project.Solution.WithDocumentText(document.Id, SourceText.From(CreateText(name)));

            Checksum checksum = null!;

            // Search teh forked document, ensuring we find all the results we expect.
            var stream = client.TryInvokeStreamAsync<IRemoteNavigateToSearchService, RoslynNavigateToItem>(
                forked,
                (service, solutionChecksum, _) =>
                {
                    checksum = solutionChecksum;
                    return service.SearchProjectAsync(solutionChecksum, document.Project.Id, ImmutableArray<DocumentId>.Empty, name, s_kinds, CancellationToken.None);
                },
                CancellationToken.None);

            var count = 0;
            await foreach (var result in stream)
            {
                Assert.Equal(document.Id, result.DocumentId);
                Assert.True(storage.GetTestAccessor().IsPinned(checksum));
                count++;
            }

            Assert.True(count >= 100);
        }

        private static string CreateText(string name)
        {
            using var _ = PooledStringBuilder.GetInstance(out var builder);

            builder.AppendLine("class C");
            builder.AppendLine("{");
            for (var i = 0; i < 100; i++)
                builder.AppendLine($"    public void {name}_{i}() {{ }}");

            builder.AppendLine("}");
            return builder.ToString();
        }

        [Fact]
        public async Task TestSessionWithNoSolution()
        {
            using var workspace = CreateWorkspace();

            var service = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();

            var mock = new MockLogService();
            var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            using var connection = client.CreateConnection<IRemoteSymbolSearchUpdateService>(callbackTarget: mock);
            Assert.True(await connection.TryInvokeAsync(
                (service, callbackId, cancellationToken) => service.UpdateContinuouslyAsync(callbackId, "emptySource", Path.GetTempPath(), cancellationToken),
                CancellationToken.None));
        }

        private class NullAssemblyAnalyzerLoader : IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                // doesn't matter what it returns
                return typeof(object).Assembly;
            }
        }

        private class MockLogService : ISymbolSearchLogService
        {
            public ValueTask LogExceptionAsync(string exception, string text, CancellationToken cancellationToken) => default;
            public ValueTask LogInfoAsync(string text, CancellationToken cancellationToken) => default;
        }
    }
}
