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

        private StrongBox<ISpanMappingService?>? _spanMappingService;
        private StrongBox<IDocumentExcerptService?>? _excerptService;
        private StrongBox<DocumentPropertiesService?>? _documentPropertiesService;

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
                    ref _spanMappingService,
                    static documentServiceProvider =>
                    {
                        var razorMappingService = documentServiceProvider.GetService<IRazorSpanMappingService>();
                        if (razorMappingService != null)
                        {
                            return new StrongBox<ISpanMappingService?>(new RazorSpanMappingServiceWrapper(razorMappingService));
                        }
                        else
                        {
                            return new StrongBox<ISpanMappingService?>(null);
                        }
                    },
                    _innerDocumentServiceProvider);

                return (TService?)spanMappingService.Value;
            }

            if (serviceType == typeof(IDocumentExcerptService))
            {
                var excerptService = LazyInitialization.EnsureInitialized(
                    ref _excerptService,
                    static documentServiceProvider =>
                    {
                        var razorExcerptService = documentServiceProvider.GetService<IRazorDocumentExcerptService>();
                        if (razorExcerptService != null)
                        {
                            return new StrongBox<IDocumentExcerptService?>(new RazorDocumentExcerptServiceWrapper(razorExcerptService));
                        }
                        else
                        {
                            return new StrongBox<IDocumentExcerptService?>(null);
                        }
                    },
                    _innerDocumentServiceProvider);

                return (TService?)excerptService.Value;
            }

            if (serviceType == typeof(DocumentPropertiesService))
            {
                var documentPropertiesService = LazyInitialization.EnsureInitialized(
                    ref _documentPropertiesService,
                    static documentServiceProvider =>
                    {
                        var razorDocumentPropertiesService = documentServiceProvider.GetService<IRazorDocumentPropertiesService>();
                        if (razorDocumentPropertiesService != null)
                        {
                            return new StrongBox<DocumentPropertiesService?>(new RazorDocumentPropertiesServiceWrapper(razorDocumentPropertiesService));
                        }
                        else
                        {
                            return new StrongBox<DocumentPropertiesService?>(null);
                        }
                    },
                    _innerDocumentServiceProvider);

                return (TService?)(object?)documentPropertiesService.Value;
            }

            return this as TService;
        }
    }
}
