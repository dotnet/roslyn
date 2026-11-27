// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

[Shared]
[Export(typeof(IRazorCohostStartupService))]
[Export(typeof(RazorClientServerManagerProvider))]
[method: ImportingConstructor]
internal class RazorClientServerManagerProvider() : IRazorCohostStartupService
{
    private IClientLanguageServerManager? _razorClientLanguageServerManager;

    public IClientLanguageServerManager? ClientLanguageServerManager => _razorClientLanguageServerManager;

    public int Order => WellKnownStartupOrder.ClientServerManager;

    public Task StartupAsync(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext, CancellationToken cancellationToken)
    {
        _razorClientLanguageServerManager = requestContext.GetRequiredService<IClientLanguageServerManager>();
        return Task.CompletedTask;
    }
}
