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
    internal class FindImplementationsHandlerShim : AbstractLiveShareHandlerShim<TextDocumentPositionParams, object>
    {
        private readonly IThreadingContext _threadingContext;

        public FindImplementationsHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
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

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentImplementationName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynFindImplementationsHandlerShim : FindImplementationsHandlerShim
    {
        [ImportingConstructor]
        public RoslynFindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentImplementationName)]
    internal class CSharpFindImplementationsHandlerShim : FindImplementationsHandlerShim
    {
        [ImportingConstructor]
        public CSharpFindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentImplementationName)]
    internal class VisualBasicFindImplementationsHandlerShim : FindImplementationsHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicFindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentImplementationName)]
    internal class TypeScriptFindImplementationsHandlerShim : FindImplementationsHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptFindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }
}
