// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
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

#if false
        public void SetOptionInfer(string projectName, bool value)
            => InvokeOnUIThread(() =>
            {
                var convertedValue = value ? 1 : 0;
                var project = GetProject(projectName);
                project.Properties.Item("OptionInfer").Value = convertedValue;
            });

        private EnvDTE.Project GetProject(string nameOrFileName)
            => GetDTE().Solution.Projects.OfType<EnvDTE.Project>().First(p =>
               string.Compare(p.FileName, nameOrFileName, StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(p.Name, nameOrFileName, StringComparison.OrdinalIgnoreCase) == 0);
#endif

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

#if false
        public void EnableQuickInfo(bool value)
            => InvokeOnUIThread(() =>
            {
                _visualStudioWorkspace.Options = _visualStudioWorkspace.Options.WithChangedOption(
                    InternalFeatureOnOffOptions.QuickInfo, value);
            });

        public void SetPerLanguageOption(string optionName, string feature, string language, object value)
        {
            var optionService = _visualStudioWorkspace.Services.GetService<IOptionService>();
            var option = GetOption(optionName, feature, optionService);
            var result = GetValue(value, option);
            var optionKey = new OptionKey(option, language);
            optionService.SetOptions(optionService.GetOptions().WithChangedOption(optionKey, result));
        }

        public void SetOption(string optionName, string feature, object value)
        {
            var optionService = _visualStudioWorkspace.Services.GetService<IOptionService>();
            var option = GetOption(optionName, feature, optionService);
            var result = GetValue(value, option);
            var optionKey = new OptionKey(option);
            optionService.SetOptions(optionService.GetOptions().WithChangedOption(optionKey, result));
        }

        private static object GetValue(object value, IOption option)
        {
            object result;
            if (value is string stringValue)
            {
                result = TypeDescriptor.GetConverter(option.Type).ConvertFromString(stringValue);
            }
            else
            {
                result = value;
            }

            return result;
        }

        private static IOption GetOption(string optionName, string feature, IOptionService optionService)
        {
            var option = optionService.GetRegisteredOptions().FirstOrDefault(o => o.Feature == feature && o.Name == optionName);
            if (option == null)
            {
                throw new Exception($"Failed to find option with feature name '{feature}' and option name '{optionName}'");
            }

            return option;
        }
#endif

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

            // Prepare to reset all options
            var optionService = _visualStudioWorkspace.Services.GetRequiredService<IOptionService>();
            var optionSet = optionService.GetOptions();
            foreach (var changedOptionKey in optionSet.GetChangedOptions(DefaultValueOptionSet.Instance).ToArray())
            {
                optionSet = optionSet.WithChangedOption(changedOptionKey, changedOptionKey.Option.DefaultValue);
            }

            // Options overrides used for testing
            optionSet = optionSet.WithChangedOption(CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain, LanguageNames.CSharp, true);
            optionSet = optionSet.WithChangedOption(CodeCleanupOptions.NeverShowCodeCleanupInfoBarAgain, LanguageNames.VisualBasic, true);

            optionService.SetOptions(optionSet);
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
