// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.TestHooks;

/// <summary>
/// Implements an LSP request handler to allow the integration tests on the client side to wait for certain server side operations to complete.
/// This is useful in a few cases where the client makes requests to the server but does not wait for the processing to complete, for example
///     1.  Loading projects
///     2.  Reacting to LSP notifications (which are fire and forget)
///
/// This should generally only be used as a last resort when it is impossible for the client to wait specifically for a result it asked for.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(WaitForAsyncOperationsHandler)), Shared]
[Method(MethodName)]
internal class WaitForAsyncOperationsHandler : ILspServiceRequestHandler<WaitForAsyncOperationsParams, WaitForAsyncOperationsResponse>
{
    internal const string MethodName = "workspace/waitForAsyncOperations";

    private readonly AsynchronousOperationListenerProvider _provider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WaitForAsyncOperationsHandler(AsynchronousOperationListenerProvider listenerProvider)
    {
        _provider = listenerProvider;
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<WaitForAsyncOperationsResponse> HandleRequestAsync(WaitForAsyncOperationsParams request, RequestContext context, CancellationToken _)
    {
        context.TraceInformation($"Waiting for {string.Join(", ", request.Operations)} to complete");
        await _provider.WaitAllAsync(context.Solution!.Workspace, request.Operations).ConfigureAwait(false);
        return new WaitForAsyncOperationsResponse();
    }
}

internal record WaitForAsyncOperationsParams([property: JsonPropertyName("operations")] string[] Operations);

internal record WaitForAsyncOperationsResponse();
