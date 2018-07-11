// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Hosting.Diagnostics.Waiters;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class VisualStudioWorkspace_InProc2 : InProcComponent2
    {
        private static readonly Guid RoslynPackageId = new Guid("6cf2e545-6109-4730-8883-cf43d7aec3e1");
        private VisualStudioWorkspace _visualStudioWorkspace;

        public VisualStudioWorkspace_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task InitializeAsync()
        {
            // we need to enable waiting service before we create workspace
            (await GetWaitingServiceAsync()).Enable(true);

            _visualStudioWorkspace = await GetComponentModelServiceAsync<VisualStudioWorkspace>();
        }

        public async Task SetOptionInferAsync(string projectName, bool value)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var convertedValue = value ? 1 : 0;
            var project = await GetProjectAsync(projectName);
            project.Properties.Item("OptionInfer").Value = convertedValue;

            await WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

        private async Task<EnvDTE.Project> GetProjectAsync(string nameOrFileName)
        {
            return (await GetDTEAsync()).Solution.Projects.OfType<EnvDTE.Project>().First(p =>
                string.Equals(p.FileName, nameOrFileName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.Name, nameOrFileName, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsUseSuggestionModeOn()
            => _visualStudioWorkspace.Options.GetOption(EditorCompletionOptions.UseSuggestionMode);

        public async Task SetUseSuggestionModeAsync(bool value)
        {
            if (IsUseSuggestionModeOn() != value)
            {
                await ExecuteCommandAsync(WellKnownCommandNames.Edit_ToggleCompletionMode);
            }
        }

        public bool IsPrettyListingOn(string languageName)
            => _visualStudioWorkspace.Options.GetOption(FeatureOnOffOptions.PrettyListing, languageName);

        public async Task SetPrettyListingAsync(string languageName, bool value)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            _visualStudioWorkspace.Options = _visualStudioWorkspace.Options.WithChangedOption(
                FeatureOnOffOptions.PrettyListing, languageName, value);
        }

        public async Task EnableQuickInfoAsync(bool value)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            _visualStudioWorkspace.Options = _visualStudioWorkspace.Options.WithChangedOption(
                InternalFeatureOnOffOptions.QuickInfo, value);
        }

        public async Task SetFullSolutionAnalysisAsync(bool value)
        {
            await SetPerLanguageOptionAsync(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, value);
            await SetPerLanguageOptionAsync(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.VisualBasic, value);
        }

        public async Task SetPerLanguageOptionAsync<T>(PerLanguageOption<T> option, string language, T value)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var optionService = _visualStudioWorkspace.Services.GetService<IOptionService>();
            var optionKey = new OptionKey(option, language);
            optionService.SetOptions(optionService.GetOptions().WithChangedOption(option, language, value));
        }

        public async Task SetOptionAsync<T>(Option<T> option, T value)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var optionService = _visualStudioWorkspace.Services.GetService<IOptionService>();
            optionService.SetOptions(optionService.GetOptions().WithChangedOption(option, value));
        }

        private async Task<TestingOnly_WaitingService> GetWaitingServiceAsync()
        {
            return (await GetComponentModelAsync()).DefaultExportProvider.GetExport<TestingOnly_WaitingService>().Value;
        }

        public async Task WaitForAsyncOperationsAsync(string featuresToWaitFor, bool waitForWorkspaceFirst = true)
        {
            await (await GetWaitingServiceAsync()).WaitForAsyncOperationsAsync(featuresToWaitFor, waitForWorkspaceFirst);
        }

        public async Task WaitForAllAsyncOperationsAsync(params string[] featureNames)
            => await (await GetWaitingServiceAsync()).WaitForAllAsyncOperationsAsync(featureNames);

        private async Task LoadRoslynPackageAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var roslynPackageGuid = RoslynPackageId;
            var vsShell = await GetGlobalServiceAsync<SVsShell, IVsShell>();

            ErrorHandler.ThrowOnFailure(vsShell.LoadPackage(ref roslynPackageGuid, out var roslynPackage));
        }

        public async Task CleanUpWorkspaceAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await LoadRoslynPackageAsync();
            _visualStudioWorkspace.TestHookPartialSolutionsDisabled = true;

            var textManager = await GetGlobalServiceAsync<SVsTextManager, IVsTextManager6>();
            var viewPreferences = new[] { default(VIEWPREFERENCES5) };
            ErrorHandler.ThrowOnFailure(textManager.GetUserPreferences6(viewPreferences, null, null));
            viewPreferences[0].fShowBlockStructure = 0;
            ErrorHandler.ThrowOnFailure(textManager.SetUserPreferences6(viewPreferences, null, null));

            // Prepare to reset all options. We explicitly read all option values from the OptionSet to ensure all
            // values changed from the defaults are detected.
            var optionService = _visualStudioWorkspace.Services.GetRequiredService<IOptionService>();
            var optionSet = optionService.GetOptions();
            await ReadAllOptionValuesAsync(optionSet);
            foreach (var changedOptionKey in optionSet.GetChangedOptions(DefaultValueOptionSet.Instance).ToArray())
            {
                optionSet = optionSet.WithChangedOption(changedOptionKey, changedOptionKey.Option.DefaultValue);
            }

            // Options overrides used for testing
            optionSet = optionSet.WithChangedOption(CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain, LanguageNames.CSharp, true);
            optionSet = optionSet.WithChangedOption(CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain, LanguageNames.VisualBasic, true);

            optionService.SetOptions(optionSet);
        }

        private async Task ReadAllOptionValuesAsync(OptionSet optionSet)
        {
            foreach (var optionProvider in (await GetComponentModelAsync()).GetExtensions<IOptionProvider>())
            {
                foreach (var option in optionProvider.Options)
                {
                    if (option.IsPerLanguage)
                    {
                        _ = optionSet.GetOption(new OptionKey(option, LanguageNames.CSharp));
                        _ = optionSet.GetOption(new OptionKey(option, LanguageNames.VisualBasic));
                    }
                    else
                    {
                        _ = optionSet.GetOption(new OptionKey(option));
                    }
                }
            }
        }

        public async Task CleanUpWaitingServiceAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var provider = (await GetComponentModelAsync()).DefaultExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

            if (provider == null)
            {
                throw new InvalidOperationException("The test waiting service could not be located.");
            }

            (await GetWaitingServiceAsync()).EnableActiveTokenTracking(true);
        }

        [Obsolete("Use the type safe overloads instead.")]
        public async Task SetFeatureOptionAsync(string feature, string optionName, string language, string valueString)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var optionService = _visualStudioWorkspace.Services.GetRequiredService<IOptionService>();
            var option = optionService.GetRegisteredOptions().FirstOrDefault(o => o.Feature == feature && o.Name == optionName);
            if (option == null)
            {
                throw new InvalidOperationException($"Failed to find option with feature name '{feature}' and option name '{optionName}'");
            }

            var value = TypeDescriptor.GetConverter(option.Type).ConvertFromString(valueString);
            var optionKey = string.IsNullOrWhiteSpace(language)
                ? new OptionKey(option)
                : new OptionKey(option, language);

            optionService.SetOptions(optionService.GetOptions().WithChangedOption(optionKey, value));
        }

        public async Task SetFeatureOptionAsync<T>(PerLanguageOption<T> option, string language, T value)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var optionService = _visualStudioWorkspace.Services.GetService<IOptionService>();
            optionService.SetOptions(optionService.GetOptions().WithChangedOption(option, language, value));
        }

        /// <summary>
        /// An implementation of <see cref="OptionSet"/> that returns the default values for all options.
        /// </summary>
        private class DefaultValueOptionSet : OptionSet
        {
            public static readonly DefaultValueOptionSet Instance = new DefaultValueOptionSet();

            private DefaultValueOptionSet()
            {
            }

            public override object GetOption(OptionKey optionKey)
            {
                return optionKey.Option.DefaultValue;
            }

            public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
            {
                throw new NotSupportedException();
            }

            internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
            {
                if (optionSet == this)
                {
                    return Enumerable.Empty<OptionKey>();
                }

                return optionSet.GetChangedOptions(this);
            }
        }

    }
}

