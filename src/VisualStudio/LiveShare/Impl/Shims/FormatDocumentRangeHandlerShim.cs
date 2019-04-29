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
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentRangeFormattingName)]
    internal class FormatDocumentRangeHandlerShim : AbstractLiveShareHandlerShim<DocumentRangeFormattingParams, TextEdit[]>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public FormatDocumentRangeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentRangeFormattingName)
        {
            _threadingContext = threadingContext;
        }

        public override async Task<TextEdit[]> HandleAsync(DocumentRangeFormattingParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // To get the formatting options, TypeScript expects to be called on the UI thread.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.HandleAsync(param, requestContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
