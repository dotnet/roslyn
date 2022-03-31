// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class StateResetInProcess
    {
        /// <summary>
        /// Contains the persistence slots of tool windows to close between tests.
        /// </summary>
        /// <seealso cref="__VSFPROPID.VSFPROPID_GuidPersistenceSlot"/>
        private static readonly ImmutableHashSet<Guid> s_windowsToClose = ImmutableHashSet.Create(
            FindReferencesWindowInProcess.FindReferencesWindowGuid,
            new Guid(EnvDTE.Constants.vsWindowKindObjectBrowser));

        public async Task ResetGlobalOptionsAsync(CancellationToken cancellationToken)
        {
            var globalOptions = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            ResetOption2(globalOptions, MetadataAsSourceOptionsStorage.NavigateToDecompiledSources);
            ResetOption2(globalOptions, VisualStudioSyntaxTreeConfigurationService.OptionsMetadata.EnableOpeningSourceGeneratedFilesInWorkspace);
            ResetPerLanguageOption(globalOptions, NavigationBarViewOptions.ShowNavigationBar);
            ResetPerLanguageOption2(globalOptions, VisualStudioNavigationOptions.NavigateToObjectBrowser);
            ResetPerLanguageOption2(globalOptions, FeatureOnOffOptions.AddImportsOnPaste);
            ResetPerLanguageOption2(globalOptions, FeatureOnOffOptions.PrettyListing);
            ResetPerLanguageOption2(globalOptions, CompletionViewOptions.EnableArgumentCompletionSnippets);

            static void ResetOption2<T>(IGlobalOptionService globalOptions, Option2<T> option)
            {
                globalOptions.SetGlobalOption(new OptionKey(option, language: null), option.DefaultValue);
            }

            static void ResetPerLanguageOption<T>(IGlobalOptionService globalOptions, PerLanguageOption<T> option)
            {
                globalOptions.SetGlobalOption(new OptionKey(option, LanguageNames.CSharp), option.DefaultValue);
                globalOptions.SetGlobalOption(new OptionKey(option, LanguageNames.VisualBasic), option.DefaultValue);
            }

            static void ResetPerLanguageOption2<T>(IGlobalOptionService globalOptions, PerLanguageOption2<T> option)
            {
                globalOptions.SetGlobalOption(new OptionKey(option, LanguageNames.CSharp), option.DefaultValue);
                globalOptions.SetGlobalOption(new OptionKey(option, LanguageNames.VisualBasic), option.DefaultValue);
            }
        }

        public async Task ResetHostSettingsAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Use default navigation behavior
            await TestServices.Editor.ConfigureAsyncNavigation(AsyncNavigationKind.Default, cancellationToken);

            // Suggestion mode defaults to on for debugger views, and off for other views.
            await TestServices.Editor.SetUseSuggestionModeAsync(forDebuggerTextView: true, true, cancellationToken);
            await TestServices.Editor.SetUseSuggestionModeAsync(forDebuggerTextView: false, false, cancellationToken);

            // Make sure responsive completion doesn't interfere if integration tests run slowly.
            var editorOptionsFactory = await GetComponentModelServiceAsync<IEditorOptionsFactoryService>(cancellationToken);
            var options = editorOptionsFactory.GlobalOptions;
            options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, false);

            var latencyGuardOptionKey = new EditorOptionKey<bool>("EnableTypingLatencyGuard");
            options.SetOptionValue(latencyGuardOptionKey, false);

            // Close any modal windows
            var mainWindow = await TestServices.Shell.GetMainWindowAsync(cancellationToken);
            var modalWindow = IntegrationHelper.GetModalWindowFromParentWindow(mainWindow);
            while (modalWindow != IntPtr.Zero)
            {
                if ("Default IME" == IntegrationHelper.GetTitleForWindow(modalWindow))
                {
                    // "Default IME" shows up as a modal window in some cases where there is no other window blocking
                    // input to Visual Studio.
                    break;
                }

                await TestServices.Input.SendWithoutActivateAsync(VirtualKey.Escape);
                var nextModalWindow = IntegrationHelper.GetModalWindowFromParentWindow(mainWindow);
                if (nextModalWindow == modalWindow)
                {
                    // Don't loop forever if windows aren't closing.
                    break;
                }
            }

            // Close tool windows where desired (see s_windowsToClose)
            await foreach (var window in TestServices.Shell.EnumerateWindowsAsync(__WindowFrameTypeFlags.WINDOWFRAMETYPE_Tool, cancellationToken).WithCancellation(cancellationToken))
            {
                ErrorHandler.ThrowOnFailure(window.GetGuidProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot, out var persistenceSlot));
                if (s_windowsToClose.Contains(persistenceSlot))
                {
                    window.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                }
            }
        }
    }
}
