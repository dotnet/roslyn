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
    internal class FormatDocumentRangeHandlerOnMainThreadShim : AbstractLiveShareHandlerOnMainThreadShim<DocumentRangeFormattingParams, TextEdit[]>
    {
        public FormatDocumentRangeHandlerOnMainThreadShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext)
            : base(requestHandlers, Methods.TextDocumentRangeFormattingName, threadingContext)
        {
        }
    }

    internal class FormatDocumentRangeHandlerShim : AbstractLiveShareHandlerShim<DocumentRangeFormattingParams, TextEdit[]>
    {
        public FormatDocumentRangeHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers, Methods.TextDocumentRangeFormattingName)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.TextDocumentRangeFormattingName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynFormatDocumentRangeHandlerShim : FormatDocumentRangeHandlerOnMainThreadShim
    {
        [ImportingConstructor]
        public RoslynFormatDocumentRangeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.TextDocumentRangeFormattingName)]
    internal class CSharpFormatDocumentRangeHandlerShim : FormatDocumentRangeHandlerShim
    {
        [ImportingConstructor]
        public CSharpFormatDocumentRangeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.TextDocumentRangeFormattingName)]
    internal class VisualBasicFormatDocumentRangeHandlerShim : FormatDocumentRangeHandlerShim
    {
        [ImportingConstructor]
        public VisualBasicFormatDocumentRangeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : base(requestHandlers)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, Methods.TextDocumentRangeFormattingName)]
    internal class TypeScriptFormatDocumentRangeHandlerShim : FormatDocumentRangeHandlerOnMainThreadShim
    {
        [ImportingConstructor]
        public TypeScriptFormatDocumentRangeHandlerShim([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, IThreadingContext threadingContext) : base(requestHandlers, threadingContext)
        {
        }
    }
}
