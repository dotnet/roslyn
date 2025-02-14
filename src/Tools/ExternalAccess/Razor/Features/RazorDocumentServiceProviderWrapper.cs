// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Shared;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

internal sealed class RazorDocumentServiceProviderWrapper : IDocumentServiceProvider, IDocumentOperationService
{
    private readonly IRazorDocumentServiceProvider _innerDocumentServiceProvider;

    // The lazily initialized service fields use StrongBox<T> to explicitly allow null as an initialized value.
    private StrongBox<ISpanMappingService?>? _lazySpanMappingService;

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
            var spanMappingService = InterlockedOperations.Initialize(
                ref _lazySpanMappingService,
                static documentServiceProvider =>
                {
                    // Razor is transitioning implementations from IRazorSpanMappingService to IRazorMappingService.
                    // While this is happening the service may not be available. If it is, use the newer implementation,
                    // otherwise fallback to IRazorSpanMappingService
                    var razorMappingService = documentServiceProvider.GetService<IRazorMappingService>();
                    if (razorMappingService is not null)
                    {
                        return new RazorMappingServiceWrapper(razorMappingService);
                    }

                    return null;
                },
                _innerDocumentServiceProvider);

            return (TService?)spanMappingService;
        }

        return this as TService;
    }
}
