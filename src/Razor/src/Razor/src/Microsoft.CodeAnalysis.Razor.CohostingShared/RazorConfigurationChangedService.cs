// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.CohostingShared;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[ExportRazorLspService(typeof(RazorConfigurationChangedService)), Shared(LspServiceComposition.SharingBoundary)]
[method: ImportingConstructor]
internal sealed class RazorConfigurationChangedService(
    [Import(AllowDefault = true)] Lazy<ICohostConfigurationChangedService>? cohostConfigurationChangedService) : ILspService, IOnConfigurationChanged
#pragma warning restore RS0030 // Do not use banned APIs
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
