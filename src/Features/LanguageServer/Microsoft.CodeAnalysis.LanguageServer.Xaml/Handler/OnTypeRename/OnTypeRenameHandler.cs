// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[Export(typeof(OnTypeRenameHandler)), Shared]
[XamlMethod(Methods.TextDocumentLinkedEditingRangeName)]
internal class OnTypeRenameHandler : XamlRequestHandlerBase<LinkedEditingRangeParams, LinkedEditingRanges?>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public OnTypeRenameHandler([Import(AllowDefault = true)] IXamlRequestHandler<LinkedEditingRangeParams, LinkedEditingRanges?> xamlHandler)
        : base(xamlHandler)
    {
    }
    public override TextDocumentIdentifier GetTextDocumentIdentifier(LinkedEditingRangeParams request) => request.TextDocument;
}
