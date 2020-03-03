// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCompletionName)]
    internal class TypeScriptCompletionHandlerShim : CompletionHandler, ILspRequestHandler<object, object?, Solution>
    {
        public async Task<object?> HandleAsync(object input, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // The VS LSP client supports streaming using IProgress<T> on various requests.
            // However, this is not yet supported through Live Share, so deserialization fails on the IProgress<T> property.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1043376 tracks Live Share support for this (committed for 16.6).
            var request = ((JObject)input).ToObject<CompletionParams>(InProcLanguageServer.JsonSerializer);
            // The return definition for TextDocumentCompletionName is SumType<CompletionItem[], CompletionList>.
            // However Live Share is unable to handle a SumType return when using ILspRequestHandler.
            // So instead we just return the actual value from the SumType.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1059193 tracks the fix.
            var result = await base.HandleRequestAsync(requestContext.Context, request, requestContext.GetClientCapabilities(), cancellationToken).ConfigureAwait(false);
            return result?.Value;
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCompletionResolveName)]
    internal class TypeScriptCompletionResolverHandlerShim : CompletionResolveHandler, ILspRequestHandler<CompletionItem, CompletionItem, Solution>
    {
        public Task<CompletionItem> HandleAsync(CompletionItem param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(requestContext.Context, param, requestContext.GetClientCapabilities(), cancellationToken);
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDocumentHighlightName)]
    internal class TypeScriptDocumentHighlightHandlerShim : DocumentHighlightsHandler, ILspRequestHandler<TextDocumentPositionParams, DocumentHighlight[], Solution>
    {
        public Task<DocumentHighlight[]> HandleAsync(TextDocumentPositionParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(requestContext.Context, param, requestContext.GetClientCapabilities(), cancellationToken);
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDocumentSymbolName)]
    internal class TypeScriptDocumentSymbolsHandlerShim : DocumentSymbolsHandler, ILspRequestHandler<DocumentSymbolParams, SymbolInformation[], Solution>
    {
        public async Task<SymbolInformation[]> HandleAsync(DocumentSymbolParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var clientCapabilities = requestContext.GetClientCapabilities();
            if (clientCapabilities.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true)
            {
                // If the value is true, set it to false.  Liveshare does not support hierarchical document symbols.
                clientCapabilities.TextDocument.DocumentSymbol.HierarchicalDocumentSymbolSupport = false;
            }

            var response = await base.HandleRequestAsync(requestContext.Context, param, clientCapabilities, cancellationToken).ConfigureAwait(false);

            // Since hierarchicalSupport will never be true, it is safe to cast the response to SymbolInformation[]
            return response.Cast<SymbolInformation>().ToArray();
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentFormattingName)]
    internal class TypeScriptFormatDocumentHandlerShim : FormatDocumentHandler, ILspRequestHandler<DocumentFormattingParams, TextEdit[], Solution>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public TypeScriptFormatDocumentHandlerShim(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public Task<TextEdit[]> HandleAsync(DocumentFormattingParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(requestContext.Context, request, requestContext.GetClientCapabilities(), cancellationToken);

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
        public TypeScriptFormatDocumentRangeHandlerShim(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public Task<TextEdit[]> HandleAsync(DocumentRangeFormattingParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(requestContext.Context, request, requestContext.GetClientCapabilities(), cancellationToken);

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
        public TypeScriptFormatDocumentOnTypeHandlerShim(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public Task<TextEdit[]> HandleAsync(DocumentOnTypeFormattingParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(requestContext.Context, request, requestContext?.ClientCapabilities?.ToObject<ClientCapabilities>(), cancellationToken);

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
    internal class TypeScriptFindImplementationsHandlerShim : FindImplementationsHandler, ILspRequestHandler<TextDocumentPositionParams, object?, Solution>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public TypeScriptFindImplementationsHandlerShim(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public async Task<object?> HandleAsync(TextDocumentPositionParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // The return definition for TextDocumentImplementationName is SumType<Location, Location[]>.
            // However Live Share is unable to handle a SumType return when using ILspRequestHandler.
            // So instead we just return the actual value from the SumType.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1059193 tracks the fix.
            var result = await base.HandleRequestAsync(requestContext.Context, request, requestContext.GetClientCapabilities(), cancellationToken).ConfigureAwait(false);
            return result?.Value;
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
        public async Task<InitializeResult> HandleAsync(InitializeParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var initializeResult = await base.HandleRequestAsync(requestContext.Context, param, requestContext.GetClientCapabilities(), cancellationToken).ConfigureAwait(false);
            initializeResult.Capabilities.Experimental = new RoslynExperimentalCapabilities { SyntacticLspProvider = true };
            return initializeResult;
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentSignatureHelpName)]
    internal class TypeScriptSignatureHelpHandlerShim : SignatureHelpHandler, ILspRequestHandler<TextDocumentPositionParams, SignatureHelp, Solution>
    {
        [ImportingConstructor]
        public TypeScriptSignatureHelpHandlerShim([ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> allProviders) : base(allProviders)
        {
        }

        public Task<SignatureHelp> HandleAsync(TextDocumentPositionParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(requestContext.Context, param, requestContext.GetClientCapabilities(), cancellationToken);
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.WorkspaceSymbolName)]
    internal class TypeScriptWorkspaceSymbolsHandlerShim : WorkspaceSymbolsHandler, ILspRequestHandler<object, SymbolInformation[], Solution>
    {
        public Task<SymbolInformation[]> HandleAsync(object input, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // The VS LSP client supports streaming using IProgress<T> on various requests.
            // However, this is not yet supported through Live Share, so deserialization fails on the IProgress<T> property.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1043376 tracks Live Share support for this (committed for 16.6).
            var request = ((JObject)input).ToObject<WorkspaceSymbolParams>(InProcLanguageServer.JsonSerializer);
            return base.HandleRequestAsync(requestContext.Context, request, requestContext.GetClientCapabilities(), cancellationToken);
        }
    }
}
