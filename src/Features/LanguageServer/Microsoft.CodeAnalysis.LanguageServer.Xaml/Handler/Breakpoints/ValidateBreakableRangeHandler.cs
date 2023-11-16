// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlStatelessLspService(typeof(ValidateBreakableRangeHandler)), Shared]
[XamlMethod(LSP.VSInternalMethods.TextDocumentValidateBreakableRangeName)]
internal sealed class ValidateBreakableRangeHandler : XamlRequestHandlerBase<LSP.VSInternalValidateBreakableRangeParams, LSP.Range?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ValidateBreakableRangeHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.VSInternalValidateBreakableRangeParams, LSP.Range?> xamlHandler)
        : base(xamlHandler)
    {
    }

    public override LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.VSInternalValidateBreakableRangeParams request)
        => request.TextDocument;
}
