// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

[ExportXamlLspServiceFactory(typeof(CompletionResolveHandler)), Shared]
internal sealed class CompletionResolveHandlerFactory : ILspServiceFactory
{
    private readonly IXamlRequestHandler<CompletionItem, CompletionItem> _xamlHandler;

    [ImportingConstructor]
    [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
    public CompletionResolveHandlerFactory([Import(AllowDefault = true)] IXamlRequestHandler<LSP.CompletionItem, LSP.CompletionItem> xamlHandler)
    {
        _xamlHandler = xamlHandler;
    }

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var documentCache = lspServices.GetRequiredService<DocumentCache>();
        return new CompletionResolveHandler(_xamlHandler, documentCache);
    }
}
