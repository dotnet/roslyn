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
    internal class FormatDocumentOnTypeHandlerOnMainThreadShim : AbstractLiveShareHandlerOnMainThreadShim<DocumentOnTypeFormattingParams, TextEdit[]>
    {
        public FormatDocumentOnTypeHandlerOnMainThreadShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentOnTypeFormattingName, threadingContext)
        {
        }
    }

    internal class FormatDocumentOnTypeHandlerShim : AbstractLiveShareHandlerShim<DocumentOnTypeFormattingParams, TextEdit[]>
    {
        public FormatDocumentOnTypeHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers, Methods.TextDocumentOnTypeFormattingName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentOnTypeFormattingName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynFormatDocumentOnTypeHandlerShim : FormatDocumentOnTypeHandlerOnMainThreadShim
    {
        [ImportingConstructor]
        public RoslynFormatDocumentOnTypeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentOnTypeFormattingName)]
    internal class CSharpFormatDocumentOnTypeHandlerShim : FormatDocumentOnTypeHandlerShim
    {
        [ImportingConstructor]
        public CSharpFormatDocumentOnTypeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentOnTypeFormattingName)]
    internal class VisualBasicFormatDocumentOnTypeHandlerShim : FormatDocumentOnTypeHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicFormatDocumentOnTypeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentOnTypeFormattingName)]
    internal class TypeScriptFormatDocumentOnTypeHandlerShim : FormatDocumentOnTypeHandlerOnMainThreadShim
    {
        [ImportingConstructor]
        public TypeScriptFormatDocumentOnTypeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }
}
