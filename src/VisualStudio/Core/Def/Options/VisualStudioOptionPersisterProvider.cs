// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Options
{
    [Export(typeof(IOptionPersisterProvider))]
    [Export(typeof(VisualStudioOptionPersisterProvider))]
    internal sealed class VisualStudioOptionPersisterProvider : IOptionPersisterProvider
    {
        private readonly IVsService<ILocalRegistry4> _localRegistryService;
        private readonly IVsService<ISettingsManager> _settingsManagerService;
        private readonly IVsService<IVsFeatureFlags> _featureFlagsService;
        private readonly ILegacyGlobalOptionService _legacyGlobalOptions;

        // maps config name to a read fallback:
        private readonly ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> _readFallbacks;

        // Use vs-threading's JTF-aware AsyncLazy<T>. Ensure only one persister instance is created (even in the face of
        // parallel requests for the value) because the constructor registers global event handler callbacks.
        private readonly Threading.AsyncLazy<IOptionPersister> _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioOptionPersisterProvider(
            IVsService<SLocalRegistry, ILocalRegistry4> localRegistryService,
            IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService,
            IVsService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
            [ImportMany] IEnumerable<Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> readFallbacks,
            IThreadingContext threadingContext,
            ILegacyGlobalOptionService legacyGlobalOptions)
        {
            _localRegistryService = localRegistryService;
            _settingsManagerService = settingsManagerService;
            _featureFlagsService = featureFlagsService;
            _legacyGlobalOptions = legacyGlobalOptions;
            _readFallbacks = readFallbacks.ToImmutableDictionary(item => item.Metadata.ConfigName, item => item);
            _lazyPersister = new Threading.AsyncLazy<IOptionPersister>(() => CreatePersisterAsync(threadingContext.DisposalToken), threadingContext.JoinableTaskFactory);
        }

        public ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
            => new(_lazyPersister.GetValueAsync(cancellationToken));

        private async Task<IOptionPersister> CreatePersisterAsync(CancellationToken cancellationToken)
        {
            // Obtain services before creating instances. This avoids state corruption in the event cancellation is
            // requested (some of the constructors register event handlers that could leak if cancellation occurred
            // in the middle of construction).
            var settingsManager = await _settingsManagerService.GetValueAsync(cancellationToken).ConfigureAwait(true);
            var localRegistry = await _localRegistryService.GetValueAsync(cancellationToken).ConfigureAwait(true);
            var featureFlags = await _featureFlagsService.GetValueOrNullAsync(cancellationToken).ConfigureAwait(true);

            // Cancellation is not allowed after this point
            cancellationToken = CancellationToken.None;

            return new VisualStudioOptionPersister(
                new VisualStudioSettingsOptionPersister(RefreshOption, _readFallbacks, settingsManager),
                LocalUserRegistryOptionPersister.Create(localRegistry),
                new FeatureFlagPersister(featureFlags));
        }

        private void RefreshOption(OptionKey2 optionKey, object? newValue)
        {
            if (_legacyGlobalOptions.GlobalOptions.RefreshOption(optionKey, newValue))
            {
                // We may be updating the values of internally defined public options.
                // Update solution snapshots of all workspaces to reflect the new values.
                _legacyGlobalOptions.UpdateRegisteredWorkspaces();
            }
        }
    }
}
