// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

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
        var resolveDataService = new ResolveCachedDataService(resolveDataCache);

        return CreateHandler(_xamlRequestHandler, resolveDataService);
    }

    private sealed class ResolveCachedDataService : IResolveCachedDataService
    {
        private readonly ResolveDataCache _resolveDataCache;

        public ResolveCachedDataService(ResolveDataCache resolveDataCache)
        {
            _resolveDataCache = resolveDataCache ?? throw new ArgumentNullException(nameof(resolveDataCache));
        }

        public object ToResolveData(object data, Uri uri)
            => ResolveDataConversions.ToCachedResolveData(data, uri, _resolveDataCache);

        public (object? data, Uri? uri) FromResolveData(object? lspData)
            => ResolveDataConversions.FromCachedResolveData(lspData, _resolveDataCache);
    }
}
