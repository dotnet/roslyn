// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class InitializeHandlerShim : AbstractLiveShareHandlerShim<InitializeParams, InitializeResult>
    {
        public InitializeHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.InitializeName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.InitializeName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynInitializeHandlerShim : InitializeHandlerShim
    {
        [ImportingConstructor]
        public RoslynInitializeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.InitializeName)]
    internal class CSharpInitializeHandlerShim : InitializeHandlerShim
    {
        [ImportingConstructor]
        public CSharpInitializeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.InitializeName)]
    internal class VisualBasicInitializeHandlerShim : InitializeHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicInitializeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.InitializeName)]
    internal class TypeScriptInitializeHandlerShim : InitializeHandlerShim
    {
        [ImportingConstructor]
        public TypeScriptInitializeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }
}
