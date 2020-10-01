﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using LSP = Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCompletionName)]
    internal class TypeScriptCompletionHandlerShim : CompletionHandler, ILspRequestHandler<object, LanguageServer.Protocol.CompletionList?, Solution>
    {
        /// <summary>
        /// The VS LSP client supports streaming using IProgress on various requests.	
        /// However, this works through liveshare on the LSP client, but not the LSP extension.
        /// see https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1107682 for tracking.
        /// </summary>
        private static readonly JsonSerializer s_jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Error = (sender, args) =>
            {
                if (object.Equals(args.ErrorContext.Member, "partialResultToken"))
                {
                    args.ErrorContext.Handled = true;
                }
            }
        });

        private readonly ILspSolutionProvider _solutionProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptCompletionHandlerShim(ILspSolutionProvider solutionProvider) : base(Array.Empty<Lazy<CompletionProvider, CompletionProviderMetadata>>())
        {
            _solutionProvider = solutionProvider;
        }

        public Task<LanguageServer.Protocol.CompletionList?> HandleAsync(object input, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // The VS LSP client supports streaming using IProgress<T> on various requests.	
            // However, this works through liveshare on the LSP client, but not the LSP extension.
            // see https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1107682 for tracking.
            var request = ((JObject)input).ToObject<CompletionParams>(s_jsonSerializer);
            var context = this.CreateRequestContext(request, _solutionProvider, requestContext.GetClientCapabilities());
            return base.HandleRequestAsync(request, context, cancellationToken);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCompletionResolveName)]
    internal class TypeScriptCompletionResolverHandlerShim : CompletionResolveHandler, ILspRequestHandler<LanguageServer.Protocol.CompletionItem, LanguageServer.Protocol.CompletionItem, Solution>
    {
        private readonly ILspSolutionProvider _solutionProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptCompletionResolverHandlerShim(ILspSolutionProvider solutionProvider)
        {
            _solutionProvider = solutionProvider;
        }

        public Task<LanguageServer.Protocol.CompletionItem> HandleAsync(LanguageServer.Protocol.CompletionItem param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var context = this.CreateRequestContext(param, _solutionProvider, requestContext.GetClientCapabilities());
            return base.HandleRequestAsync(param, context, cancellationToken);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDocumentSymbolName)]
    internal class TypeScriptDocumentSymbolsHandlerShim : DocumentSymbolsHandler, ILspRequestHandler<DocumentSymbolParams, SymbolInformation[], Solution>
    {
        private readonly ILspSolutionProvider _solutionProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptDocumentSymbolsHandlerShim(ILspSolutionProvider solutionProvider)
        {
            _solutionProvider = solutionProvider;
        }

        public async Task<SymbolInformation[]> HandleAsync(DocumentSymbolParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var clientCapabilities = requestContext.GetClientCapabilities();
            if (clientCapabilities.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true)
            {
                // If the value is true, set it to false.  Liveshare does not support hierarchical document symbols.
                clientCapabilities.TextDocument.DocumentSymbol.HierarchicalDocumentSymbolSupport = false;
            }

            var context = this.CreateRequestContext(param, _solutionProvider, clientCapabilities);
            var response = await base.HandleRequestAsync(param, context, cancellationToken).ConfigureAwait(false);

            // Since hierarchicalSupport will never be true, it is safe to cast the response to SymbolInformation[]
            return response.Cast<SymbolInformation>().ToArray();
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentImplementationName)]
    internal class TypeScriptFindImplementationsHandlerShim : FindImplementationsHandler, ILspRequestHandler<TextDocumentPositionParams, LanguageServer.Protocol.Location[], Solution>
    {
        private readonly ILspSolutionProvider _solutionProvider;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptFindImplementationsHandlerShim(ILspSolutionProvider solutionProvider, IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
            _solutionProvider = solutionProvider;
        }

        public Task<LanguageServer.Protocol.Location[]> HandleAsync(TextDocumentPositionParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var context = this.CreateRequestContext(request, _solutionProvider, requestContext.GetClientCapabilities());
            return base.HandleRequestAsync(request, context, cancellationToken);
        }

        protected override async Task FindImplementationsAsync(IFindUsagesService findUsagesService, Document document, int position, SimpleFindUsagesContext context)
        {
            // TypeScript expects to be called on the UI to get implementations.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(context.CancellationToken);
            await base.FindImplementationsAsync(findUsagesService, document, position, context).ConfigureAwait(false);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.InitializeName)]
    internal class TypeScriptInitializeHandlerShim : InitializeHandler, ILspRequestHandler<InitializeParams, InitializeResult, Solution>
    {
        private readonly ILspSolutionProvider _solutionProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptInitializeHandlerShim([ImportMany] IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders,
            ILspSolutionProvider solutionProvider) : base(completionProviders)
        {
            _solutionProvider = solutionProvider;
        }

        public Task<InitializeResult> HandleAsync(InitializeParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(param, this.CreateRequestContext(param, _solutionProvider, requestContext.GetClientCapabilities()), cancellationToken);
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentSignatureHelpName)]
    internal class TypeScriptSignatureHelpHandlerShim : SignatureHelpHandler, ILspRequestHandler<TextDocumentPositionParams, SignatureHelp, Solution>
    {
        private readonly ILspSolutionProvider _solutionProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptSignatureHelpHandlerShim([ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> allProviders,
            ILspSolutionProvider solutionProvider) : base(allProviders)
        {
            _solutionProvider = solutionProvider;
        }

        public Task<SignatureHelp> HandleAsync(TextDocumentPositionParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var context = this.CreateRequestContext(param, _solutionProvider, requestContext.GetClientCapabilities());
            return base.HandleRequestAsync(param, context, cancellationToken);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.WorkspaceSymbolName)]
    internal class TypeScriptWorkspaceSymbolsHandlerShim : WorkspaceSymbolsHandler, ILspRequestHandler<WorkspaceSymbolParams, SymbolInformation[], Solution>
    {
        private readonly ILspSolutionProvider _solutionProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptWorkspaceSymbolsHandlerShim(ILspSolutionProvider solutionProvider)
        {
            _solutionProvider = solutionProvider;
        }

        [JsonRpcMethod(UseSingleObjectParameterDeserialization = true)]
        public Task<SymbolInformation[]> HandleAsync(WorkspaceSymbolParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var context = this.CreateRequestContext(request, _solutionProvider, requestContext.GetClientCapabilities());
            return base.HandleRequestAsync(request, context, cancellationToken);
        }
    }

    /// <summary>
    /// Helper methods only used by the above, that can be deleted along with the above
    /// </summary>
    internal static class Extensions
    {
        public static LSP.RequestContext CreateRequestContext<TRequest, TResponse>(this IRequestHandler<TRequest, TResponse> requestHandler, TRequest request, ILspSolutionProvider provider, ClientCapabilities clientCapabilities, string? clientName = null)
        {
            var textDocument = requestHandler.GetTextDocumentIdentifier(request);

            var (document, solution) = provider.GetDocumentAndSolution(textDocument, clientName);

            return new LSP.RequestContext(solution, clientCapabilities, clientName, document, solutionUpdater: null);
        }
    }
}
