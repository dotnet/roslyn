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
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentReferencesName)]
    internal class FindAllReferencesHandlerShim : AbstractLiveShareHandlerShim<ReferenceParams, object[]>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public FindAllReferencesHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentReferencesName)
        {
            _threadingContext = threadingContext;
        }

        public override async Task<object[]> HandleAsync(ReferenceParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // Roslyn calls into third party extensions to compute reference results and needs to be on the UI thread to compute results.
            // This is not great for us and ideally we should ask for a Roslyn API where we can make this call without blocking the UI.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.HandleAsync(param, requestContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
