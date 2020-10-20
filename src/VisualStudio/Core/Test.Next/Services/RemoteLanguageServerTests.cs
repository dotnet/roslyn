// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Services
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.RemoteHost)]
    public class RemoteLanguageServerTests
    {
        private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeatures.WithTestHostParts(TestHost.OutOfProcess);

        [Fact]
        public async Task CSharpLanguageServiceTest()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code, composition: s_composition);

            var results = await GetVsSearchResultsAsync(workspace, "met");

            Assert.Equal("Method", Assert.Single(results).Name);
        }

        [Fact]
        public async Task CSharpLanguageServiceTest_Streaming()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code, composition: s_composition);

            using var progress = BufferedProgress.Create<SymbolInformation>(null);

            var results = await GetVsSearchResultsAsync(workspace, "met", progress);

            Assert.Null(results);

            results = progress.GetValues().ToArray();

            Assert.Equal("Method", Assert.Single(results).Name);
        }

        [Fact]
        public async Task CSharpLanguageServiceTest_MultipleResults()
        {
            var code = @"class Test 
{ 
    void Method() { } 
    void Method2() { }
    void method3() { }

    int methodProperty { get; }
}";

            using var workspace = TestWorkspace.CreateCSharp(code, composition: s_composition);
            var results = await GetVsSearchResultsAsync(workspace, "met");

            Assert.Equal(4, results.Length);
        }

        [Fact]
        public async Task VisualBasicLanguageServiceTest()
        {
            var code = @"Class Test
    Sub Method()
    End Sub
End Class";

            using var workspace = TestWorkspace.CreateVisualBasic(code, composition: s_composition);

            var results = await GetVsSearchResultsAsync(workspace, "met");

            Assert.Equal("Method", Assert.Single(results).Name);
        }

        private async Task<SymbolInformation[]> GetVsSearchResultsAsync(TestWorkspace workspace, string query, IProgress<SymbolInformation[]> progress = null)
        {
            var solution = workspace.CurrentSolution;

            using var client = await RemoteHostClient.TryGetClientAsync(workspace, CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(client);

            var document = solution.Projects.First().Documents.First();
            await UpdatePrimaryWorkspace(client, solution.WithDocumentFilePath(document.Id, Path.Combine(TempRoot.Root, document.FilePath)));

            var workspaceSymbolParams = new WorkspaceSymbolParams
            {
                Query = query,
            };

            workspaceSymbolParams.PartialResultToken = progress;

            var result = await client.RunRemoteAsync<JObject>(
                WellKnownServiceHubService.RemoteLanguageServer,
                Methods.InitializeName,
                solution: null,
                new object[] { new InitializeParams() },
                callbackTarget: null,
                CancellationToken.None).ConfigureAwait(false);

            Assert.True(result["capabilities"]["workspaceSymbolProvider"].ToObject<bool>());

            return await client.RunRemoteAsync<SymbolInformation[]>(
                WellKnownServiceHubService.RemoteLanguageServer,
                Methods.WorkspaceSymbolName,
                solution: null,
                new object[] { workspaceSymbolParams },
                callbackTarget: null,
                CancellationToken.None).ConfigureAwait(false);
        }

        // make sure we always move remote workspace forward
        private int _solutionVersion = 0;

        private async Task UpdatePrimaryWorkspace(RemoteHostClient client, Solution solution)
        {
            var checksum = await solution.State.GetChecksumAsync(CancellationToken.None);
            await client.TryInvokeAsync<IRemoteAssetSynchronizationService>(
                solution,
                async (service, solutionInfo, cancellationToken) => await service.SynchronizePrimaryWorkspaceAsync(solutionInfo, checksum, _solutionVersion++, cancellationToken),
                CancellationToken.None);
        }
    }
}
