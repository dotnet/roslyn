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
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentImplementationName)]
    internal class FindImplementationsHandlerShim : AbstractLiveShareHandlerShim<TextDocumentPositionParams, object>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public FindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentImplementationName)
        {
            _threadingContext = threadingContext;
        }

        public override async Task<object> HandleAsync(TextDocumentPositionParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // TypeScript requires this call to be on the UI thread.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.HandleAsyncPreserveThreadContext(param, requestContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
