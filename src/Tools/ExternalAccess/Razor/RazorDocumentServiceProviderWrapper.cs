// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable annotations

using System;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorDocumentServiceProviderWrapper : IDocumentServiceProvider, IDocumentOperationService
    {
        private readonly IRazorDocumentServiceProvider _innerDocumentServiceProvider;
        private readonly object _lock;

        private RazorSpanMappingServiceWrapper? _spanMappingService;
        private RazorDocumentExcerptServiceWrapper? _excerptService;
        private RazorDocumentPropertiesServiceWrapper? _documentPropertiesService;
        private RazorDocumentOptionSetProviderWrapper? _documentOptionSetProvider;

        public RazorDocumentServiceProviderWrapper(IRazorDocumentServiceProvider innerDocumentServiceProvider)
        {
            _innerDocumentServiceProvider = innerDocumentServiceProvider ?? throw new ArgumentNullException(nameof(innerDocumentServiceProvider));

            _lock = new object();
        }

        public bool CanApplyChange => _innerDocumentServiceProvider.CanApplyChange;

        public bool SupportDiagnostics => _innerDocumentServiceProvider.SupportDiagnostics;

        public TService? GetService<TService>() where TService : class, IDocumentService
        {
            if (typeof(TService) == typeof(ISpanMappingService))
            {
                if (_spanMappingService == null)
                {
                    lock (_lock)
                    {
                        if (_spanMappingService == null)
                        {
                            var razorMappingService = _innerDocumentServiceProvider.GetService<IRazorSpanMappingService>();
                            if (razorMappingService != null)
                            {
                                _spanMappingService = new RazorSpanMappingServiceWrapper(razorMappingService);
                            }
                            else
                            {
                                return this as TService;
                            }
                        }
                    }
                }

                return (TService)(object)_spanMappingService;
            }

            if (typeof(TService) == typeof(IDocumentExcerptService))
            {
                if (_excerptService == null)
                {
                    lock (_lock)
                    {
                        if (_excerptService == null)
                        {
                            var excerptService = _innerDocumentServiceProvider.GetService<IRazorDocumentExcerptService>();
                            if (excerptService != null)
                            {
                                _excerptService = new RazorDocumentExcerptServiceWrapper(excerptService);
                            }
                            else
                            {
                                return this as TService;
                            }
                        }
                    }
                }

                return (TService)(object)_excerptService;
            }

            if (typeof(TService) == typeof(DocumentPropertiesService))
            {
                if (_documentPropertiesService == null)
                {
                    lock (_lock)
                    {
                        if (_documentPropertiesService == null)
                        {
                            var documentPropertiesService = _innerDocumentServiceProvider.GetService<IRazorDocumentPropertiesService>();

                            if (documentPropertiesService != null)
                            {
                                _documentPropertiesService = new RazorDocumentPropertiesServiceWrapper(documentPropertiesService);
                            }
                            else
                            {
                                return this as TService;
                            }
                        }
                    }
                }

                return (TService)(object)_documentPropertiesService;
            }

            if (typeof(TService) == typeof(IDocumentOptionSetProvider))
            {
                if (_documentOptionSetProvider == null)
                {
                    lock (_lock)
                    {
                        if (_documentOptionSetProvider == null)
                        {
                            var razorOptionSetProvider = _innerDocumentServiceProvider.GetService<IRazorDocumentOptionSetProvider>();
                            if (razorOptionSetProvider != null)
                            {
                                _documentOptionSetProvider = new RazorDocumentOptionSetProviderWrapper(razorOptionSetProvider);
                            }
                            else
                            {
                                return this as TService;
                            }
                        }
                    }
                }

                return (TService)(object)_documentOptionSetProvider;
            }

            return this as TService;
        }
    }
}
