// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Typescript format expects to be called from the main thread.
    /// </summary>
    internal class FindImplementationsHandlerOnMainThreadShim : AbstractLiveShareHandlerOnMainThreadShim<TextDocumentPositionParams, object>
    {
        public FindImplementationsHandlerOnMainThreadShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentImplementationName, threadingContext)
        {
        }
    }

    internal class FindImplementationsHandlerShim : AbstractLiveShareHandlerShim<TextDocumentPositionParams, object>
    {
        public FindImplementationsHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers, Methods.TextDocumentImplementationName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentImplementationName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynFindImplementationsHandlerShim : FindImplementationsHandlerOnMainThreadShim
    {
        [ImportingConstructor]
        public RoslynFindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentImplementationName)]
    internal class CSharpFindImplementationsHandlerShim : FindImplementationsHandlerShim
    {
        [ImportingConstructor]
        public CSharpFindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentImplementationName)]
    internal class VisualBasicFindImplementationsHandlerShim : FindImplementationsHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicFindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentImplementationName)]
    internal class TypeScriptFindImplementationsHandlerShim : FindImplementationsHandlerOnMainThreadShim
    {
        [ImportingConstructor]
        public TypeScriptFindImplementationsHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }
}
