// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicStatelessLspService(typeof(OpenSolutionHandler)), Shared]
[Method("solution/open")]
internal class OpenSolutionHandler : ILspServiceNotificationHandler<OpenSolutionHandler.NotificationParams>
{
    private readonly LanguageServerProjectSystem _projectSystem;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public OpenSolutionHandler(LanguageServerProjectSystem projectSystem)
    {
        _projectSystem = projectSystem;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => false;

    Task INotificationHandler<NotificationParams, RequestContext>.HandleNotificationAsync(NotificationParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        return _projectSystem.OpenSolutionAsync(request.Solution.LocalPath);
    }

    private class NotificationParams
    {
        [JsonPropertyName("solution")]
        public required Uri Solution { get; set; }
    }
}
