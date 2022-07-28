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

[ExportCSharpVisualBasicStatelessLspService(typeof(InitializedHandler)), Shared]
[Method(Methods.InitializedName)]
internal class InitializedHandler : IRoslynNotificationHandler<InitializedParams>
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

[ExportCSharpVisualBasicStatelessLspService(typeof(ShutdownHandler)), Shared]
[Method(Methods.ShutdownName)]
internal class ShutdownHandler : IRoslynNotificationHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ShutdownHandler()
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

        ShutdownServer();

        return Task.CompletedTask;
    }

    private void ShutdownServer()
    {
        throw new NotImplementedException("How to shutdown");
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

        Exit();

        return Task.CompletedTask;
    }

    private void Exit()
    {
        throw new NotImplementedException("Exit not implemented");
    }
}

[ExportCSharpVisualBasicStatelessLspService(typeof(InitializeHandler)), Shared]
[Method(Methods.InitializeName)]
internal class InitializeHandler : IRoslynRequestHandler<InitializeParams, InitializeResult>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InitializeHandler()
    {
    }

    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => false;

    public Uri? GetTextDocumentIdentifier(InitializeParams request)
    {
        return null;
    }

    public Task<InitializeResult> HandleRequestAsync(InitializeParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var logger = context.GetRequiredLspService<IRoslynLspLogger>();
        try
        {
            logger.TraceStart("Initialize");

            var clientCapabilitiesManager = context.GetRequiredLspService<IClientCapabilitiesManager>();
            var clientCapabilities = clientCapabilitiesManager.TryGetClientCapabilities();
            if (clientCapabilities != null)
            {
                throw new InvalidOperationException($"{nameof(Methods.InitializeName)} called multiple times");
            }

            clientCapabilities = request.Capabilities;
            clientCapabilitiesManager.SetClientCapabilities(clientCapabilities);

            var capabilitiesProvider = context.GetRequiredLspService<ICapabilitiesProvider>();
            var serverCapabilities = capabilitiesProvider.GetCapabilities(clientCapabilities);

            return Task.FromResult(new InitializeResult
            {
                Capabilities = serverCapabilities,
            });
        }
        finally
        {
            logger.TraceStop("Initialize");
        }
    }
}
