// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using NuGet.SolutionRestoreManager;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.Testing.InProcess
{
    public class SolutionExplorerInProcess : InProcComponent
    {
        public SolutionExplorerInProcess(TestServices testServices)
            : base(testServices)
        {
        }

        private async Task<bool> IsSolutionOpenAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>();
            ErrorHandler.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpen));
            return (bool)isOpen;
        }

        public async Task CloseSolutionAsync(bool saveFirst = false)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!await IsSolutionOpenAsync())
            {
                return;
            }

            if (saveFirst)
            {
                await SaveSolutionAsync();
            }

            await CloseSolutionAsync();
        }

        private async Task CloseSolutionAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>();
            if (!await IsSolutionOpenAsync())
            {
                return;
            }

            using var semaphore = new SemaphoreSlim(1);
            using var solutionEvents = new SolutionEvents(JoinableTaskFactory, solution);

            await semaphore.WaitAsync();

            void HandleAfterCloseSolution(object sender, EventArgs e) => semaphore.Release();

            solutionEvents.AfterCloseSolution += HandleAfterCloseSolution;
            try
            {
                ErrorHandler.ThrowOnFailure(solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject | (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave, null, 0));
                await semaphore.WaitAsync();
            }
            finally
            {
                solutionEvents.AfterCloseSolution -= HandleAfterCloseSolution;
            }
        }

        public async Task CreateSolutionAsync(string solutionName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var solutionPath = CreateTemporaryPath();
            await CreateSolutionAsync(solutionPath, solutionName);
        }

        private async Task CreateSolutionAsync(string solutionPath, string solutionName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await CloseSolutionAsync();

            var solutionFileName = Path.ChangeExtension(solutionName, ".sln");
            Directory.CreateDirectory(solutionPath);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>();
            ErrorHandler.ThrowOnFailure(solution.CreateSolution(solutionPath, solutionFileName, (uint)__VSCREATESOLUTIONFLAGS.CSF_SILENT));
            ErrorHandler.ThrowOnFailure(solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_ForceSave, null, 0));
        }

        public async Task SaveSolutionAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!await IsSolutionOpenAsync())
            {
                throw new InvalidOperationException("Cannot save solution when no solution is open.");
            }

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>();

            // Make sure the directory exists so the Save dialog doesn't appear
            ErrorHandler.ThrowOnFailure(solution.GetSolutionInfo(out var solutionDirectory, out _, out _));
            Directory.CreateDirectory(solutionDirectory);

            ErrorHandler.ThrowOnFailure(solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_ForceSave, null, 0));
        }

        private static string ConvertLanguageName(string languageName)
        {
            return languageName switch
            {
                LanguageNames.CSharp => "CSharp",
                LanguageNames.VisualBasic => "VisualBasic",
                _ => throw new ArgumentException($"'{languageName}' is not supported.", nameof(languageName)),
            };
        }

        private async Task<string> GetDirectoryNameAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>();
            var solution = (EnvDTE80.Solution2)dte.Solution;
            var solutionFullName = solution.FullName;
            var solutionFileFullPath = string.IsNullOrEmpty(solutionFullName)
                ? throw new InvalidOperationException()
                : solutionFullName;

            return Path.GetDirectoryName(solutionFileFullPath);
        }

        private async Task<ImmutableDictionary<string, string>> GetCSharpProjectTemplatesAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>();
            var localeID = dte.LocaleID;

            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder[WellKnownProjectTemplates.ClassLibrary] = $@"Windows\{localeID}\ClassLibrary.zip";
            builder[WellKnownProjectTemplates.ConsoleApplication] = "Microsoft.CSharp.ConsoleApplication";
            builder[WellKnownProjectTemplates.Website] = "EmptyWeb.zip";
            builder[WellKnownProjectTemplates.WinFormsApplication] = "WindowsApplication.zip";
            builder[WellKnownProjectTemplates.WpfApplication] = "WpfApplication.zip";
            builder[WellKnownProjectTemplates.WebApplication] = "WebApplicationProject40";
            return builder.ToImmutable();
        }

        private async Task<ImmutableDictionary<string, string>> GetVisualBasicProjectTemplatesAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>();
            var localeID = dte.LocaleID;

            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder[WellKnownProjectTemplates.ClassLibrary] = $@"Windows\{localeID}\ClassLibrary.zip";
            builder[WellKnownProjectTemplates.ConsoleApplication] = "Microsoft.VisualBasic.Windows.ConsoleApplication";
            builder[WellKnownProjectTemplates.Website] = "EmptyWeb.zip";
            builder[WellKnownProjectTemplates.WinFormsApplication] = "WindowsApplication.zip";
            builder[WellKnownProjectTemplates.WpfApplication] = "WpfApplication.zip";
            builder[WellKnownProjectTemplates.WebApplication] = "WebApplicationProject40";
            return builder.ToImmutable();
        }

        public async Task AddProjectAsync(string projectName, string projectTemplate, string languageName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var projectPath = Path.Combine(await GetDirectoryNameAsync(), projectName);
            var projectTemplatePath = await GetProjectTemplatePathAsync(projectTemplate, ConvertLanguageName(languageName));
            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution6>();
            ErrorHandler.ThrowOnFailure(solution.AddNewProjectFromTemplate(projectTemplatePath, null, null, projectPath, projectName, null, out _));
        }

        private async Task<string> GetProjectTemplatePathAsync(string projectTemplate, string languageName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>();
            var solution = (EnvDTE80.Solution2)dte.Solution;

            if (string.Equals(languageName, "csharp", StringComparison.OrdinalIgnoreCase)
                && (await GetCSharpProjectTemplatesAsync()).TryGetValue(projectTemplate, out var csharpProjectTemplate))
            {
                return solution.GetProjectTemplate(csharpProjectTemplate, languageName);
            }

            if (string.Equals(languageName, "visualbasic", StringComparison.OrdinalIgnoreCase)
                && (await GetVisualBasicProjectTemplatesAsync()).TryGetValue(projectTemplate, out var visualBasicProjectTemplate))
            {
                return solution.GetProjectTemplate(visualBasicProjectTemplate, languageName);
            }

            return solution.GetProjectTemplate(projectTemplate, languageName);
        }

        public async Task RestoreNuGetPackagesAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>();
            var solution = (EnvDTE80.Solution2)dte.Solution;
            foreach (var project in solution.Projects.OfType<EnvDTE.Project>())
            {
                await RestoreNuGetPackagesAsync(project.FullName, cancellationToken);
            }
        }

        public async Task RestoreNuGetPackagesAsync(string projectName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var operationProgressStatus = await GetRequiredGlobalServiceAsync<SVsOperationProgress, IVsOperationProgressStatusService>();
            var stageStatus = operationProgressStatus.GetStageStatus(CommonOperationProgressStageIds.Intellisense);
            await stageStatus.WaitForCompletionAsync();

            var solutionRestoreService = await GetComponentModelServiceAsync<IVsSolutionRestoreService>();
            await solutionRestoreService.CurrentRestoreOperation;

            var projectFullPath = (await GetProjectAsync(projectName)).FullName;
            var solutionRestoreStatusProvider = await GetComponentModelServiceAsync<IVsSolutionRestoreStatusProvider>();
            if (await solutionRestoreStatusProvider.IsRestoreCompleteAsync(cancellationToken))
            {
                return;
            }

            var solutionRestoreService2 = (IVsSolutionRestoreService2)solutionRestoreService;
            await solutionRestoreService2.NominateProjectAsync(projectFullPath, cancellationToken);

            while (true)
            {
                if (await solutionRestoreStatusProvider.IsRestoreCompleteAsync(cancellationToken))
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }
        }

        public async Task<string?> BuildSolutionAsync(bool waitForBuildToFinish)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var buildOutputWindowPane = await GetBuildOutputWindowPaneAsync();
            buildOutputWindowPane.Clear();

            await ExecuteCommandAsync(WellKnownCommandNames.Build.BuildSolution);
            if (waitForBuildToFinish)
            {
                return await WaitForBuildToFinishAsync(buildOutputWindowPane);
            }

            return null;
        }

        public async Task<string> WaitForBuildToFinishAsync()
        {
            var buildOutputWindowPane = await GetBuildOutputWindowPaneAsync();
            return await WaitForBuildToFinishAsync(buildOutputWindowPane);
        }

        public async Task<IVsOutputWindowPane> GetBuildOutputWindowPaneAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var outputWindow = await GetRequiredGlobalServiceAsync<SVsOutputWindow, IVsOutputWindow>();
            ErrorHandler.ThrowOnFailure(outputWindow.GetPane(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, out var pane));
            return pane;
        }

        private async Task<string> WaitForBuildToFinishAsync(IVsOutputWindowPane buildOutputWindowPane)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var buildManager = await GetRequiredGlobalServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>();
            using var semaphore = new SemaphoreSlim(1);
            using var solutionEvents = new UpdateSolutionEvents(buildManager);

            await semaphore.WaitAsync();

            void HandleUpdateSolutionDone(bool succeeded, bool modified, bool canceled) => semaphore.Release();
            solutionEvents.OnUpdateSolutionDone += HandleUpdateSolutionDone;
            try
            {
                await semaphore.WaitAsync();
            }
            finally
            {
                solutionEvents.OnUpdateSolutionDone -= HandleUpdateSolutionDone;
            }

            // Force the error list to update
            ErrorHandler.ThrowOnFailure(buildOutputWindowPane.FlushToTaskList());

            var textView = (IVsTextView)buildOutputWindowPane;
            ErrorHandler.ThrowOnFailure(((IVsUserData)textView).GetData(EditorInProcess.IWpfTextViewId, out var wpfTextViewHost));
            var lines = ((IWpfTextViewHost)wpfTextViewHost).TextView.TextViewLines;
            if (lines.Count < 1)
            {
                return string.Empty;
            }

            return lines[lines.Count - 2].Extent.GetText();
        }

        private string CreateTemporaryPath()
        {
            return Path.Combine(Path.GetTempPath(), "roslyn-sdk-test", Path.GetRandomFileName());
        }

        private async Task<EnvDTE.Project> GetProjectAsync(string nameOrFileName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>();
            var solution = (EnvDTE80.Solution2)dte.Solution;
            return solution.Projects.OfType<EnvDTE.Project>().First(
                project =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return string.Equals(project.FileName, nameOrFileName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(project.Name, nameOrFileName, StringComparison.OrdinalIgnoreCase);
                });
        }

        private sealed class SolutionEvents : IVsSolutionEvents, IDisposable
        {
            private readonly JoinableTaskFactory _joinableTaskFactory;
            private readonly IVsSolution _solution;
            private readonly uint _cookie;

            public SolutionEvents(JoinableTaskFactory joinableTaskFactory, IVsSolution solution)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                _joinableTaskFactory = joinableTaskFactory;
                _solution = solution;
                ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out _cookie));
            }

            public event EventHandler? AfterCloseSolution;

            public void Dispose()
            {
                _joinableTaskFactory.Run(async () =>
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    ErrorHandler.ThrowOnFailure(_solution.UnadviseSolutionEvents(_cookie));
                });
            }

            public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeCloseSolution(object pUnkReserved)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterCloseSolution(object pUnkReserved)
            {
                AfterCloseSolution?.Invoke(this, EventArgs.Empty);
                return VSConstants.S_OK;
            }
        }

        internal sealed class UpdateSolutionEvents : IVsUpdateSolutionEvents, IVsUpdateSolutionEvents2, IDisposable
        {
            private uint _cookie;
            private IVsSolutionBuildManager2 _solutionBuildManager;

            internal delegate void UpdateSolutionDoneEvent(bool succeeded, bool modified, bool canceled);

            internal delegate void UpdateSolutionBeginEvent(ref bool cancel);

            internal delegate void UpdateSolutionStartUpdateEvent(ref bool cancel);

            internal delegate void UpdateProjectConfigDoneEvent(IVsHierarchy projectHierarchy, IVsCfg projectConfig, int success);

            internal delegate void UpdateProjectConfigBeginEvent(IVsHierarchy projectHierarchy, IVsCfg projectConfig);

            public event UpdateSolutionDoneEvent? OnUpdateSolutionDone;

            public event UpdateSolutionBeginEvent? OnUpdateSolutionBegin;

            public event UpdateSolutionStartUpdateEvent? OnUpdateSolutionStartUpdate;

            public event Action? OnActiveProjectConfigurationChange;

            public event Action? OnUpdateSolutionCancel;

            public event UpdateProjectConfigDoneEvent? OnUpdateProjectConfigDone;

            public event UpdateProjectConfigBeginEvent? OnUpdateProjectConfigBegin;

            internal UpdateSolutionEvents(IVsSolutionBuildManager2 solutionBuildManager)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                _solutionBuildManager = solutionBuildManager;
                ErrorHandler.ThrowOnFailure(solutionBuildManager.AdviseUpdateSolutionEvents(this, out _cookie));
            }

            int IVsUpdateSolutionEvents.UpdateSolution_Begin(ref int pfCancelUpdate)
            {
                var cancel = false;
                OnUpdateSolutionBegin?.Invoke(ref cancel);
                if (cancel)
                {
                    pfCancelUpdate = 1;
                }

                return 0;
            }

            int IVsUpdateSolutionEvents.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
            {
                OnUpdateSolutionDone?.Invoke(fSucceeded != 0, fModified != 0, fCancelCommand != 0);
                return 0;
            }

            int IVsUpdateSolutionEvents.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
            {
                return UpdateSolution_StartUpdate(ref pfCancelUpdate);
            }

            int IVsUpdateSolutionEvents.UpdateSolution_Cancel()
            {
                OnUpdateSolutionCancel?.Invoke();
                return 0;
            }

            int IVsUpdateSolutionEvents.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
            {
                return OnActiveProjectCfgChange(pIVsHierarchy);
            }

            int IVsUpdateSolutionEvents2.UpdateSolution_Begin(ref int pfCancelUpdate)
            {
                var cancel = false;
                OnUpdateSolutionBegin?.Invoke(ref cancel);
                if (cancel)
                {
                    pfCancelUpdate = 1;
                }

                return 0;
            }

            int IVsUpdateSolutionEvents2.UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
            {
                OnUpdateSolutionDone?.Invoke(fSucceeded != 0, fModified != 0, fCancelCommand != 0);
                return 0;
            }

            int IVsUpdateSolutionEvents2.UpdateSolution_StartUpdate(ref int pfCancelUpdate)
            {
                return UpdateSolution_StartUpdate(ref pfCancelUpdate);
            }

            int IVsUpdateSolutionEvents2.UpdateSolution_Cancel()
            {
                OnUpdateSolutionCancel?.Invoke();
                return 0;
            }

            int IVsUpdateSolutionEvents2.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
            {
                return OnActiveProjectCfgChange(pIVsHierarchy);
            }

            int IVsUpdateSolutionEvents2.UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
            {
                OnUpdateProjectConfigBegin?.Invoke(pHierProj, pCfgProj);
                return 0;
            }

            int IVsUpdateSolutionEvents2.UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
            {
                OnUpdateProjectConfigDone?.Invoke(pHierProj, pCfgProj, fSuccess);
                return 0;
            }

            private int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
            {
                var cancel = false;
                OnUpdateSolutionStartUpdate?.Invoke(ref cancel);
                if (cancel)
                {
                    pfCancelUpdate = 1;
                }

                return 0;
            }

            private int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
            {
                OnActiveProjectConfigurationChange?.Invoke();
                return 0;
            }

            void IDisposable.Dispose()
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                OnUpdateSolutionDone = null;
                OnUpdateSolutionBegin = null;
                OnUpdateSolutionStartUpdate = null;
                OnActiveProjectConfigurationChange = null;
                OnUpdateSolutionCancel = null;
                OnUpdateProjectConfigDone = null;
                OnUpdateProjectConfigBegin = null;

                if (_cookie != 0)
                {
                    var tempCookie = _cookie;
                    _cookie = 0;
                    ErrorHandler.ThrowOnFailure(_solutionBuildManager.UnadviseUpdateSolutionEvents(tempCookie));
                }
            }
        }
    }
}
