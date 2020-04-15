// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCompletionName)]
    internal class TypeScriptCompletionHandlerShim : AbstractLiveShareHandlerShim<CompletionParams, object>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptCompletionHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentCompletionName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCompletionResolveName)]
    internal class TypeScriptCompletionResolverHandlerShim : AbstractLiveShareHandlerShim<CompletionItem, CompletionItem>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptCompletionResolverHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentCompletionResolveName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDocumentHighlightName)]
    internal class TypeScriptDocumentHighlightHandlerShim : AbstractLiveShareHandlerShim<TextDocumentPositionParams, DocumentHighlight[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptDocumentHighlightHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentDocumentHighlightName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDocumentSymbolName)]
    internal class TypeScriptDocumentSymbolsHandlerShim : AbstractLiveShareHandlerShim<DocumentSymbolParams, SymbolInformation[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptDocumentSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentDocumentSymbolName)
        {
        }

        public override async Task<SymbolInformation[]> HandleAsync(DocumentSymbolParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var clientCapabilities = requestContext.ClientCapabilities?.ToObject<VSClientCapabilities>();
            if (clientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true)
            {
                // If the value is true, set it to false.  Liveshare does not support hierarchical document symbols.
                clientCapabilities.TextDocument.DocumentSymbol.HierarchicalDocumentSymbolSupport = false;
            }

            var handler = (IRequestHandler<DocumentSymbolParams, object[]>)LazyRequestHandler.Value;
            var response = await handler.HandleRequestAsync(requestContext.Context, param, clientCapabilities, cancellationToken).ConfigureAwait(false);

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
        public TypeScriptFormatDocumentHandlerShim(IThreadingContext threadingContext)
            => _threadingContext = threadingContext;

        public Task<TextEdit[]> HandleAsync(DocumentFormattingParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(requestContext.Context, request, requestContext.ClientCapabilities?.ToObject<ClientCapabilities>(), cancellationToken);

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
        public TypeScriptFormatDocumentRangeHandlerShim(IThreadingContext threadingContext)
            => _threadingContext = threadingContext;

        public Task<TextEdit[]> HandleAsync(DocumentRangeFormattingParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(requestContext.Context, request, requestContext.ClientCapabilities?.ToObject<ClientCapabilities>(), cancellationToken);

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
        public TypeScriptFormatDocumentOnTypeHandlerShim(IThreadingContext threadingContext)
            => _threadingContext = threadingContext;

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
    internal class TypeScriptFindImplementationsHandlerShim : FindImplementationsHandler, ILspRequestHandler<TextDocumentPositionParams, object, Solution>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptFindImplementationsHandlerShim(IThreadingContext threadingContext)
            => _threadingContext = threadingContext;

        public Task<object> HandleAsync(TextDocumentPositionParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
            => base.HandleRequestAsync(requestContext.Context, request, requestContext.ClientCapabilities?.ToObject<ClientCapabilities>(), cancellationToken);

        protected override async Task FindImplementationsAsync(IFindUsagesService findUsagesService, Document document, int position, SimpleFindUsagesContext context)
        {
            // TypeScript expects to be called on the UI to get implementations.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(context.CancellationToken);
            await base.FindImplementationsAsync(findUsagesService, document, position, context).ConfigureAwait(false);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.InitializeName)]
    internal class TypeScriptInitializeHandlerShim : AbstractLiveShareHandlerShim<InitializeParams, InitializeResult>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptInitializeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.InitializeName)
        {
        }

        public override async Task<InitializeResult> HandleAsync(InitializeParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var initializeResult = await base.HandleAsync(param, requestContext, cancellationToken).ConfigureAwait(false);
            initializeResult.Capabilities.Experimental = new RoslynExperimentalCapabilities { SyntacticLspProvider = true };
            return initializeResult;
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentSignatureHelpName)]
    internal class TypeScriptSignatureHelpHandlerShim : AbstractLiveShareHandlerShim<TextDocumentPositionParams, SignatureHelp>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptSignatureHelpHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentSignatureHelpName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentRenameName)]
    internal class TypeScriptRenameHandlerShim : AbstractLiveShareHandlerShim<RenameParams, WorkspaceEdit>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptRenameHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentRenameName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.WorkspaceSymbolName)]
    internal class TypeScriptWorkspaceSymbolsHandlerShim : AbstractLiveShareHandlerShim<WorkspaceSymbolParams, SymbolInformation[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptWorkspaceSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.WorkspaceSymbolName)
        {
        }
    }
}
