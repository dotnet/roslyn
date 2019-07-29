// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class CodeActionsHandlerShim : AbstractLiveShareHandlerShim<CodeActionParams, object[]>
    {
        public CodeActionsHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentCodeActionName)
        {
        }

        public async override Task<object[]> HandleAsync(CodeActionParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            return await base.HandleAsync(param, requestContext, cancellationToken).ConfigureAwait(false);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentCodeActionName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        public RoslynCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentCodeActionName)]
    internal class CSharpCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        public CSharpCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentCodeActionName)]
    internal class VisualBasicCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCodeActionName)]
    internal class TypeScriptCodeActionsHandlerShim : CodeActionsHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptCodeActionsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }
}
