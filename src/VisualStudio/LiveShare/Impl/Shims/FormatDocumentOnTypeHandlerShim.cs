// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class FormatDocumentOnTypeHandlerShim : AbstractLiveShareHandlerShim<DocumentOnTypeFormattingParams, TextEdit[]>
    {
        private readonly IThreadingContext _threadingContext;

        public FormatDocumentOnTypeHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentOnTypeFormattingName)
        {
            _threadingContext = threadingContext;
        }

        public override async Task<TextEdit[]> HandleAsync(DocumentOnTypeFormattingParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // To get the formatting options, TypeScript expects to be called on the UI thread.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.HandleAsyncPreserveThreadContext(param, requestContext, cancellationToken).ConfigureAwait(false);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentOnTypeFormattingName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynFormatDocumentOnTypeHandlerShim : FormatDocumentOnTypeHandlerShim
    {
        [ImportingConstructor]
        public RoslynFormatDocumentOnTypeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentOnTypeFormattingName)]
    internal class CSharpFormatDocumentOnTypeHandlerShim : FormatDocumentOnTypeHandlerShim
    {
        [ImportingConstructor]
        public CSharpFormatDocumentOnTypeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentOnTypeFormattingName)]
    internal class VisualBasicFormatDocumentOnTypeHandlerShim : FormatDocumentOnTypeHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicFormatDocumentOnTypeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentOnTypeFormattingName)]
    internal class TypeScriptFormatDocumentOnTypeHandlerShim : FormatDocumentOnTypeHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptFormatDocumentOnTypeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }
}
