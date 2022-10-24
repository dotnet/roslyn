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

        private static ImmutableArray<string> s_kinds = ImmutableArray.Create(
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
        public async Task UpdaterService()
        {
            using var workspace = CreateWorkspace();

            var exportProvider = workspace.Services.SolutionServices.ExportProvider;
            var listenerProvider = exportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

            var service = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();

            // add solution, change document
            workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));
            var project = workspace.AddProject("proj", LanguageNames.CSharp);
            var document = workspace.AddDocument(project.Id, "doc.cs", SourceText.From(""));

            var tasks = new List<Task>();

            for (var i = 0; i < 1000; i++)
            {
                var name = "Goo" + i;
                var forked = document.Project.Solution.WithDocumentText(document.Id, SourceText.Create(CreateText(name)));

                tasks.Add(Task.Run(async () =>
                {
                    // make sure client is ready
                    using var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);
                    var stream = client.TryInvokeStreamAsync<IRemoteNavigateToSearchService, RoslynNavigateToItem>(
                        forked,
                        (service, checksum, _) => service.SearchProjectAsync(checksum, project.Id, ImmutableArray<DocumentId>.Empty, name, s_kinds, CancellationToken.None),
                        CancellationToken.None);

                    var count = 0;
                    await foreach (var result in stream)
                        count++;

                    Assert.True(count >= 100);
                }));
            }

            await Task.WhenAll(tasks);
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
