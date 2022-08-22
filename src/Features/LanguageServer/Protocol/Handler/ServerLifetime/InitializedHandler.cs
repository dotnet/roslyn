// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportGeneralStatelessLspService(typeof(InitializedHandler)), Shared]
[Method(Methods.InitializedName)]
internal class InitializedHandler : ILspServiceNotificationHandler<InitializedParams>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InitializedHandler()
    {
    }

    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => false;

    public Task HandleNotificationAsync(InitializedParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        if (requestContext.ClientCapabilities is null)
        {
            throw new InvalidOperationException($"{Methods.InitializedName} called before {Methods.InitializeName}");
        }

        return Task.CompletedTask;
    }
}
