// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.LiveShare.Host;

[ExportCollaborationService(
    typeof(IProjectHierarchyProxy),
    Name = nameof(IProjectHierarchyProxy),
    Scope = SessionScope.Host,
    Role = ServiceRole.RemoteService)]
[method: ImportingConstructor]
internal class ProjectHierarchyProxyFactory(
    [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
    JoinableTaskContext joinableTaskContext) : ICollaborationServiceFactory
{
    public Task<ICollaborationService> CreateServiceAsync(CollaborationSession session, CancellationToken cancellationToken)
    {
        var service = new ProjectHierarchyProxy(session, serviceProvider, joinableTaskContext.Factory);
        return Task.FromResult<ICollaborationService>(service);
    }
}
