// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Storage;

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

        public static readonly Option2<bool> DiagnosticCache = new(
            nameof(InternalFeatureOnOffOptions), nameof(DiagnosticCache), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(StorageOptions.LocalRegistryPath + nameof(DiagnosticCache)));

        private static bool IsDiagnosticCacheEnabled(HostWorkspaceServices services)
            => services.GetRequiredService<IOptionService>().GetOption(DiagnosticCache) ||
               services.GetRequiredService<IExperimentationService>().IsExperimentEnabled(WellKnownExperimentNames.DiagnosticCache);

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace is not VisualStudioWorkspace vsWorkspace)
            {
                return new NoOpDiagnosticCacheService();
            }

            if (!IsDiagnosticCacheEnabled(workspaceServices))
            {
                return new NoOpDiagnosticCacheService();
            }

            return new VisualStudioDiagnosticCacheService(vsWorkspace, _diagnosticService, _registrationService, _listenerProvider);
        }

        private class NoOpDiagnosticCacheService : IDiagnosticCacheService
        {
#pragma warning disable CS0067
            public event EventHandler<DiagnosticsUpdatedArgs>? CachedDiagnosticsUpdated;
#pragma warning restore CS0067

            public Task LoadCachedDiagnosticsAsync(Document document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public bool TryGetLoadedCachedDiagnostics(DocumentId documentId, out ImmutableArray<DiagnosticData> cachedDiagnostics)
            {
                cachedDiagnostics = default;
                return false;
            }
        }
    }
}
