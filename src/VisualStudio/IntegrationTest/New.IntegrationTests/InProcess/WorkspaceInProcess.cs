// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    internal partial class WorkspaceInProcess
    {
        private static bool s_initializedAsyncSaveListener;
        private static IVsRunningDocTableEvents? s_runningDocTableEventListener;
#pragma warning disable IDE0052 // Remove unread private members
        private static uint s_runningDocTableEventListenerCookie;
#pragma warning restore IDE0052 // Remove unread private members

        internal static void EnableAsynchronousOperationTracking()
        {
            AsynchronousOperationListenerProvider.Enable(true, diagnostics: true);
        }

        protected override async Task InitializeCoreAsync()
        {
            await base.InitializeCoreAsync();

            if (s_initializedAsyncSaveListener)
                return;

            s_initializedAsyncSaveListener = true;
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var threadingContext = await GetComponentModelServiceAsync<IThreadingContext>(CancellationToken.None);
            var listenerProvider = await GetComponentModelServiceAsync<IAsynchronousOperationListenerProvider>(CancellationToken.None);
            var rdtEvents = await GetRequiredGlobalServiceAsync<SVsRunningDocumentTable, IVsRunningDocumentTable>(CancellationToken.None);
            s_runningDocTableEventListener = new RunningDocumentTableEventListener(threadingContext, listenerProvider.GetListener(FeatureAttribute.Workspace));
            ErrorHandler.ThrowOnFailure(rdtEvents.AdviseRunningDocTableEvents(s_runningDocTableEventListener, out s_runningDocTableEventListenerCookie));
        }

        public async Task<bool> IsPrettyListingOnAsync(string languageName, CancellationToken cancellationToken)
        {
            var globalOptions = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            return globalOptions.GetOption(LineCommitOptionsStorage.PrettyListing, languageName);
        }

        public async Task SetPrettyListingAsync(string languageName, bool value, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var globalOptions = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            globalOptions.SetGlobalOption(LineCommitOptionsStorage.PrettyListing, languageName, value);
        }

        public async Task SetTriggerCompletionInArgumentListsAsync(string languageName, bool value, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var globalOptions = await GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            globalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, languageName, value);
        }

        public async Task SetFileScopedNamespaceAsync(bool value, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);
            globalOptions.SetGlobalOption(Microsoft.CodeAnalysis.CSharp.CodeStyle.CSharpCodeStyleOptions.NamespaceDeclarations,
                new CodeStyleOption2<NamespaceDeclarationPreference>(value ? NamespaceDeclarationPreference.FileScoped : NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Suggestion));
        }

        public Task SetFullSolutionAnalysisAsync(bool value, CancellationToken cancellationToken)
        {
            var analyzerScope = value ? BackgroundAnalysisScope.FullSolution : BackgroundAnalysisScope.Default;
            var compilerScope = value ? CompilerDiagnosticsScope.FullSolution : CompilerDiagnosticsScope.OpenFiles;
            return SetBackgroundAnalysisOptionsAsync(analyzerScope, compilerScope, cancellationToken);
        }

        public async Task SetBackgroundAnalysisOptionsAsync(BackgroundAnalysisScope analyzerScope, CompilerDiagnosticsScope compilerScope, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(cancellationToken);

            globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, analyzerScope);
            globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, analyzerScope);

            globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, LanguageNames.CSharp, compilerScope);
            globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, LanguageNames.VisualBasic, compilerScope);
        }

        public Task WaitForAsyncOperationsAsync(string featuresToWaitFor, CancellationToken cancellationToken)
            => WaitForAsyncOperationsAsync(featuresToWaitFor, waitForWorkspaceFirst: true, cancellationToken);

        public async Task WaitForAsyncOperationsAsync(string featuresToWaitFor, bool waitForWorkspaceFirst, CancellationToken cancellationToken)
        {
            if (waitForWorkspaceFirst || featuresToWaitFor == FeatureAttribute.Workspace)
            {
                await WaitForProjectSystemAsync(cancellationToken);
                await TestServices.Shell.WaitForFileChangeNotificationsAsync(cancellationToken);
                await TestServices.Editor.WaitForEditorOperationsAsync(cancellationToken);
            }

            var listenerProvider = await GetComponentModelServiceAsync<AsynchronousOperationListenerProvider>(cancellationToken);

            if (waitForWorkspaceFirst)
            {
                var workspaceWaiter = listenerProvider.GetWaiter(FeatureAttribute.Workspace);
                await workspaceWaiter.ExpeditedWaitAsync().WithCancellation(cancellationToken);
            }

            var featureWaiter = listenerProvider.GetWaiter(featuresToWaitFor);
            await featureWaiter.ExpeditedWaitAsync().WithCancellation(cancellationToken);
        }

        public async Task WaitForAllAsyncOperationsAsync(string[] featureNames, CancellationToken cancellationToken)
        {
            if (featureNames.Contains(FeatureAttribute.Workspace))
            {
                await WaitForProjectSystemAsync(cancellationToken);
                await TestServices.Shell.WaitForFileChangeNotificationsAsync(cancellationToken);
                await TestServices.Editor.WaitForEditorOperationsAsync(cancellationToken);
            }

            var listenerProvider = await GetComponentModelServiceAsync<AsynchronousOperationListenerProvider>(cancellationToken);
            var workspace = await GetComponentModelServiceAsync<VisualStudioWorkspace>(cancellationToken);

            if (featureNames.Contains(FeatureAttribute.NavigateTo))
            {
                var statusService = workspace.Services.GetRequiredService<IWorkspaceStatusService>();
                Contract.ThrowIfFalse(await statusService.IsFullyLoadedAsync(cancellationToken));

                // Make sure the "priming" operation has started for Nav To
                var threadingContext = await GetComponentModelServiceAsync<IThreadingContext>(cancellationToken);
                var asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);
                var searchHost = new DefaultNavigateToSearchHost(workspace.CurrentSolution, asyncListener, threadingContext.DisposalToken);

                // Calling DefaultNavigateToSearchHost.IsFullyLoadedAsync starts the fire-and-forget asynchronous
                // operation to populate the remote host. The call to WaitAllAsync below will wait for that operation to
                // complete.
                await searchHost.IsFullyLoadedAsync(cancellationToken);
            }

            await listenerProvider.WaitAllAsync(workspace, featureNames).WithCancellation(cancellationToken);
        }

        public async Task WaitForRenameAsync(CancellationToken cancellationToken)
        {
            await WaitForAllAsyncOperationsAsync(
                [
                    FeatureAttribute.Rename,
                    FeatureAttribute.RenameTracking,
                    FeatureAttribute.InlineRenameFlyout,
                ],
                cancellationToken);
        }

        /// <summary>
        /// This event listener is an adapter to expose asynchronous file save operations to Roslyn via its standard
        /// workspace event waiters.
        /// </summary>
        private sealed class RunningDocumentTableEventListener : IVsRunningDocTableEvents, IVsRunningDocTableEvents7
        {
            private readonly IThreadingContext _threadingContext;
            private readonly IAsynchronousOperationListener _asynchronousOperationListener;

            public RunningDocumentTableEventListener(IThreadingContext threadingContext, IAsynchronousOperationListener asynchronousOperationListener)
            {
                _threadingContext = threadingContext;
                _asynchronousOperationListener = asynchronousOperationListener;
            }

            int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
                => VSConstants.S_OK;

            int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
                => VSConstants.S_OK;

            int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
                => VSConstants.S_OK;

            int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
                => VSConstants.S_OK;

            int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
                => VSConstants.S_OK;

            int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
                => VSConstants.S_OK;

            IVsTask? IVsRunningDocTableEvents7.OnBeforeSaveAsync(uint cookie, uint flags, IVsTask? saveTask)
            {
                if (saveTask is not null)
                {
                    _ = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
                    {
                        // Track asynchronous save operations via Roslyn's Workspace events
                        using var _ = _asynchronousOperationListener.BeginAsyncOperation("OnBeforeSaveAsync");
                        await saveTask;
                    });
                }

                // No additional work for the caller to handle
                return null;
            }

            IVsTask? IVsRunningDocTableEvents7.OnAfterSaveAsync(uint cookie, uint flags)
            {
                // No additional work for the caller to handle
                return null;
            }
        }
    }
}
