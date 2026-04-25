// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;

internal class TestRazorDocumentServiceProvider(IRazorMappingService mappingService) : IRazorDocumentServiceProvider
{
    private readonly IRazorMappingService _mappingService = mappingService;

    public bool CanApplyChange => true;

    public bool SupportDiagnostics => true;

    TService? IRazorDocumentServiceProvider.GetService<TService>() where TService : class
    {
        var serviceType = typeof(TService);

        if (serviceType == typeof(IRazorMappingService))
        {
            return (TService?)_mappingService;
        }

        if (serviceType == typeof(IRazorDocumentPropertiesService))
        {
            return (TService?)(IRazorDocumentPropertiesService)new TestRazorDocumentPropertiesService();
        }

        if (serviceType == typeof(IRazorMappingService))
        {
            return null;
        }

        return (this as TService).AssumeNotNull();
    }

    private class TestRazorDocumentPropertiesService : IRazorDocumentPropertiesService
    {
        public bool DesignTimeOnly => false;

        public string DiagnosticsLspClientName => "RazorCSharp";
    }
}
