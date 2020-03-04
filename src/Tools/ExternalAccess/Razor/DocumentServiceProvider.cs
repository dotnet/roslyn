// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal class DocumentServiceProvider : IDocumentServiceProvider, IDocumentOperationService
    {
        private readonly IRazorDocumentContainer _documentContainer;
        private readonly object _lock;

        private DelegatingRazorSpanMappingService _spanMappingService;
        private DelegatingRazorDocumentExcerptService _excerptService;

        public DocumentServiceProvider()
            : this(null)
        {
        }

        public DocumentServiceProvider(IRazorDocumentContainer documentContainer)
        {
            _documentContainer = documentContainer;

            _lock = new object();
        }

        public bool CanApplyChange => false;

        public bool SupportDiagnostics => false;

        public TService GetService<TService>() where TService : class, IDocumentService
        {
            if (typeof(TService) == typeof(ISpanMappingService))
            {
                if (_spanMappingService == null)
                {
                    lock (_lock)
                    {
                        if (_spanMappingService == null)
                        {
                            var razorMappingService = _documentContainer.GetMappingService();
                            _spanMappingService = new DelegatingRazorSpanMappingService(razorMappingService);
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
                            var excerptService = _documentContainer.GetExcerptService();
                            _excerptService = new DelegatingRazorDocumentExcerptService(excerptService);
                        }
                    }
                }

                return (TService)(object)_excerptService;
            }

            return this as TService;
        }
    }
}
