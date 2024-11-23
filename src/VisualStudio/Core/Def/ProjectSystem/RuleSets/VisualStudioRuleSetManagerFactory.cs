// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

[ExportWorkspaceServiceFactory(typeof(IRuleSetManager), ServiceLayer.Host), Shared]
internal sealed class VisualStudioRuleSetManagerFactory : IWorkspaceServiceFactory
{
    private readonly IThreadingContext _threadingContext;
    private readonly FileChangeWatcherProvider _fileChangeWatcherProvider;
    private readonly IAsynchronousOperationListener _listener;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioRuleSetManagerFactory(
        IThreadingContext threadingContext,
        FileChangeWatcherProvider fileChangeWatcherProvider,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _threadingContext = threadingContext;
        _fileChangeWatcherProvider = fileChangeWatcherProvider;
        _listener = listenerProvider.GetListener(FeatureAttribute.RuleSetEditor);
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new VisualStudioRuleSetManager(_threadingContext, _fileChangeWatcherProvider.Watcher, _listener);
}
