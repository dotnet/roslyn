﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorDocumentServiceProviderWrapper : IDocumentServiceProvider, IDocumentOperationService
    {
        private readonly IRazorDocumentServiceProvider _innerDocumentServiceProvider;
        private readonly object _lock;

        private RazorSpanMappingServiceWrapper? _spanMappingService;
        private RazorDocumentExcerptServiceWrapper? _excerptService;

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
                            _spanMappingService = new RazorSpanMappingServiceWrapper(razorMappingService);
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
                            _excerptService = new RazorDocumentExcerptServiceWrapper(excerptService);
                        }
                    }
                }

                return (TService)(object)_excerptService;
            }

            return this as TService;
        }
    }
}
