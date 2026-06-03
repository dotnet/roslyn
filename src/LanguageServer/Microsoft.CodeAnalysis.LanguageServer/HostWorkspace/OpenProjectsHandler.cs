// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicLspServiceFactory(typeof(OpenProjectHandler)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OpenProjectHandlerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new OpenProjectHandler(lspServices.GetRequiredService<LanguageServerProjectSystem>());
}

[Method(OpenProjectName)]
internal sealed class OpenProjectHandler : ILspService, ILspServiceNotificationHandler<OpenProjectHandler.NotificationParams>
{
    internal const string OpenProjectName = "project/open";

    private readonly LanguageServerProjectSystem _projectSystem;

    public OpenProjectHandler(LanguageServerProjectSystem projectSystem)
    {
        _projectSystem = projectSystem;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    Task INotificationHandler<NotificationParams, RequestContext>.HandleNotificationAsync(NotificationParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        return _projectSystem.OpenProjectsAsync(request.Projects.SelectAsArray(p => p.LocalPath));
    }

    internal sealed class NotificationParams
    {
        [JsonPropertyName("projects")]
        public required Uri[] Projects { get; set; }
    }
}
