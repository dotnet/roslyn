// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Hosting.Diagnostics.Waiters;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class VisualStudioWorkspace_InProc : InProcComponent
    {
        private static readonly Guid RoslynPackageId = new Guid("6cf2e545-6109-4730-8883-cf43d7aec3e1");
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private readonly IGlobalOptionService _globalOptions;

        private VisualStudioWorkspace_InProc()
        {
            // we need to enable waiting service before we create workspace
            GetWaitingService().Enable(true);

            _visualStudioWorkspace = GetComponentModelService<VisualStudioWorkspace>();
            _globalOptions = GetComponentModelService<IGlobalOptionService>();
        }

        public static VisualStudioWorkspace_InProc Create()
            => new VisualStudioWorkspace_InProc();

        public void SetOptionInfer(string projectName, bool value)
            => InvokeOnUIThread(cancellationToken =>
            {
                var convertedValue = value ? 1 : 0;
                var project = GetProject(projectName);
                project.Properties.Item("OptionInfer").Value = convertedValue;
            });

        private static EnvDTE.Project GetProject(string nameOrFileName)
            => GetDTE().Solution.Projects.OfType<EnvDTE.Project>().First(p =>
               string.Compare(p.FileName, nameOrFileName, StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(p.Name, nameOrFileName, StringComparison.OrdinalIgnoreCase) == 0);

        public bool IsPrettyListingOn(string languageName)
            => _globalOptions.GetOption(FeatureOnOffOptions.PrettyListing, languageName);

        public void SetPrettyListing(string languageName, bool value)
            => InvokeOnUIThread(cancellationToken =>
            {
                _globalOptions.SetGlobalOption(new OptionKey(FeatureOnOffOptions.PrettyListing, languageName), value);
            });

        public void SetFileScopedNamespaces(bool value)
            => InvokeOnUIThread(cancellationToken =>
            {
                _visualStudioWorkspace.SetOptions(_visualStudioWorkspace.Options.WithChangedOption(
                    new OptionKey(GetOption("NamespaceDeclarations", "CSharpCodeStyleOptions")),
                    new CodeStyleOption2<NamespaceDeclarationPreference>(value
                        ? NamespaceDeclarationPreference.FileScoped
                        : NamespaceDeclarationPreference.BlockScoped,
                        NotificationOption2.Suggestion)));
            });

        public void SetPerLanguageOption(string optionName, string feature, string language, object value)
        {
            var option = GetOption(optionName, feature);
            var result = GetValue(value, option);
            var optionKey = new OptionKey(option, language);
            SetOption(optionKey, result);
        }

        public void SetOption(string optionName, string feature, object value)
        {
            var option = GetOption(optionName, feature);
            var result = GetValue(value, option);
            var optionKey = new OptionKey(option);
            SetOption(optionKey, result);
        }

        public void SetGlobalOption(WellKnownGlobalOption option, string? language, object? value)
            => InvokeOnUIThread(_ => _globalOptions.SetGlobalOption(option.GetKey(language), value));

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

        private IOption GetOption(string optionName, string feature)
        {
            var optionService = _visualStudioWorkspace.Services.GetRequiredService<IOptionService>();
            var option = optionService.GetRegisteredOptions().FirstOrDefault(o => o.Feature == feature && o.Name == optionName);
            if (option == null)
            {
                throw new Exception($"Failed to find option with feature name '{feature}' and option name '{optionName}'");
            }

            return option;
        }

        private void SetOption(OptionKey optionKey, object? result)
            => _visualStudioWorkspace.SetOptions(_visualStudioWorkspace.Options.WithChangedOption(optionKey, result));

        public void WaitForAsyncOperations(TimeSpan timeout, string featuresToWaitFor, bool waitForWorkspaceFirst = true)
        {
            if (waitForWorkspaceFirst || featuresToWaitFor == FeatureAttribute.Workspace)
            {
                WaitForProjectSystem(timeout);
            }

            GetWaitingService().WaitForAsyncOperations(timeout, featuresToWaitFor, waitForWorkspaceFirst);
        }

        public void WaitForAllAsyncOperations(TimeSpan timeout, params string[] featureNames)
        {
            if (featureNames.Contains(FeatureAttribute.Workspace))
            {
                WaitForProjectSystem(timeout);
            }

            GetWaitingService().WaitForAllAsyncOperations(_visualStudioWorkspace, timeout, featureNames);
        }

        public void WaitForAllAsyncOperationsOrFail(TimeSpan timeout, params string[] featureNames)
        {
            try
            {
                WaitForAllAsyncOperations(timeout, featureNames);
            }
            catch (Exception e)
            {
                var listenerProvider = GetComponentModel().DefaultExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();
                var messageBuilder = new StringBuilder("Failed to clean up listeners in a timely manner.");
                foreach (var token in ((AsynchronousOperationListenerProvider)listenerProvider).GetTokens())
                {
                    messageBuilder.AppendLine().Append($"  {token}");
                }

                Environment.FailFast("Terminating test process due to unrecoverable timeout.", new TimeoutException(messageBuilder.ToString(), e));
            }
        }

        private static void WaitForProjectSystem(TimeSpan timeout)
        {
            var operationProgressStatus = InvokeOnUIThread(_ => GetGlobalService<SVsOperationProgress, IVsOperationProgressStatusService>());
            var stageStatus = operationProgressStatus.GetStageStatus(CommonOperationProgressStageIds.Intellisense);
            stageStatus.WaitForCompletionAsync().Wait(timeout);
        }

        private static void LoadRoslynPackage()
        {
            var roslynPackageGuid = RoslynPackageId;
            var vsShell = GetGlobalService<SVsShell, IVsShell>();

            var hresult = vsShell.LoadPackage(ref roslynPackageGuid, out _);
            Marshal.ThrowExceptionForHR(hresult);
        }

        public void CleanUpWorkspace()
            => InvokeOnUIThread(cancellationToken =>
            {
                LoadRoslynPackage();
                _visualStudioWorkspace.TestHookPartialSolutionsDisabled = true;
            });

        /// <summary>
        /// Reset options that are manipulated by integration tests back to their default values.
        /// </summary>
        public void ResetOptions()
        {
            SetFileScopedNamespaces(false);

            ResetOption(CompletionViewOptions.EnableArgumentCompletionSnippets);
            ResetOption(MetadataAsSourceOptionsStorage.NavigateToDecompiledSources);
            return;

            // Local function
            void ResetOption(IOption option)
            {
                if (option is IPerLanguageOption)
                {
                    SetOption(new OptionKey(option, LanguageNames.CSharp), option.DefaultValue);
                    SetOption(new OptionKey(option, LanguageNames.VisualBasic), option.DefaultValue);
                }
                else
                {
                    SetOption(new OptionKey(option), option.DefaultValue);
                }
            }
        }

        public void CleanUpWaitingService()
            => InvokeOnUIThread(cancellationToken =>
            {
                var provider = GetComponentModel().DefaultExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

                if (provider == null)
                {
                    throw new InvalidOperationException("The test waiting service could not be located.");
                }

                GetWaitingService().EnableActiveTokenTracking(true);
            });

        public void SetFeatureOption(string feature, string optionName, string? language, string? valueString)
            => InvokeOnUIThread(cancellationToken =>
            {
                var option = GetOption(optionName, feature);

                var value = TypeDescriptor.GetConverter(option.Type).ConvertFromString(valueString);
                SetOption(new OptionKey(option, language), value);
            });
    }
}
