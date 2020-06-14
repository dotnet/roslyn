﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCompletionName)]
    internal class TypeScriptCompletionHandlerShim : CompletionHandler, ILspRequestHandler<object, LanguageServer.Protocol.CompletionItem[], Solution>
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptCompletionHandlerShim(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public Task<LanguageServer.Protocol.CompletionItem[]> HandleAsync(object input, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // The VS LSP client supports streaming using IProgress<T> on various requests.	
            // However, this works through liveshare on the LSP client, but not the LSP extension.
            // see https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1107682 for tracking.
            var request = ((JObject)input).ToObject<CompletionParams>(s_jsonSerializer);
            return base.HandleRequestAsync(request, requestContext.GetClientCapabilities(), null, cancellationToken);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCompletionResolveName)]
    internal class TypeScriptCompletionResolverHandlerShim : CompletionResolveHandler, ILspRequestHandler<LanguageServer.Protocol.CompletionItem, LanguageServer.Protocol.CompletionItem, Solution>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptCompletionResolverHandlerShim(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public Task<LanguageServer.Protocol.CompletionItem> HandleAsync(LanguageServer.Protocol.CompletionItem param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(param, requestContext.GetClientCapabilities(), null, cancellationToken);
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDocumentHighlightName)]
    internal class TypeScriptDocumentHighlightHandlerShim : DocumentHighlightsHandler, ILspRequestHandler<TextDocumentPositionParams, DocumentHighlight[], Solution>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptDocumentHighlightHandlerShim(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public Task<DocumentHighlight[]> HandleAsync(TextDocumentPositionParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(param, requestContext.GetClientCapabilities(), null, cancellationToken);
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDocumentSymbolName)]
    internal class TypeScriptDocumentSymbolsHandlerShim : DocumentSymbolsHandler, ILspRequestHandler<DocumentSymbolParams, SymbolInformation[], Solution>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptDocumentSymbolsHandlerShim(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public async Task<SymbolInformation[]> HandleAsync(DocumentSymbolParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var clientCapabilities = requestContext.GetClientCapabilities();
            if (clientCapabilities.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true)
            {
                // If the value is true, set it to false.  Liveshare does not support hierarchical document symbols.
                clientCapabilities.TextDocument.DocumentSymbol.HierarchicalDocumentSymbolSupport = false;
            }

            var response = await base.HandleRequestAsync(param, clientCapabilities, null, cancellationToken).ConfigureAwait(false);

            // Since hierarchicalSupport will never be true, it is safe to cast the response to SymbolInformation[]
            return response.Cast<SymbolInformation>().ToArray();
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentFormattingName)]
    internal class TypeScriptFormatDocumentHandlerShim : FormatDocumentHandler, ILspRequestHandler<DocumentFormattingParams, TextEdit[], Solution>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptFormatDocumentHandlerShim(ILspSolutionProvider solutionProvider, IThreadingContext threadingContext) : base(solutionProvider)
            => _threadingContext = threadingContext;

        public Task<TextEdit[]> HandleAsync(DocumentFormattingParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(request, requestContext.GetClientCapabilities(), null, cancellationToken);

        protected override async Task<IList<TextChange>> GetFormattingChangesAsync(IEditorFormattingService formattingService, Document document, TextSpan? textSpan, CancellationToken cancellationToken)
        {
            // TypeScript expects to be called on the UI thread to get the formatting options.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.GetFormattingChangesAsync(formattingService, document, textSpan, cancellationToken).ConfigureAwait(false);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentRangeFormattingName)]
    internal class TypeScriptFormatDocumentRangeHandlerShim : FormatDocumentRangeHandler, ILspRequestHandler<DocumentRangeFormattingParams, TextEdit[], Solution>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptFormatDocumentRangeHandlerShim(ILspSolutionProvider solutionProvider, IThreadingContext threadingContext) : base(solutionProvider)
            => _threadingContext = threadingContext;

        public Task<TextEdit[]> HandleAsync(DocumentRangeFormattingParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(request, requestContext.GetClientCapabilities(), null, cancellationToken);

        protected override async Task<IList<TextChange>> GetFormattingChangesAsync(IEditorFormattingService formattingService, Document document, TextSpan? textSpan, CancellationToken cancellationToken)
        {
            // TypeScript expects to be called on the UI thread to get the formatting options.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.GetFormattingChangesAsync(formattingService, document, textSpan, cancellationToken).ConfigureAwait(false);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentOnTypeFormattingName)]
    internal class TypeScriptFormatDocumentOnTypeHandlerShim : FormatDocumentOnTypeHandler, ILspRequestHandler<DocumentOnTypeFormattingParams, TextEdit[], Solution>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptFormatDocumentOnTypeHandlerShim(ILspSolutionProvider solutionProvider, IThreadingContext threadingContext) : base(solutionProvider)
            => _threadingContext = threadingContext;

        public Task<TextEdit[]> HandleAsync(DocumentOnTypeFormattingParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(request, requestContext?.ClientCapabilities?.ToObject<ClientCapabilities>(), null, cancellationToken);

        protected override async Task<IList<TextChange>?> GetFormattingChangesAsync(IEditorFormattingService formattingService, Document document, char typedChar, int position, CancellationToken cancellationToken)
        {
            // TypeScript expects to be called on the UI thread to get the formatting options.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.GetFormattingChangesAsync(formattingService, document, typedChar, position, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<IList<TextChange>?> GetFormattingChangesOnReturnAsync(IEditorFormattingService formattingService, Document document, int position, CancellationToken cancellationToken)
        {
            // TypeScript expects to be called on the UI thread to get the formatting options.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.GetFormattingChangesOnReturnAsync(formattingService, document, position, cancellationToken).ConfigureAwait(false);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentImplementationName)]
    internal class TypeScriptFindImplementationsHandlerShim : FindImplementationsHandler, ILspRequestHandler<TextDocumentPositionParams, LanguageServer.Protocol.Location[], Solution>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptFindImplementationsHandlerShim(ILspSolutionProvider solutionProvider, IThreadingContext threadingContext) : base(solutionProvider)
            => _threadingContext = threadingContext;

        public Task<LanguageServer.Protocol.Location[]> HandleAsync(TextDocumentPositionParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(request, requestContext.GetClientCapabilities(), null, cancellationToken);

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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptInitializeHandlerShim([ImportMany] IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders,
            ILspSolutionProvider solutionProvider) : base(completionProviders)
        {
        }

        public async Task<InitializeResult> HandleAsync(InitializeParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var initializeResult = await base.HandleRequestAsync(param, requestContext.GetClientCapabilities(), null, cancellationToken).ConfigureAwait(false);
            initializeResult.Capabilities.Experimental = new RoslynExperimentalCapabilities { SyntacticLspProvider = true };
            return initializeResult;
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentSignatureHelpName)]
    internal class TypeScriptSignatureHelpHandlerShim : SignatureHelpHandler, ILspRequestHandler<TextDocumentPositionParams, SignatureHelp, Solution>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptSignatureHelpHandlerShim([ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> allProviders,
            ILspSolutionProvider solutionProvider) : base(allProviders, solutionProvider)
        {
        }

        public Task<SignatureHelp> HandleAsync(TextDocumentPositionParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(param, requestContext.GetClientCapabilities(), null, cancellationToken);
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentRenameName)]
    internal class TypeScriptRenameHandlerShim : RenameHandler, ILspRequestHandler<RenameParams, WorkspaceEdit?, Solution>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptRenameHandlerShim(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public Task<WorkspaceEdit?> HandleAsync(RenameParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(param, requestContext.GetClientCapabilities(), null, cancellationToken);
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.WorkspaceSymbolName)]
    internal class TypeScriptWorkspaceSymbolsHandlerShim : WorkspaceSymbolsHandler, ILspRequestHandler<WorkspaceSymbolParams, SymbolInformation[], Solution>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptWorkspaceSymbolsHandlerShim(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        [JsonRpcMethod(UseSingleObjectParameterDeserialization = true)]
        public Task<SymbolInformation[]> HandleAsync(WorkspaceSymbolParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(request, requestContext.GetClientCapabilities(), null, cancellationToken);
    }
}
