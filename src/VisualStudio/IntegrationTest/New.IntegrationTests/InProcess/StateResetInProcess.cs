// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Text.Editor;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class StateResetInProcess
    {
        public async Task ResetGlobalOptionsAsync(CancellationToken cancellationToken)
        {
            var globalOptions = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            globalOptions.SetGlobalOption(new OptionKey(NavigationBarViewOptions.ShowNavigationBar, LanguageNames.CSharp), true);
        }

        public async Task ResetHostSettingsAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Suggestion mode defaults to on for debugger views, and off for other views.
            await TestServices.Editor.SetUseSuggestionModeAsync(forDebuggerTextView: true, true, cancellationToken);
            await TestServices.Editor.SetUseSuggestionModeAsync(forDebuggerTextView: false, false, cancellationToken);

            // Make sure responsive completion doesn't interfere if integration tests run slowly.
            var editorOptionsFactory = await GetComponentModelServiceAsync<IEditorOptionsFactoryService>(cancellationToken);
            var options = editorOptionsFactory.GlobalOptions;
            options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, false);

            var latencyGuardOptionKey = new EditorOptionKey<bool>("EnableTypingLatencyGuard");
            options.SetOptionValue(latencyGuardOptionKey, false);
        }
    }
}
