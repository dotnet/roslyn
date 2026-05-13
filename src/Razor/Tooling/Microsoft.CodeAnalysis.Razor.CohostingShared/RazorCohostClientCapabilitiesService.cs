// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(IRazorCohostStartupService))]
[Export(typeof(IClientCapabilitiesService))]
internal sealed class RazorCohostClientCapabilitiesService : AbstractClientCapabilitiesService, IRazorCohostStartupService
{
    public int Order => WellKnownStartupOrder.ClientCapabilities;

    public Task StartupAsync(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext, CancellationToken cancellationToken)
    {
        SetCapabilities(clientCapabilities);

        return Task.CompletedTask;
    }
}
