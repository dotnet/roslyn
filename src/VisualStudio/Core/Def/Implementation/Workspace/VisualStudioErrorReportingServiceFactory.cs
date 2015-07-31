// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IErrorReportingService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioErrorReportingServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IErrorReportingService _singleton;

        [ImportingConstructor]
        public VisualStudioErrorReportingServiceFactory(
            VisualStudioWorkspaceImpl workspace,
            IForegroundNotificationService foregroundNotificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _singleton = new VisualStudioErrorReportingService(workspace, foregroundNotificationService, new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.InfoBar));
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton;
        }
    }
}
