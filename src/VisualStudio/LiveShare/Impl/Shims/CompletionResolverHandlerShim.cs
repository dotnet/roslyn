// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class CompletionResolverHandlerShim : AbstractLiveShareHandlerShim<CompletionItem, CompletionItem>
    {
        public CompletionResolverHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentCompletionResolveName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentCompletionResolveName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynCompletionResolverHandlerShim : CompletionResolverHandlerShim
    {
        [ImportingConstructor]
        public RoslynCompletionResolverHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentCompletionResolveName)]
    internal class CSharpCompletionResolverHandlerShim : CompletionResolverHandlerShim
    {
        [ImportingConstructor]
        public CSharpCompletionResolverHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentCompletionResolveName)]
    internal class VisualBasicCompletionResolverHandlerShim : CompletionResolverHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicCompletionResolverHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentCompletionResolveName)]
    internal class TypeScriptCompletionResolverHandlerShim : CompletionResolverHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptCompletionResolverHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }
}
