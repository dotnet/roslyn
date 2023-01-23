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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using SAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.SAsyncServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Options
{
    [Export(typeof(IOptionPersisterProvider))]
    [Export(typeof(VisualStudioOptionPersisterProvider))]
    internal sealed class VisualStudioOptionPersisterProvider : IOptionPersisterProvider
    {
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly ILegacyGlobalOptionService _optionService;

        // maps config name to a read fallback:
        private readonly ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> _readFallbacks;

        private VisualStudioOptionPersister? _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioOptionPersisterProvider(
            [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider serviceProvider,
            [ImportMany] IEnumerable<Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> readFallbacks,
            IThreadingContext threadingContext,
            ILegacyGlobalOptionService optionService)
        {
            _serviceProvider = serviceProvider;
            _optionService = optionService;
            _readFallbacks = readFallbacks.ToImmutableDictionary(item => item.Metadata.ConfigName, item => item);
        }

        public async ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
            => _lazyPersister ??=
                new VisualStudioOptionPersister(
                    new VisualStudioSettingsOptionPersister(_optionService, _readFallbacks, await TryGetServiceAsync<SVsSettingsPersistenceManager, ISettingsManager>().ConfigureAwait(true)),
                    await LocalUserRegistryOptionPersister.CreateAsync(_serviceProvider).ConfigureAwait(false),
                    new FeatureFlagPersister(await TryGetServiceAsync<SVsFeatureFlags, IVsFeatureFlags>().ConfigureAwait(false)));

        private async ValueTask<I?> TryGetServiceAsync<T, I>() where I : class
        {
            try
            {
                return (I?)await _serviceProvider.GetServiceAsync(typeof(T)).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                return null;
            }
        }
    }
}
