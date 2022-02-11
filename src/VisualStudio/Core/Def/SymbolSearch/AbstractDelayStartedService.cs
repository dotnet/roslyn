// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    /// <summary>
    /// Base type for services that we want to delay running until certain criteria is met.
    /// For example, we don't want to run the <see cref="VisualStudioSymbolSearchService"/> core codepath
    /// if the user has not enabled the features that need it.  That helps us avoid loading
    /// dlls unnecessarily and bloating the VS memory space.
    /// </summary>
    internal abstract class AbstractDelayStartedService : ForegroundThreadAffinitizedObject
    {
        private readonly List<string> _registeredLanguageNames = new();

        private readonly Workspace _workspace;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IGlobalOptionService _globalOptions;

        // Option that controls if this service is enabled or not (regardless of language).
        private readonly Option2<bool> _featureEnabledOption;

        // Options that control if this service is enabled or not for a particular language.
        private readonly ImmutableArray<PerLanguageOption2<bool>> _perLanguageOptions;

        private bool _enabled = false;

        protected CancellationToken DisposalToken { get; }

        protected AbstractDelayStartedService(
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext,
            Workspace workspace,
            Option2<bool> featureEnabledOption,
            ImmutableArray<PerLanguageOption2<bool>> perLanguageOptions)
            : base(threadingContext)
        {
            _workspace = workspace;
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.Workspace);
            _globalOptions = globalOptions;
            _featureEnabledOption = featureEnabledOption;
            _perLanguageOptions = perLanguageOptions;
            DisposalToken = threadingContext.DisposalToken;
        }

        protected abstract Task EnableServiceAsync(CancellationToken cancellationToken);

        protected abstract void StartWorking();

        internal void Connect(string languageName)
        {
            this.AssertIsForeground();

            if (!_globalOptions.GetOption(_featureEnabledOption))
            {
                // Feature is totally disabled.  Do nothing.
                return;
            }

            _registeredLanguageNames.Add(languageName);
            if (_registeredLanguageNames.Count == 1)
            {
                // Register to hear about option changing.
                var optionsService = _workspace.Services.GetRequiredService<IOptionService>();
                optionsService.OptionChanged += OnOptionChanged;
            }

            // Kick things off.
            OnOptionChanged(this, EventArgs.Empty);
        }

        private void OnOptionChanged(object sender, EventArgs e)
        {
            this.AssertIsForeground();

            if (!_registeredLanguageNames.Any(IsRegisteredForLanguage))
            {
                // The feature is not enabled for any registered languages.
                return;
            }

            var asyncToken = _asyncListener.BeginAsyncOperation(nameof(AbstractDelayStartedService.EnableServiceAsync), tag: GetType());
            var enableAsync = ThreadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                // The first time we see that we're registered for a language, enable the
                // service.
                if (!_enabled)
                {
                    _enabled = true;
                    await EnableServiceAsync(ThreadingContext.DisposalToken).ConfigureAwait(true);
                }

                // Then tell it to start work.
                StartWorking();
            });

            enableAsync.Task.CompletesAsyncOperation(asyncToken);
        }

        private bool IsRegisteredForLanguage(string language)
            => _perLanguageOptions.Any(option => _globalOptions.GetOption(option, language));
    }
}
