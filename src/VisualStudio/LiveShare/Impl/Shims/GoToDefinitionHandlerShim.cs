using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Shims
{
    internal class GoToDefinitionHandlerShim : AbstractLiveShareHandlerShim<TextDocumentPositionParams, object>
    {
        public GoToDefinitionHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers, Methods.TextDocumentDefinitionName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentDefinitionName)]
    internal class CSharpFindImplementationsHandlerShim : GoToDefinitionHandlerShim
    {
        [ImportingConstructor]
        public CSharpFindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentDefinitionName)]
    internal class VisualBasicFindImplementationsHandlerShim : GoToDefinitionHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicFindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }
}
