// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[ExportCSharpVisualBasicLspServiceFactory(typeof(RazorConfigurationChangedService)), Shared]
[method: ImportingConstructor]
internal sealed class RazorConfigurationChangedServiceFactory(
    [Import(AllowDefault = true)] Lazy<ICohostConfigurationChangedService>? cohostConfigurationChangedService) : ILspServiceFactory
#pragma warning restore RS0030 // Do not use banned APIs
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return new RazorConfigurationChangedService(cohostConfigurationChangedService);
    }

    private class RazorConfigurationChangedService(
        Lazy<ICohostConfigurationChangedService>? cohostConfigurationChangedService) : ILspService, IOnConfigurationChanged
    {
        public Task OnConfigurationChangedAsync(RequestContext context, CancellationToken cancellationToken)
        {
            if (context.ServerKind is not (WellKnownLspServerKinds.AlwaysActiveVSLspServer or WellKnownLspServerKinds.CSharpVisualBasicLspServer))
            {
                return Task.CompletedTask;
            }

            if (cohostConfigurationChangedService is null)
            {
                return Task.CompletedTask;
            }

            using var languageScope = context.Logger.CreateLanguageContext(LanguageInfoProvider.RazorLanguageName);
            return cohostConfigurationChangedService.Value.OnConfigurationChangedAsync(context, cancellationToken);
        }
    }
}
