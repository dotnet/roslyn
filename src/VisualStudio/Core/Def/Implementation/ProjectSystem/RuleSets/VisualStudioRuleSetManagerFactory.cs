// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [ExportWorkspaceServiceFactory(typeof(VisualStudioRuleSetManager), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioRuleSetManagerFactory : IWorkspaceServiceFactory
    {
        private readonly FileChangeWatcherProvider _fileChangeWatcherProvider;
        private readonly IForegroundNotificationService _foregroundNotificationService;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioRuleSetManagerFactory(
            FileChangeWatcherProvider fileChangeWatcherProvider,
            IForegroundNotificationService foregroundNotificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _fileChangeWatcherProvider = fileChangeWatcherProvider;
            _foregroundNotificationService = foregroundNotificationService;
            _listener = listenerProvider.GetListener(FeatureAttribute.RuleSetEditor);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new VisualStudioRuleSetManager(_fileChangeWatcherProvider.Watcher, _foregroundNotificationService, _listener);
    }
}
