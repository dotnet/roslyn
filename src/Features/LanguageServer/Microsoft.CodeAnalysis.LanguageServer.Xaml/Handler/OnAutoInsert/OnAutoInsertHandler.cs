// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(OnAutoInsertHandler)), Shared]
[XamlMethod(VSInternalMethods.OnAutoInsertName)]
internal class OnAutoInsertHandler : XamlRequestHandlerBase<VSInternalDocumentOnAutoInsertParams, VSInternalDocumentOnAutoInsertResponseItem?>
{
    [ImportingConstructor]
    [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
    public OnAutoInsertHandler([Import(AllowDefault = true)] IXamlRequestHandler<VSInternalDocumentOnAutoInsertParams, VSInternalDocumentOnAutoInsertResponseItem?> xamlHandler)
        : base(xamlHandler)
    {
    }

    public override TextDocumentIdentifier GetTextDocumentIdentifier(VSInternalDocumentOnAutoInsertParams request) => request.TextDocument;
}
