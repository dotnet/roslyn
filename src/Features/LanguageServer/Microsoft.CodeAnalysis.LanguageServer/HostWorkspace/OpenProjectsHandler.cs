// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicStatelessLspService(typeof(OpenProjectHandler)), Shared]
[Method("project/open")]
internal class OpenProjectHandler : ILspServiceNotificationHandler<OpenProjectHandler.NotificationParams>
{
    private readonly LanguageServerProjectSystem _projectSystem;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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

    [DataContract]
    private class NotificationParams
    {
        [DataMember(Name = "projects")]
        public required Uri[] Projects { get; set; }
    }
}
