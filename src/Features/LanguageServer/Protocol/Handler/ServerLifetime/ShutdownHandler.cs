﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[Method(Methods.ShutdownName)]
internal class ShutdownHandler : ILspServiceNotificationHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ShutdownHandler()
    {
    }

    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => false;

    public async Task HandleNotificationAsync(RequestContext requestContext, CancellationToken _)
    {
        requestContext.GetRequiredClientCapabilities();

        var lifeCycleManager = requestContext.GetRequiredService<ILifeCycleManager>();
        await lifeCycleManager.ShutdownAsync().ConfigureAwait(false);
    }
}
