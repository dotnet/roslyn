// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[ExportCSharpVisualBasicLspServiceFactory(typeof(RazorConfigurationChangedService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorConfigurationChangedServiceFactory(
    [Import(AllowDefault = true)] Lazy<ICohostConfigurationChangedService>? cohostConfigurationChangedService) : ILspServiceFactory
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

            using var languageScope = context.Logger.CreateLanguageContext(Constants.RazorLanguageName);
            var requestContext = new RazorCohostRequestContext(context);
            return cohostConfigurationChangedService.Value.OnConfigurationChangedAsync(requestContext, cancellationToken);
        }
    }
}
