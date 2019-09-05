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
    internal class FormatDocumentHandlerOnMainThreadShim : AbstractLiveShareHandlerOnMainThreadShim<DocumentFormattingParams, TextEdit[]>
    {
        public FormatDocumentHandlerOnMainThreadShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentFormattingName, threadingContext)
        {
        }
    }

    internal class FormatDocumentHandlerShim : AbstractLiveShareHandlerShim<DocumentFormattingParams, TextEdit[]>
    {

        public FormatDocumentHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            : base(requestHandlers, Methods.TextDocumentFormattingName)
        {
        }
    }


    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentFormattingName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynFormatDocumentHandlerShim : FormatDocumentHandlerOnMainThreadShim
    {
        [ImportingConstructor]
        public RoslynFormatDocumentHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentFormattingName)]
    internal class CSharpFormatDocumentHandlerShim : FormatDocumentHandlerShim
    {
        [ImportingConstructor]
        public CSharpFormatDocumentHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentFormattingName)]
    internal class VisualBasicFormatDocumentHandlerShim : FormatDocumentHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicFormatDocumentHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentFormattingName)]
    internal class TypeScriptFormatDocumentHandlerShim : FormatDocumentHandlerOnMainThreadShim
    {
        [ImportingConstructor]
        public TypeScriptFormatDocumentHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, threadingContext)
        {
        }
    }
}
