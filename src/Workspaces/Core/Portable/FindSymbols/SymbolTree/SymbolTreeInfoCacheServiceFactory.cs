// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.FindSymbols.SymbolTree;

[ExportWorkspaceServiceFactory(typeof(ISymbolTreeInfoCacheService)), Shared]
internal sealed partial class SymbolTreeInfoCacheServiceFactory : IWorkspaceServiceFactory
{
    private readonly IAsynchronousOperationListener _listener;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SymbolTreeInfoCacheServiceFactory(
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _listener = listenerProvider.GetListener(FeatureAttribute.SolutionCrawlerLegacy);
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new SymbolTreeInfoCacheService(workspaceServices.Workspace, _listener);
}
