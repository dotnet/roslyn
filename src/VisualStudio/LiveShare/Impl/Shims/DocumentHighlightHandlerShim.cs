// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class DocumentHighlightHandlerShim : AbstractLiveShareHandlerShim<TextDocumentPositionParams, DocumentHighlight[]>
    {
        public DocumentHighlightHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentDocumentHighlightName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentDocumentHighlightName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynDocumentHighlightHandlerShim : DocumentHighlightHandlerShim
    {
        [ImportingConstructor]
        public RoslynDocumentHighlightHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentDocumentHighlightName)]
    internal class CSharpDocumentHighlightHandlerShim : DocumentHighlightHandlerShim
    {
        [ImportingConstructor]
        public CSharpDocumentHighlightHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentDocumentHighlightName)]
    internal class VisualBasicDocumentHighlightHandlerShim : DocumentHighlightHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicDocumentHighlightHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentDocumentHighlightName)]
    internal class TypeScriptDocumentHighlightHandlerShim : DocumentHighlightHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptDocumentHighlightHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }
}
