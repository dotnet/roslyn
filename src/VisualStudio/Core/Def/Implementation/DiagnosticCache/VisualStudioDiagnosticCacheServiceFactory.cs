// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DiagnosticCache
{
    [ExportWorkspaceServiceFactory(typeof(IDiagnosticCacheService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioDiagnosticCacheServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IDiagnosticService _diagnosticService;
        private readonly IDiagnosticUpdateSourceRegistrationService _registrationService;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDiagnosticCacheServiceFactory(
            IDiagnosticService diagnosticService,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _diagnosticService = diagnosticService;
            _registrationService = registrationService;
            _listenerProvider = listenerProvider;
        }

        public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace is not VisualStudioWorkspace vsWorkspace)
            {
                return new NoOpDiagnosticCacheService();
            }

            var experimentationService = workspaceServices.GetRequiredService<IExperimentationService>();
            if (!experimentationService.IsExperimentEnabled(WellKnownExperimentNames.DiagnosticCache))
            {
                return new NoOpDiagnosticCacheService();
            }

            return new VisualStudioDiagnosticCacheService(vsWorkspace, _diagnosticService, _registrationService, _listenerProvider);
        }

        private class NoOpDiagnosticCacheService : IDiagnosticCacheService
        {
            public Task<bool> TryLoadCachedDiagnosticsAsync(Document document, CancellationToken cancellation)
                => Task.FromResult(false);
        }
    }
}
