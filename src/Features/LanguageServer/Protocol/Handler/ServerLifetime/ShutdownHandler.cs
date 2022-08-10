// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportGeneralStatelessLspService(typeof(ShutdownHandler)), Shared]
[Method(Methods.ShutdownName)]
internal class ShutdownHandler : IRoslynNotificationHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ShutdownHandler()
    {
    }

    public bool MutatesSolutionState => true;

    public async Task HandleNotificationAsync(RequestContext requestContext, CancellationToken _)
    {
        if (requestContext.ClientCapabilities is null)
        {
            throw new InvalidOperationException($"{Methods.ShutdownName} called before {Methods.InitializeName}");
        }

        var lifeCycleManager = requestContext.GetRequiredService<LifeCycleManager<RequestContext>>();
        await lifeCycleManager.ShutdownAsync();
    }
}
