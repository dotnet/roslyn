// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class WorkspaceSymbolsHandlerShim : AbstractLiveShareHandlerShim<WorkspaceSymbolParams, SymbolInformation[]>
    {
        public WorkspaceSymbolsHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.WorkspaceSymbolName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.WorkspaceSymbolName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynWorkspaceSymbolsHandlerShim : WorkspaceSymbolsHandlerShim
    {
        [ImportingConstructor]
        public RoslynWorkspaceSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.WorkspaceSymbolName)]
    internal class CSharpWorkspaceSymbolsHandlerShim : WorkspaceSymbolsHandlerShim
    {
        [ImportingConstructor]
        public CSharpWorkspaceSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.WorkspaceSymbolName)]
    internal class VisualBasicWorkspaceSymbolsHandlerShim : WorkspaceSymbolsHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicWorkspaceSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.WorkspaceSymbolName)]
    internal class TypeScriptWorkspaceSymbolsHandlerShim : WorkspaceSymbolsHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptWorkspaceSymbolsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }
}
