// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Remote
{
    internal class LanguageServer : ServiceBase
    {
        private static readonly IImmutableSet<string> s_supportedKinds =
            ImmutableHashSet.Create(
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

        public LanguageServer(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            StartService();
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public Task<InitializeResult> InitializeAsync(JToken input, CancellationToken cancellationToken)
        {
            return Task.FromResult(new InitializeResult()
            {
                Capabilities = new VSServerCapabilities()
                {
                    WorkspaceStreamingSymbolProvider = true
                }
            });
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public Task InitializedAsync()
        {
            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public void Shutdown(CancellationToken cancellationToken)
        {
            // our language server shutdown when VS shutdown
            // we have this so that we don't get log file every time VS shutdown
        }

        [JsonRpcMethod(Methods.ExitName)]
        public void Exit()
        {
            // our language server exit when VS shutdown
            // we have this so that we don't get log file every time VS shutdown
        }

        [JsonRpcMethod(VSSymbolMethods.WorkspaceBeginSymbolName)]
        public Task<VSBeginSymbolParams> BeginWorkspaceSymbolAsync(string query, int searchId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    // for now, we use whatever solution we have currently. in future, we will add an ability to sync VS's current solution
                    // on demand from OOP side
                    // https://github.com/dotnet/roslyn/issues/37424
                    await SearchAsync(SolutionService.PrimaryWorkspace.CurrentSolution, query, searchId, cancellationToken).ConfigureAwait(false);
                    return new VSBeginSymbolParams();
                }
            }, cancellationToken);
        }

        private async Task SearchAsync(Solution solution, string query, int searchId, CancellationToken cancellationToken)
        {
            var tasks = solution.Projects.Select(p => SearchProjectAsync(p, cancellationToken)).ToArray();
            await Task.WhenAll(tasks).ConfigureAwait(false);
            return;

            async Task SearchProjectAsync(Project project, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var results = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                    project,
                    ImmutableArray<Document>.Empty,
                    query,
                    s_supportedKinds,
                    cancellationToken).ConfigureAwait(false);

                var convertedResults = await ConvertAsync(results, cancellationToken).ConfigureAwait(false);

                await EndPoint.InvokeAsync(
                    VSSymbolMethods.WorkspacePublishSymbolName,
                    new object[] { new VSPublishSymbolParams() { SearchId = searchId, Symbols = convertedResults } },
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<VSSymbolInformation[]> ConvertAsync(
            ImmutableArray<INavigateToSearchResult> results, CancellationToken cancellationToken)
        {
            var symbols = new VSSymbolInformation[results.Length];

            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                var text = await result.NavigableItem.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                symbols[i] = new VSSymbolInformation()
                {
                    Name = result.Name,
                    ContainerName = result.AdditionalInformation,
                    Kind = ProtocolConversions.NavigateToKindToSymbolKind(result.Kind),
                    Location = new LSP.Location()
                    {
                        Uri = result.NavigableItem.Document.GetURI(),
                        Range = ProtocolConversions.TextSpanToRange(result.NavigableItem.SourceSpan, text)
                    },
                    Icon = new VisualStudio.Text.Adornments.ImageElement(result.NavigableItem.Glyph.GetImageId())
                };
            }

            return symbols;
        }
    }
}
