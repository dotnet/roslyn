// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        public Task<InitializeResult> Initialize(JToken input, CancellationToken cancellationToken)
        {
            return Task.FromResult(new InitializeResult()
            {
                Capabilities = new VSServerCapabilities()
                {
                    DisableGoToWorkspaceSymbols = true,
                    WorkspaceSymbolProvider = true,
                }
            });
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public Task Initialized()
        {
            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public void Shutdown(CancellationToken _)
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

        [JsonRpcMethod(Methods.WorkspaceSymbolName, UseSingleObjectParameterDeserialization = true)]
        public Task<SymbolInformation[]> WorkspaceSymbolAsync(WorkspaceSymbolParams args, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    // for now, we use whatever solution we have currently. in future, we will add an ability to sync VS's current solution
                    // on demand from OOP side
                    // https://github.com/dotnet/roslyn/issues/37424
                    var results = await SearchAsync(SolutionService.PrimaryWorkspace.CurrentSolution, args, cancellationToken).ConfigureAwait(false);
                    return results.ToArray();
                }
            }, cancellationToken);
        }

        private async Task<ImmutableArray<SymbolInformation>> SearchAsync(Solution solution, WorkspaceSymbolParams args, CancellationToken cancellationToken)
        {
            // When progress reporting is supported, report incrementally per project and return an empty result at the end.
            // Otherwise aggregate and return the results for all projects at the end.
            if (args.Progress != null)
            {
                var tasks = solution.Projects.Select(p => SearchProjectAndReportSymbolsAsync(p, args, cancellationToken));
                await Task.WhenAll(tasks).ConfigureAwait(false);
                return ImmutableArray<SymbolInformation>.Empty;
            }
            else
            {
                var tasks = solution.Projects.Select(p => SearchProjectAsync(p, args.Query, cancellationToken));
                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                return results.SelectMany(a => a).ToImmutableArray();
            }
        }

        private static async Task<ImmutableArray<SymbolInformation>> SearchProjectAsync(Project project, string query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var results = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                project,
                ImmutableArray<Document>.Empty,
                query,
                s_supportedKinds,
                cancellationToken).ConfigureAwait(false);

            return await ConvertAsync(results, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Search the project and report the results back using <see cref="IProgress{T}"/>
        /// <see cref="IProgress{T}.Report(T)"/> implementation for symbol search is threadsafe.
        /// </summary>
        private static async Task SearchProjectAndReportSymbolsAsync(Project project, WorkspaceSymbolParams args, CancellationToken cancellationToken)
        {
            var convertedResults = await SearchProjectAsync(project, args.Query, cancellationToken).ConfigureAwait(false);
            args.Progress.Report(convertedResults.ToArray());
        }

        private static async Task<ImmutableArray<SymbolInformation>> ConvertAsync(
            ImmutableArray<INavigateToSearchResult> results, CancellationToken cancellationToken)
        {
            var symbols = ImmutableArray.CreateBuilder<SymbolInformation>();

            foreach (var result in results)
            {
                var text = await result.NavigableItem.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                symbols.Add(new VSSymbolInformation()
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
                });
            }

            return symbols.ToImmutableArray();
        }
    }
}
