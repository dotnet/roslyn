// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly IServiceProvider _serviceProvider;
        private readonly IForegroundNotificationService _foregroundNotificationService;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        public VisualStudioRuleSetManagerFactory(
            SVsServiceProvider serviceProvider,
            IForegroundNotificationService foregroundNotificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _serviceProvider = serviceProvider;
            _foregroundNotificationService = foregroundNotificationService;
            _listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.RuleSetEditor);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            IVsFileChangeEx fileChangeService = (IVsFileChangeEx)_serviceProvider.GetService(typeof(SVsFileChangeEx));
            return new VisualStudioRuleSetManager(fileChangeService, _foregroundNotificationService, _listener);
        }
    }
}
