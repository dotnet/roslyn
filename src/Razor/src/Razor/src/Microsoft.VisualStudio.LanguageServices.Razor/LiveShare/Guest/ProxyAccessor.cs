// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Razor.LiveShare.Guest;

[Export(typeof(IProxyAccessor))]
[method: ImportingConstructor]
internal class ProxyAccessor(
    ILiveShareSessionAccessor liveShareSessionAccessor,
    JoinableTaskContext joinableTaskContext) : IProxyAccessor
{
    private readonly ILiveShareSessionAccessor _liveShareSessionAccessor = liveShareSessionAccessor;
    private readonly JoinableTaskFactory _jtf = joinableTaskContext.Factory;

    private IProjectHierarchyProxy? _projectHierarchyProxy;

    public IProjectHierarchyProxy GetProjectHierarchyProxy()
        => _projectHierarchyProxy ??= CreateServiceProxy<IProjectHierarchyProxy>();

    private TProxy CreateServiceProxy<TProxy>() where TProxy : class
    {
        Assumes.NotNull(_liveShareSessionAccessor.Session);

        return _jtf.Run(
            () => _liveShareSessionAccessor.Session.GetRemoteServiceAsync<TProxy>(typeof(TProxy).Name, CancellationToken.None));
    }
}
