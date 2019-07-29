// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class SignatureHelpHandlerShim : AbstractLiveShareHandlerShim<TextDocumentPositionParams, SignatureHelp>
    {
        public SignatureHelpHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentSignatureHelpName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentSignatureHelpName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynSignatureHelpHandlerShim : SignatureHelpHandlerShim
    {
        [ImportingConstructor]
        public RoslynSignatureHelpHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentSignatureHelpName)]
    internal class CSharpSignatureHelpHandlerShim : SignatureHelpHandlerShim
    {
        [ImportingConstructor]
        public CSharpSignatureHelpHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentSignatureHelpName)]
    internal class VisualBasicSignatureHelpHandlerShim : SignatureHelpHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicSignatureHelpHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentSignatureHelpName)]
    internal class TypeScriptSignatureHelpHandlerShim : SignatureHelpHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptSignatureHelpHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }
}
