// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler
{
    [ExportXamlStatelessLspService(typeof(FoldingRangesHandler)), Shared]
    [XamlMethod(Methods.TextDocumentFoldingRangeName)]
    internal class FoldingRangesHandler : XamlRequestHandlerBase<FoldingRangeParams, FoldingRange[]>
    {
        [ImportingConstructor]
        [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
        public FoldingRangesHandler([Import(AllowDefault = true)] IXamlRequestHandler<FoldingRangeParams, FoldingRange[]> xamlHandler)
            : base(xamlHandler)
        {
        }

        public override TextDocumentIdentifier GetTextDocumentIdentifier(FoldingRangeParams request) => request.TextDocument;
    }
}
