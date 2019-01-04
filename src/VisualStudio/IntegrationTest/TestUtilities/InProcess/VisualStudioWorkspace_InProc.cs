// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Hosting.Diagnostics.Waiters;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class VisualStudioWorkspace_InProc : InProcComponent
    {
        private static readonly Guid RoslynPackageId = new Guid("6cf2e545-6109-4730-8883-cf43d7aec3e1");
        private readonly VisualStudioWorkspace _visualStudioWorkspace;

        private VisualStudioWorkspace_InProc()
        {
            // we need to enable waiting service before we create workspace
            GetWaitingService().Enable(true);

            _visualStudioWorkspace = GetComponentModelService<VisualStudioWorkspace>();
        }

        public static VisualStudioWorkspace_InProc Create()
            => new VisualStudioWorkspace_InProc();

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

        public bool IsUseSuggestionModeOn()
            => _visualStudioWorkspace.Options.GetOption(EditorCompletionOptions.UseSuggestionMode);

        public void SetUseSuggestionMode(bool value)
        {
            if (IsUseSuggestionModeOn() != value)
            {
                ExecuteCommand(WellKnownCommandNames.Edit_ToggleCompletionMode);
            }
        }

        public bool IsPrettyListingOn(string languageName)
            => _visualStudioWorkspace.Options.GetOption(FeatureOnOffOptions.PrettyListing, languageName);

        public void SetPrettyListing(string languageName, bool value)
            => InvokeOnUIThread(() =>
            {
                _visualStudioWorkspace.Options = _visualStudioWorkspace.Options.WithChangedOption(
                    FeatureOnOffOptions.PrettyListing, languageName, value);
            });

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

        private static TestingOnly_WaitingService GetWaitingService()
            => GetComponentModel().DefaultExportProvider.GetExport<TestingOnly_WaitingService>().Value;

        public void WaitForAsyncOperations(string featuresToWaitFor, bool waitForWorkspaceFirst = true)
            => GetWaitingService().WaitForAsyncOperations(featuresToWaitFor, waitForWorkspaceFirst);

        public void WaitForAllAsyncOperations(params string[] featureNames)
            => GetWaitingService().WaitForAllAsyncOperations(featureNames);

        private static void LoadRoslynPackage()
        {
            var roslynPackageGuid = RoslynPackageId;
            var vsShell = GetGlobalService<SVsShell, IVsShell>();

            var hresult = vsShell.LoadPackage(ref roslynPackageGuid, out var roslynPackage);
            Marshal.ThrowExceptionForHR(hresult);
        }

        public void CleanUpWorkspace()
            => InvokeOnUIThread(() =>
            {
                LoadRoslynPackage();
                _visualStudioWorkspace.TestHookPartialSolutionsDisabled = true;
            });

        public void CleanUpWaitingService()
            => InvokeOnUIThread(() =>
            {
                var provider = GetComponentModel().DefaultExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

                if (provider == null)
                {
                    throw new InvalidOperationException("The test waiting service could not be located.");
                }

                GetWaitingService().EnableActiveTokenTracking(true);
            });

        public void SetFeatureOption(string feature, string optionName, string language, string valueString)
            => InvokeOnUIThread(() =>
            {
                var optionService = _visualStudioWorkspace.Services.GetService<IOptionService>();
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
            });

        public string GetWorkingFolder()
        {
            var service = _visualStudioWorkspace.Services.GetRequiredService<IPersistentStorageLocationService>();
            return service.TryGetStorageLocation(_visualStudioWorkspace.CurrentSolution.Id);
        }
    }
}
