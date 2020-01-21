using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Shims
{
    internal abstract class AbstractGoToDefinitionHandlerShim : AbstractLiveShareHandlerShim<TextDocumentPositionParams, object>
    {
        public AbstractGoToDefinitionHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentDefinitionName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentDefinitionName)]
    internal class CSharpGoToDefinitionHandlerShim : AbstractGoToDefinitionHandlerShim
    {
        [ImportingConstructor]
        public CSharpGoToDefinitionHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentDefinitionName)]
    internal class VisualBasicGoToDefinitionHandlerShim : AbstractGoToDefinitionHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicGoToDefinitionHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers)
        {
        }
    }
}
