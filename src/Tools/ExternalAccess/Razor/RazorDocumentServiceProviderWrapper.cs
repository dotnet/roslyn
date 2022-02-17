// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorDocumentServiceProviderWrapper : IDocumentServiceProvider, IDocumentOperationService
    {
        private readonly IRazorDocumentServiceProvider _innerDocumentServiceProvider;

        // The lazily initialized service fields use StrongBox<T> to explicitly allow null as an initialized value.
        private StrongBox<ISpanMappingService?>? _lazySpanMappingService;
        private StrongBox<IDocumentExcerptService?>? _lazyExcerptService;
        private StrongBox<DocumentPropertiesService?>? _lazyDocumentPropertiesService;

        public RazorDocumentServiceProviderWrapper(IRazorDocumentServiceProvider innerDocumentServiceProvider)
        {
            _innerDocumentServiceProvider = innerDocumentServiceProvider ?? throw new ArgumentNullException(nameof(innerDocumentServiceProvider));
        }

        public bool CanApplyChange => _innerDocumentServiceProvider.CanApplyChange;

        public bool SupportDiagnostics => _innerDocumentServiceProvider.SupportDiagnostics;

        public TService? GetService<TService>() where TService : class, IDocumentService
        {
            var serviceType = typeof(TService);
            if (serviceType == typeof(ISpanMappingService))
            {
                var spanMappingService = LazyInitialization.EnsureInitialized(
                    ref _lazySpanMappingService,
                    static documentServiceProvider =>
                    {
                        var razorMappingService = documentServiceProvider.GetService<IRazorSpanMappingService>();
                        return razorMappingService != null ? new RazorSpanMappingServiceWrapper(razorMappingService) : null;
                    },
                    _innerDocumentServiceProvider);

                return (TService?)spanMappingService;
            }

            if (serviceType == typeof(IDocumentExcerptService))
            {
                var excerptService = LazyInitialization.EnsureInitialized(
                    ref _lazyExcerptService,
                    static documentServiceProvider =>
                    {
                        var impl = documentServiceProvider.GetService<IRazorDocumentExcerptServiceImplementation>();
                        return (impl != null) ? new RazorDocumentExcerptServiceWrapper(impl) : null;
                    },
                    _innerDocumentServiceProvider);

                return (TService?)excerptService;
            }

            if (serviceType == typeof(DocumentPropertiesService))
            {
                var documentPropertiesService = LazyInitialization.EnsureInitialized(
                    ref _lazyDocumentPropertiesService,
                    static documentServiceProvider =>
                    {
                        var razorDocumentPropertiesService = documentServiceProvider.GetService<IRazorDocumentPropertiesService>();
                        return razorDocumentPropertiesService is not null ? new RazorDocumentPropertiesServiceWrapper(razorDocumentPropertiesService) : null;
                    },
                    _innerDocumentServiceProvider);

                return (TService?)(object?)documentPropertiesService;
            }

            return this as TService;
        }
    }
}