namespace Microsoft.VisualStudio.TextManager.Interop
{
    [Guid("A50CF306-7BEE-4349-8789-DAE896A15E07"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IVsTextManager6
    {
        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        int GetUserPreferences6([MarshalAs(UnmanagedType.LPArray)] [Out] VIEWPREFERENCES5[] pViewPrefs, [MarshalAs(UnmanagedType.LPArray)] [In] [Out] LANGPREFERENCES3[] pLangPrefs, [MarshalAs(UnmanagedType.LPArray)] [In] [Out] FONTCOLORPREFERENCES2[] pColorPrefs);

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        int SetUserPreferences6([MarshalAs(UnmanagedType.LPArray)] [In] VIEWPREFERENCES5[] pViewPrefs, [MarshalAs(UnmanagedType.LPArray)] [In] LANGPREFERENCES3[] pLangPrefs, [MarshalAs(UnmanagedType.LPArray)] [In] FONTCOLORPREFERENCES2[] pColorPrefs);
    }

    [TypeIdentifier("96B36253-76A4-4DF5-9071-34CD1B5A5EFF", "Microsoft.VisualStudio.TextManager.Interop.VIEWPREFERENCES5")]
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct VIEWPREFERENCES5
    {
        public uint fVisibleWhitespace;
        public uint fSelectionMargin;
        public uint fAutoDelimiterHighlight;
        public uint fGoToAnchorAfterEscape;
        public uint fDragDropEditing;
        public uint fUndoCaretMovements;
        public uint fOvertype;
        public uint fDragDropMove;
        public uint fWidgetMargin;
        public uint fReadOnly;
        public uint fActiveInModalState;
        public uint fClientDragDropFeedback;
        public uint fTrackChanges;
        public uint uCompletorSize;
        public uint fDetectUTF8;
        public int lEditorEmulation;
        public uint fHighlightCurrentLine;
        public uint fShowBlockStructure;
        public uint fEnableCodingConventions;
        public uint fEnableClickGotoDef;
        public uint uModifierKey;
        public uint fOpenDefInPeek;
    }
}
