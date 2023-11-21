// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal abstract class XamlRequestHandlerFactoryBase<TRequest, TResponse> : ILspServiceFactory
{
    private readonly IXamlRequestHandler<TRequest, TResponse>? _xamlRequestHandler;

    public XamlRequestHandlerFactoryBase(IXamlRequestHandler<TRequest, TResponse>? xamlRequestHandler)
    {
        _xamlRequestHandler = xamlRequestHandler;
    }

    public abstract XamlRequestHandlerBase<TRequest, TResponse> CreateHandler(IXamlRequestHandler<TRequest, TResponse>? xamlRequestHandler, IResolveCachedDataService resolveDataService);

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var resolveDataCache = lspServices.GetRequiredService<ResolveDataCache>();
        var documentCache = lspServices.GetRequiredService<DocumentCache>();
        var resolveDataService = new ResolveCachedDataService(resolveDataCache, documentCache);

        return CreateHandler(_xamlRequestHandler, resolveDataService);
    }

    private class ResolveCachedDataService : IResolveCachedDataService
    {
        private readonly ResolveDataCache _resolveDataCache;
        private readonly DocumentCache _documentCache;

        public ResolveCachedDataService(ResolveDataCache resolveDataCache, DocumentCache documentCache)
        {
            _resolveDataCache = resolveDataCache ?? throw new ArgumentNullException(nameof(resolveDataCache));
            _documentCache = documentCache ?? throw new ArgumentNullException(nameof(documentCache));
        }

        public object ToResolveData(object data, LSP.TextDocumentIdentifier document)
            => ResolveDataConversions.ToCachedResolveData(data, document, _resolveDataCache, _documentCache);

        public (object? data, LSP.TextDocumentIdentifier? document) FromResolveData(object? lspData)
            => ResolveDataConversions.FromCachedResolveData(lspData, _resolveDataCache, _documentCache);
    }
}
