// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Hosting.Diagnostics.Waiters;

namespace Roslyn.VisualStudio.Test.Utilities.InProcess
{
    internal class VisualStudioWorkspace_InProc : InProcComponent
    {
        private static readonly Guid RoslynPackageId = new Guid("6cf2e545-6109-4730-8883-cf43d7aec3e1");
        private readonly VisualStudioWorkspace _visualStudioWorkspace;

        private VisualStudioWorkspace_InProc()
        {
            _visualStudioWorkspace = GetComponentModelService<VisualStudioWorkspace>();
        }

        public static VisualStudioWorkspace_InProc Create()
        {
            return new VisualStudioWorkspace_InProc();
        }

        public bool IsUseSuggestionModeOn()
        {
            return _visualStudioWorkspace.Options.GetOption(EditorCompletionOptions.UseSuggestionMode);
        }

        public void SetUseSuggestionMode(bool value)
        {
            if (IsUseSuggestionModeOn() != value)
            {
                ExecuteCommand(WellKnownCommandNames.ToggleCompletionMode);
            }
        }

        public bool IsPrettyListingOn(string languageName)
        {
            return _visualStudioWorkspace.Options.GetOption(FeatureOnOffOptions.PrettyListing, languageName);
        }

        public void SetPrettyListing(string languageName, bool value)
        {
            InvokeOnUIThread(() =>
            {
                _visualStudioWorkspace.Options = _visualStudioWorkspace.Options.WithChangedOption(
                    FeatureOnOffOptions.PrettyListing, languageName, value);
            });
        }

        private static TestingOnly_WaitingService GetWaitingService()
        {
            return GetComponentModel().DefaultExportProvider.GetExport<TestingOnly_WaitingService>().Value;
        }

        public void WaitForAsyncOperations(string featuresToWaitFor, bool waitForWorkspaceFirst = true)
        {
            GetWaitingService().WaitForAsyncOperations(featuresToWaitFor, waitForWorkspaceFirst);
        }

        public void WaitForAllAsyncOperations()
        {
            GetWaitingService().WaitForAllAsyncOperations();
        }

        private static void LoadRoslynPackage()
        {
            var roslynPackageGuid = RoslynPackageId;
            IVsPackage roslynPackage = null;

            var vsShell = GetGlobalService<SVsShell, IVsShell>();
            var hresult = vsShell.LoadPackage(ref roslynPackageGuid, out roslynPackage);
            Marshal.ThrowExceptionForHR(hresult);
        }

        public void CleanUpWorkspace()
        {
            InvokeOnUIThread(() =>
            {
                LoadRoslynPackage();
                _visualStudioWorkspace.TestHookPartialSolutionsDisabled = true;
            });
        }

        public void CleanUpWaitingService()
        {
            InvokeOnUIThread(() =>
            {
                var asynchronousOperationWaiterExports = GetComponentModel().DefaultExportProvider.GetExports<IAsynchronousOperationWaiter>();

                if (!asynchronousOperationWaiterExports.Any())
                {
                    throw new InvalidOperationException("The test waiting service could not be located.");
                }

                GetWaitingService().EnableActiveTokenTracking(true);
            });
        }
    }
}
