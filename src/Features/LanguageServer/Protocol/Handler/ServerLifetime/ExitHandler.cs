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

internal class RoslynLifeCycleManager : LifeCycleManager<RequestContext>, ILspService
{
    public RoslynLifeCycleManager(LanguageServerTarget<RequestContext> languageServerTarget) : base(languageServerTarget)
    {
    }
}

[ExportCSharpVisualBasicStatelessLspService(typeof(ExitHandler)), Shared]
[Method(Methods.ExitName)]
internal class ExitHandler : IRoslynNotificationHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExitHandler()
    {
    }

    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => false;

    public Task HandleNotificationAsync(RequestContext requestContext, CancellationToken _)
    {
        if (requestContext.ClientCapabilities is null)
        {
            throw new InvalidOperationException($"{Methods.InitializedName} called before {Methods.InitializeName}");
        }
        var lifeCycleManager = requestContext.GetRequiredLspService<RoslynLifeCycleManager>();
        lifeCycleManager.Exit();

        return Task.CompletedTask;
    }
}
