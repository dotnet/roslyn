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
    internal class FormatDocumentRangeHandlerShim : AbstractLiveShareHandlerShim<DocumentRangeFormattingParams, TextEdit[]>
    {
        private readonly IThreadingContext _threadingContext;

        public FormatDocumentRangeHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentRangeFormattingName)
        {
            _threadingContext = threadingContext;
        }

        public override async Task<TextEdit[]> HandleAsync(DocumentRangeFormattingParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // To get the formatting options, TypeScript expects to be called on the UI thread.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.HandleAsyncPreserveThreadContext(param, requestContext, cancellationToken).ConfigureAwait(false);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentRangeFormattingName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynFormatDocumentRangeHandlerShim : FormatDocumentRangeHandlerShim
    {
        [ImportingConstructor]
        public RoslynFormatDocumentRangeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentRangeFormattingName)]
    internal class CSharpFormatDocumentRangeHandlerShim : FormatDocumentRangeHandlerShim
    {
        [ImportingConstructor]
        public CSharpFormatDocumentRangeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentRangeFormattingName)]
    internal class VisualBasicFormatDocumentRangeHandlerShim : FormatDocumentRangeHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicFormatDocumentRangeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentRangeFormattingName)]
    internal class TypeScriptFormatDocumentRangeHandlerShim : FormatDocumentRangeHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptFormatDocumentRangeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }
}
