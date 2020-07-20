// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Services
{
    [UseExportProvider]
    public class LanguageServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task CSharpLanguageServiceTest()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);

            var results = await GetVsSearchResultsAsync(workspace, "met");

            Assert.Equal("Method", Assert.Single(results).Name);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task CSharpLanguageServiceTest_MultipleResults()
        {
            var code = @"class Test 
{ 
    void Method() { } 
    void Method2() { }
    void method3() { }

    int methodProperty { get; }
}";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var results = await GetVsSearchResultsAsync(workspace, "met");

            Assert.Equal(4, results.Length);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task VisualBasicLanguageServiceTest()
        {
            var code = @"Class Test
    Sub Method()
    End Sub
End Class";

            using var workspace = TestWorkspace.CreateVisualBasic(code);

            var results = await GetVsSearchResultsAsync(workspace, "met");

            Assert.Equal("Method", Assert.Single(results).Name);
        }

        private async Task<ImmutableArray<SymbolInformation>> GetVsSearchResultsAsync(TestWorkspace workspace, string query)
        {
            var solution = workspace.CurrentSolution;
            var client = (InProcRemoteHostClient)await InProcRemoteHostClient.CreateAsync(workspace.Services, runCacheCleanup: false);

            var document = solution.Projects.First().Documents.First();
            await UpdatePrimaryWorkspace(client, solution.WithDocumentFilePath(document.Id, Path.Combine(TempRoot.Root, document.FilePath)));

            var workspaceSymbolParams = new WorkspaceSymbolParams
            {
                Query = query,
            };

            var symbolResultsBuilder = ArrayBuilder<SymbolInformation>.GetInstance();
            var threadingContext = workspace.ExportProvider.GetExportedValue<IThreadingContext>();
            var awaitableProgress = new ProgressWithCompletion<SymbolInformation[]>(symbols =>
            {
                symbolResultsBuilder.AddRange(symbols);
            }, threadingContext.JoinableTaskFactory);
            workspaceSymbolParams.PartialResultToken = awaitableProgress;

            using (var jsonRpc = JsonRpc.Attach(await client.RequestServiceAsync(WellKnownServiceHubService.LanguageServer)))
            {
                var result = await jsonRpc.InvokeWithCancellationAsync<JObject>(
                    Methods.InitializeName,
                    new object[] { new InitializeParams() },
                    CancellationToken.None);

                Assert.True(result["capabilities"]["workspaceSymbolProvider"].ToObject<bool>());

                var symbolResult = await jsonRpc.InvokeWithCancellationAsync<SymbolInformation[]>(
                    Methods.WorkspaceSymbolName,
                    new object[] { workspaceSymbolParams },
                    CancellationToken.None);

                await awaitableProgress.WaitAsync(CancellationToken.None);
            }

            return symbolResultsBuilder.ToImmutableAndFree();
        }

        // make sure we always move remote workspace forward
        private int _solutionVersion = 0;

        private async Task UpdatePrimaryWorkspace(InProcRemoteHostClient client, Solution solution)
        {
            await client.RunRemoteAsync(
                WellKnownServiceHubService.RemoteHost,
                nameof(IRemoteHostService.SynchronizePrimaryWorkspaceAsync),
                solution,
                new object[] { await solution.State.GetChecksumAsync(CancellationToken.None), _solutionVersion++ },
                callbackTarget: null,
                CancellationToken.None);
        }
    }
}
