// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    [ExportWorkspaceServiceFactory(typeof(ExternalErrorDiagnosticUpdateSource), ServiceLayer.Host)]
    [Shared]
    internal sealed class ExternalErrorDiagnosticUpdateSourceFactory : IWorkspaceServiceFactory
    {
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IDiagnosticUpdateSourceRegistrationService _registrationService;
        private readonly IGlobalOperationNotificationService _notificationService;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExternalErrorDiagnosticUpdateSourceFactory(
            IDiagnosticAnalyzerService diagnosticService,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IGlobalOperationNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext)
        {
            _diagnosticService = diagnosticService;
            _registrationService = registrationService;
            _notificationService = notificationService;
            _listenerProvider = listenerProvider;
            _threadingContext = threadingContext;
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new ExternalErrorDiagnosticUpdateSource(workspaceServices.Workspace, _diagnosticService, _registrationService, _notificationService, _listenerProvider, _threadingContext);
        }
    }
}
