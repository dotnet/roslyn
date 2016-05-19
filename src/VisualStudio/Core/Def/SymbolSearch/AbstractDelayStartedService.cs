using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal abstract class AbstractDelayStartedService : ForegroundThreadAffinitizedObject
    {
        protected readonly List<string> RegisteredLanguageNames = new List<string>();

        protected readonly Workspace Workspace;

        // Option that controls if this service is enabled or not (regardless of language).
        private readonly Option<bool> _serviceOnOffOption;

        // Options that control if this service is enabled or not for a particular language.
        private readonly ImmutableArray<PerLanguageOption<bool>> _perLanguageOptions;

        protected AbstractDelayStartedService(
            Workspace workspace,
            Option<bool> onOffOption,
            params PerLanguageOption<bool>[] perLanguageOptions)
        {
            Workspace = workspace;
            _serviceOnOffOption = onOffOption;
            _perLanguageOptions = perLanguageOptions.ToImmutableArray();
        }

        protected abstract void StartWork();
        protected abstract void StopWork();

        protected abstract void ConnectToAdditionalEventSources();
        protected abstract void DisconnectFromAdditionalEventSources();

        internal void Start(string languageName)
        {
            this.AssertIsForeground();

            this.RegisteredLanguageNames.Add(languageName);
            if (this.RegisteredLanguageNames.Count == 1)
            {
                // When the first language registers, start the service.

                var options = Workspace.Options;
                if (!options.GetOption(_serviceOnOffOption))
                {
                    // Feature is totally disabled.  Do nothing.
                    return;
                }

                // Register to hear about option changing.
                var optionsService = Workspace.Services.GetService<IOptionService>();
                optionsService.OptionChanged += OnOptionChanged;

                // Start the whole process once we're connected
                ConnectToAdditionalEventSources();
            }

            // Kick things off.
            OnOptionChanged(this, EventArgs.Empty);
        }

        protected void OnOptionChanged(object sender, EventArgs e)
        {
            if (!RegisteredLanguageNames.Any(IsRegisteredForLanguage))
            {
                // The feature is not enabled for any registered languages.
                return;
            }

            StartWork();
        }

        private bool IsRegisteredForLanguage(string language)
        {
            var options = Workspace.Options;
            return _perLanguageOptions.Any(o => options.GetOption(o, language));
        }

        internal void Stop(string languageName)
        {
            RegisteredLanguageNames.Remove(languageName);
            if (RegisteredLanguageNames.Count == 0)
            {
                // once there are no more languages registered, we can actually stop this service.

                var optionsService = Workspace.Services.GetService<IOptionService>();
                optionsService.OptionChanged -= OnOptionChanged;

                DisconnectFromAdditionalEventSources();
                StopWork();
            }
        }
    }
}