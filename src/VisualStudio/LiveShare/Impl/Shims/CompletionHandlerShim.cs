// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class CompletionHandlerShim : AbstractLiveShareHandlerShim<CompletionParams, object>
    {
        public CompletionHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentCompletionName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentCompletionName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynCompletionHandlerShim : CompletionHandlerShim
    {
        [ImportingConstructor]
        public RoslynCompletionHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentCompletionName)]
    internal class CSharpCompletionHandlerShim : CompletionHandlerShim
    {
        [ImportingConstructor]
        public CSharpCompletionHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentCompletionName)]
    internal class VisualBasicCompletionHandlerShim : CompletionHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicCompletionHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCompletionName)]
    internal class TypeScriptCompletionHandlerShim : CompletionHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptCompletionHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }
}
