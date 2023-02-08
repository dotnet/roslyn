// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using NuGet.SolutionRestoreManager;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    internal partial class SolutionExplorerInProcess
    {
        public async Task CreateSolutionAsync(string solutionName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solutionPath = CreateTemporaryPath();
            await CreateSolutionAsync(solutionPath, solutionName, cancellationToken);
        }

        private async Task CreateSolutionAsync(string solutionPath, string solutionName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await CloseSolutionAsync(cancellationToken);

            var solutionFileName = Path.ChangeExtension(solutionName, ".sln");
            Directory.CreateDirectory(solutionPath);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            ErrorHandler.ThrowOnFailure(solution.CreateSolution(solutionPath, solutionFileName, (uint)__VSCREATESOLUTIONFLAGS.CSF_SILENT));
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

        private async Task<string> GetDirectoryNameAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            ErrorHandler.ThrowOnFailure(solution.GetSolutionInfo(out _, out var solutionFileFullPath, out _));
            if (string.IsNullOrEmpty(solutionFileFullPath))
            {
                throw new InvalidOperationException();
            }

            return Path.GetDirectoryName(solutionFileFullPath);
        }

        private async Task<ImmutableDictionary<string, string>> GetCSharpProjectTemplatesAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var hostLocale = await GetRequiredGlobalServiceAsync<SUIHostLocale, IUIHostLocale>(cancellationToken);
            ErrorHandler.ThrowOnFailure(hostLocale.GetUILocale(out var localeID));

            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder[WellKnownProjectTemplates.ClassLibrary] = $@"Windows\{localeID}\ClassLibrary.zip";
            builder[WellKnownProjectTemplates.ConsoleApplication] = "Microsoft.CSharp.ConsoleApplication";
            builder[WellKnownProjectTemplates.Website] = "EmptyWeb.zip";
            builder[WellKnownProjectTemplates.WinFormsApplication] = "WindowsApplication.zip";
            builder[WellKnownProjectTemplates.WpfApplication] = "WpfApplication.zip";
            builder[WellKnownProjectTemplates.WebApplication] = "WebApplicationProject40";
            return builder.ToImmutable();
        }

        private async Task<ImmutableDictionary<string, string>> GetVisualBasicProjectTemplatesAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var hostLocale = await GetRequiredGlobalServiceAsync<SUIHostLocale, IUIHostLocale>(cancellationToken);
            ErrorHandler.ThrowOnFailure(hostLocale.GetUILocale(out var localeID));

            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            builder[WellKnownProjectTemplates.ClassLibrary] = $@"Windows\{localeID}\ClassLibrary.zip";
            builder[WellKnownProjectTemplates.ConsoleApplication] = "Microsoft.VisualBasic.Windows.ConsoleApplication";
            builder[WellKnownProjectTemplates.Website] = "EmptyWeb.zip";
            builder[WellKnownProjectTemplates.WinFormsApplication] = "WindowsApplication.zip";
            builder[WellKnownProjectTemplates.WpfApplication] = "WpfApplication.zip";
            builder[WellKnownProjectTemplates.WebApplication] = "WebApplicationProject40";
            return builder.ToImmutable();
        }

        public async Task AddProjectAsync(string projectName, string projectTemplate, string languageName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var projectPath = Path.Combine(await GetDirectoryNameAsync(cancellationToken), projectName);
            var projectTemplatePath = await GetProjectTemplatePathAsync(projectTemplate, ConvertLanguageName(languageName), cancellationToken);
            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution6>(cancellationToken);
            ErrorHandler.ThrowOnFailure(solution.AddNewProjectFromTemplate(projectTemplatePath, null, null, projectPath, projectName, null, out _));
        }

        private async Task<string> GetProjectTemplatePathAsync(string projectTemplate, string languageName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
            var solution = (EnvDTE80.Solution2)dte.Solution;

            if (string.Equals(languageName, "csharp", StringComparison.OrdinalIgnoreCase)
                && (await GetCSharpProjectTemplatesAsync(cancellationToken)).TryGetValue(projectTemplate, out var csharpProjectTemplate))
            {
                return solution.GetProjectTemplate(csharpProjectTemplate, languageName);
            }

            if (string.Equals(languageName, "visualbasic", StringComparison.OrdinalIgnoreCase)
                && (await GetVisualBasicProjectTemplatesAsync(cancellationToken)).TryGetValue(projectTemplate, out var visualBasicProjectTemplate))
            {
                return solution.GetProjectTemplate(visualBasicProjectTemplate, languageName);
            }

            return solution.GetProjectTemplate(projectTemplate, languageName);
        }

        public async Task RestoreNuGetPackagesAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
            var solution = (EnvDTE80.Solution2)dte.Solution;
            foreach (var project in solution.Projects.OfType<EnvDTE.Project>())
            {
                await RestoreNuGetPackagesAsync(project.FullName, cancellationToken);
            }
        }

        public async Task RestoreNuGetPackagesAsync(string projectName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var operationProgressStatus = await GetRequiredGlobalServiceAsync<SVsOperationProgress, IVsOperationProgressStatusService>(cancellationToken);
            var stageStatus = operationProgressStatus.GetStageStatus(CommonOperationProgressStageIds.Intellisense);
            await stageStatus.WaitForCompletionAsync();

            var solutionRestoreService = await GetComponentModelServiceAsync<IVsSolutionRestoreService>(cancellationToken);
            await solutionRestoreService.CurrentRestoreOperation;

            var projectFullPath = (await GetProjectAsync(projectName, cancellationToken)).FullName;
            var solutionRestoreStatusProvider = await GetComponentModelServiceAsync<IVsSolutionRestoreStatusProvider>(cancellationToken);
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

        public async Task<string?> BuildSolutionAsync(bool waitForBuildToFinish, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var buildOutputWindowPane = await GetBuildOutputWindowPaneAsync(cancellationToken);
            buildOutputWindowPane.Clear();

            await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.BuildSln, cancellationToken);
            if (waitForBuildToFinish)
            {
                return await WaitForBuildToFinishAsync(buildOutputWindowPane, cancellationToken);
            }

            return null;
        }

        public async Task<string> WaitForBuildToFinishAsync(CancellationToken cancellationToken)
        {
            var buildOutputWindowPane = await GetBuildOutputWindowPaneAsync(cancellationToken);
            return await WaitForBuildToFinishAsync(buildOutputWindowPane, cancellationToken);
        }

        public async Task<IVsOutputWindowPane> GetBuildOutputWindowPaneAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var outputWindow = await GetRequiredGlobalServiceAsync<SVsOutputWindow, IVsOutputWindow>(cancellationToken);
            ErrorHandler.ThrowOnFailure(outputWindow.GetPane(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, out var pane));
            return pane;
        }

        private async Task<string> WaitForBuildToFinishAsync(IVsOutputWindowPane buildOutputWindowPane, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var buildManager = await GetRequiredGlobalServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>(cancellationToken);
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
            var wpfTextViewHost = await textView.GetTextViewHostAsync(JoinableTaskFactory, cancellationToken);
            var lines = wpfTextViewHost.TextView.TextViewLines;
            if (lines.Count < 1)
            {
                return string.Empty;
            }

            var summary = lines[lines.Count - 2].Extent.GetText();
            if (!summary.Contains("Build:") && lines.Count > 1)
            {
                var priorToSummary = lines[lines.Count - 3].Extent.GetText();
                if (priorToSummary.Contains("Build:"))
                {
                    summary = priorToSummary;
                }
            }

            return summary;
        }

        private string CreateTemporaryPath()
        {
            return Path.Combine(Path.GetTempPath(), "roslyn-sdk-test", Path.GetRandomFileName());
        }

        private async Task<EnvDTE.Project> GetProjectAsync(string nameOrFileName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
            var solution = (EnvDTE80.Solution2)dte.Solution;
            return solution.Projects.OfType<EnvDTE.Project>().First(
                project =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return string.Equals(project.FileName, nameOrFileName, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(project.Name, nameOrFileName, StringComparison.OrdinalIgnoreCase);
                });
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
