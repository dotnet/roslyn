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
    internal class FormatDocumentHandlerShim : AbstractLiveShareHandlerShim<DocumentFormattingParams, TextEdit[]>
    {
        private readonly IThreadingContext _threadingContext;

        public FormatDocumentHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentFormattingName)
        {
            _threadingContext = threadingContext;
        }

        public override async Task<TextEdit[]> HandleAsync(DocumentFormattingParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // To get the formatting options, TypeScript expects to be called on the UI thread.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.HandleAsyncPreserveThreadContext(param, requestContext, cancellationToken).ConfigureAwait(false);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentFormattingName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynFormatDocumentHandlerShim : FormatDocumentHandlerShim
    {
        [ImportingConstructor]
        public RoslynFormatDocumentHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentFormattingName)]
    internal class CSharpFormatDocumentHandlerShim : FormatDocumentHandlerShim
    {
        [ImportingConstructor]
        public CSharpFormatDocumentHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentFormattingName)]
    internal class VisualBasicFormatDocumentHandlerShim : FormatDocumentHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicFormatDocumentHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentFormattingName)]
    internal class TypeScriptFormatDocumentHandlerShim : FormatDocumentHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptFormatDocumentHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }
}
