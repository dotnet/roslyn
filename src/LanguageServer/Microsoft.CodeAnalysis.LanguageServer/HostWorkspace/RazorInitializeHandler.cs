// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicStatelessLspService(typeof(RazorInitializeHandler)), Shared]
[Method("razor/initialize")]
internal class RazorInitializeHandler : ILspServiceNotificationHandler<RazorInitializeParams>
{
    private readonly Lazy<RazorWorkspaceListenerInitializer> _razorWorkspaceListenerInitializer;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorInitializeHandler(Lazy<RazorWorkspaceListenerInitializer> razorWorkspaceListenerInitializer)
    {
        _razorWorkspaceListenerInitializer = razorWorkspaceListenerInitializer;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    Task INotificationHandler<RazorInitializeParams, RequestContext>.HandleNotificationAsync(RazorInitializeParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        _razorWorkspaceListenerInitializer.Value.Initialize(request.PipeName);

        return Task.CompletedTask;
    }
}

internal class RazorInitializeParams
{
    [JsonPropertyName("pipeName")]
    public required string PipeName { get; set; }
}
