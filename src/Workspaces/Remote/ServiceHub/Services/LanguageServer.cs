// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Newtonsoft.Json;
using Roslyn.Utilities;
using StreamJsonRpc;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Remote
{
    // we need per language server for now since ILanguageClient
    // doesn't allow multiple content types to be associated with
    // one language server
    internal class CSharpLanguageServer : LanguageServer
    {
        public CSharpLanguageServer(Stream stream, IServiceProvider serviceProvider)
            : base(stream, serviceProvider, LanguageNames.CSharp)
        {
        }
    }

    internal class VisualBasicLanguageServer : LanguageServer
    {
        public VisualBasicLanguageServer(Stream stream, IServiceProvider serviceProvider)
            : base(stream, serviceProvider, LanguageNames.VisualBasic)
        {
        }
    }

    internal abstract class LanguageServer : ServiceBase
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

        private readonly string _languageName;

        public LanguageServer(Stream stream, IServiceProvider serviceProvider, string languageName)
            : base(serviceProvider, stream, SpecializedCollections.EmptyEnumerable<JsonConverter>())
        {
            _languageName = languageName;

            StartService();
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public object Initialize(int? processId, string rootPath, Uri rootUri, ClientCapabilities capabilities, TraceSetting trace, CancellationToken cancellationToken)
        {
            // our LSP server only supports WorkspaceStreamingSymbolProvider capability
            // for now
            return new InitializeResult()
            {
                Capabilities = new VSServerCapabilities()
                {
                    WorkspaceStreamingSymbolProvider = true
                }
            };
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public Task Initialized()
        {
            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public void Shutdown(CancellationToken cancellationToken)
        {
            // our language server shutdown when VS shutdown
        }

        [JsonRpcMethod(VSSymbolMethods.WorkspaceBeginSymbolName)]
        public Task<VSBeginSymbolParams> WorkspaceBeginSymbolAsync(string query, int searchId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    // for now, we use whatever solution we have currently. in future, we will add an ability to sync VS's current solution
                    // on demand from OOP side
                    await SearchAsync(SolutionService.PrimaryWorkspace.CurrentSolution, query, searchId, cancellationToken).ConfigureAwait(false);
                    return new VSBeginSymbolParams();
                }
            }, cancellationToken);
        }

        private async Task SearchAsync(Solution solution, string query, int searchId, CancellationToken cancellationToken)
        {
            foreach (var project in solution.Projects.Where(p => p.Language == _languageName))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var results = await AbstractNavigateToSearchService.SearchProjectInCurrentProcessAsync(
                    project,
                    ImmutableArray<Document>.Empty,
                    query,
                    s_supportedKinds,
                    cancellationToken).ConfigureAwait(false);

                var lspResults = await Convert(results, cancellationToken).ConfigureAwait(false);

                await InvokeAsync(
                    VSSymbolMethods.WorkspacePublishSymbolName,
                    new object[] { new VSPublishSymbolParams() { SearchId = searchId, Symbols = lspResults } },
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<VSSymbolInformation[]> Convert(
            ImmutableArray<INavigateToSearchResult> results, CancellationToken cancellationToken)
        {
            var symbols = new VSSymbolInformation[results.Length];

            for (var i = 0; i < results.Length; i++)
            {
                var text = await results[i].NavigableItem.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                symbols[i] = new VSSymbolInformation()
                {
                    Name = results[i].Name,
                    ContainerName = results[i].AdditionalInformation,
                    Kind = ProtocolConversions.NavigateToKindToSymbolKind(results[i].Kind),
                    Location = new LSP.Location()
                    {
                        Uri = new Uri(results[i].NavigableItem.Document.FilePath),
                        Range = ProtocolConversions.TextSpanToRange(results[i].NavigableItem.SourceSpan, text)
                    },
                    Icon = new VisualStudio.Text.Adornments.ImageElement(results[i].NavigableItem.Glyph.GetImageId())
                };
            }

            return symbols;
        }
    }
}
