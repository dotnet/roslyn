// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(ProjectContextChangedHandler)), Shared]
[Method(VSMethods.GetProjectContextsName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class ProjectContextChangedHandler() : ILspServiceNotificationHandler<TextDocumentIdentifier?>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public Task HandleNotificationAsync(TextDocumentIdentifier? request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var refresher = requestContext.GetRequiredService<IFeatureProviderRefresher>();
        refresher.RequestProviderRefresh(request?.DocumentUri);
        return Task.CompletedTask;
    }
}
