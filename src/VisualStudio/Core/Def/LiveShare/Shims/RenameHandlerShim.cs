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
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentRenameName)]
    internal class RenameHandlerShim : AbstractLiveShareHandlerShim<RenameParams, WorkspaceEdit>
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        public RenameHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentRenameName)
        {
            _threadingContext = threadingContext;
        }

        public override async Task<WorkspaceEdit> HandleAsync(RenameParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            // We need to be on the UI thread to call GetRenameInfo which computes the rename locations.
            // This is because Roslyn reads the readonly regions of the buffer to compute the locations in the document.
            // This is typically quick. It's marked configureawait(false) so that the bulk of the rename operation can happen
            // in background threads.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await base.HandleAsync(param, requestContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
