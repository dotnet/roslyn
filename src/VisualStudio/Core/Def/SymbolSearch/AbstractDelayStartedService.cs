// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;

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
        private readonly List<string> _registeredLanguageNames = new List<string>();

        protected readonly Workspace Workspace;

        // Option that controls if this service is enabled or not (regardless of language).
        private readonly Option2<bool> _serviceOnOffOption;

        // Options that control if this service is enabled or not for a particular language.
        private readonly ImmutableArray<PerLanguageOption2<bool>> _perLanguageOptions;

        private bool _enabled = false;

        protected CancellationToken DisposalToken { get; }

        protected AbstractDelayStartedService(
            IThreadingContext threadingContext,
            Workspace workspace,
            Option2<bool> onOffOption,
            params PerLanguageOption2<bool>[] perLanguageOptions)
            : base(threadingContext)
        {
            Workspace = workspace;
            _serviceOnOffOption = onOffOption;
            _perLanguageOptions = perLanguageOptions.ToImmutableArray();
            DisposalToken = threadingContext.DisposalToken;
        }

        protected abstract void EnableService();

        protected abstract void StartWorking();

        internal void Connect(string languageName)
        {
            this.AssertIsForeground();

            var options = Workspace.Options;
            if (!options.GetOption(_serviceOnOffOption))
            {
                // Feature is totally disabled.  Do nothing.
                return;
            }

            this._registeredLanguageNames.Add(languageName);
            if (this._registeredLanguageNames.Count == 1)
            {
                // Register to hear about option changing.
                var optionsService = Workspace.Services.GetService<IOptionService>();
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

            // The first time we see that we're registered for a language, enable the
            // service.
            if (!_enabled)
            {
                _enabled = true;
                EnableService();
            }

            // Then tell it to start work.
            StartWorking();
        }

        private bool IsRegisteredForLanguage(string language)
        {
            var options = Workspace.Options;
            return _perLanguageOptions.Any(o => options.GetOption(o, language));
        }
    }
}
