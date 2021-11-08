// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using NuGet.SolutionRestoreManager;
using IAsyncDisposable = System.IAsyncDisposable;
using Reference = VSLangProj.Reference;
using VSProject = VSLangProj.VSProject;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal class SolutionExplorerInProcess : InProcComponent
    {
        public SolutionExplorerInProcess(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task CreateSolutionAsync(string solutionName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solutionPath = CreateTemporaryPath();
            await CreateSolutionAsync(solutionPath, solutionName, cancellationToken);
        }

        public async Task CreateSolutionAsync(string solutionName, XElement solutionElement, CancellationToken cancellationToken)
        {
            if (solutionElement.Name != "Solution")
            {
                throw new ArgumentException(nameof(solutionElement));
            }

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await CreateSolutionAsync(solutionName, cancellationToken);

            foreach (var projectElement in solutionElement.Elements("Project"))
            {
                await CreateProjectAsync(projectElement, cancellationToken);
            }

            foreach (var projectElement in solutionElement.Elements("Project"))
            {
                var projectReferences = projectElement.Attribute("ProjectReferences")?.Value;
                if (projectReferences != null)
                {
                    var projectName = projectElement.Attribute("ProjectName").Value;
                    foreach (var projectReference in projectReferences.Split(';'))
                    {
                        await AddProjectReferenceAsync(projectName, projectReference, cancellationToken);
                    }
                }
            }
        }

        private async Task CreateProjectAsync(XElement projectElement, CancellationToken cancellationToken)
        {
            const string language = "Language";
            const string name = "ProjectName";
            const string template = "ProjectTemplate";
            var languageName = projectElement.Attribute(language)?.Value
                ?? throw new ArgumentException($"You must specify an attribute called '{language}' on a project element.");
            var projectName = projectElement.Attribute(name)?.Value
                ?? throw new ArgumentException($"You must specify an attribute called '{name}' on a project element.");
            var projectTemplate = projectElement.Attribute(template)?.Value
                ?? throw new ArgumentException($"You must specify an attribute called '{template}' on a project element.");

            var projectPath = Path.Combine(await GetDirectoryNameAsync(cancellationToken), projectName);
            var projectTemplatePath = await GetProjectTemplatePathAsync(projectTemplate, ConvertLanguageName(languageName), cancellationToken);

            var solution = (await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken)).Solution;
            Assumes.Present(solution);

            solution.AddFromTemplate(projectTemplatePath, projectPath, projectName, Exclusive: false);
            foreach (var documentElement in projectElement.Elements("Document"))
            {
                var fileName = documentElement.Attribute("FileName").Value;
                await UpdateOrAddFileAsync(projectName, fileName, contents: documentElement.Value, cancellationToken: cancellationToken);
            }
        }

        public async Task AddProjectReferenceAsync(string projectName, string projectToReferenceName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var project = await GetProjectAsync(projectName, cancellationToken);
            var projectToReference = await GetProjectAsync(projectToReferenceName, cancellationToken);
            ((VSProject)project.Object).References.AddProject(projectToReference);
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

        public async Task<string[]> GetAssemblyReferencesAsync(string projectName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var project = await GetProjectAsync(projectName, cancellationToken);
            var references = ((VSProject)project.Object).References.Cast<Reference>()
                .Where(x => x.SourceProject == null)
                .Select(x => x.Name + "," + x.Version + "," + x.PublicKeyToken).ToArray();
            return references;
        }

        public async Task<string[]> GetProjectReferencesAsync(string projectName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var project = await GetProjectAsync(projectName, cancellationToken);
            var references = ((VSProject)project.Object).References.Cast<Reference>().Where(x => x.SourceProject != null).Select(x => x.Name).ToArray();
            return references;
        }

        public async Task AddProjectAsync(string projectName, string projectTemplate, string languageName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var projectPath = Path.Combine(await GetDirectoryNameAsync(cancellationToken), projectName);
            var projectTemplatePath = await GetProjectTemplatePathAsync(projectTemplate, ConvertLanguageName(languageName), cancellationToken);
            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution6>(cancellationToken);
            ErrorHandler.ThrowOnFailure(solution.AddNewProjectFromTemplate(projectTemplatePath, null, null, projectPath, projectName, null, out _));
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
            await stageStatus.WaitForCompletionAsync().WithCancellation(cancellationToken);

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

        public async Task OpenFileAsync(string projectName, string relativeFilePath, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var filePath = await GetAbsolutePathForProjectRelativeFilePathAsync(projectName, relativeFilePath, cancellationToken);
            VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, filePath, VSConstants.LOGVIEWID.Code_guid, out _, out _, out _, out var view);

            // Reliably set focus using NavigateToLineAndColumn
            var textManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
            ErrorHandler.ThrowOnFailure(view.GetBuffer(out var textLines));
            ErrorHandler.ThrowOnFailure(view.GetCaretPos(out var line, out var column));
            ErrorHandler.ThrowOnFailure(textManager.NavigateToLineAndColumn(textLines, VSConstants.LOGVIEWID.Code_guid, line, column, line, column));
        }

        public async Task CloseCodeFileAsync(string projectName, string relativeFilePath, bool saveFile, CancellationToken cancellationToken)
        {
            await CloseFileAsync(projectName, relativeFilePath, VSConstants.LOGVIEWID.Code_guid, saveFile, cancellationToken);
        }

        private async Task CloseFileAsync(string projectName, string relativeFilePath, Guid logicalView, bool saveFile, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var filePath = await GetAbsolutePathForProjectRelativeFilePathAsync(projectName, relativeFilePath, cancellationToken);
            if (!VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, filePath, logicalView, out _, out _, out var windowFrame))
            {
                throw new InvalidOperationException($"File '{filePath}' is not open in logical view '{logicalView}'");
            }

            var frameClose = saveFile ? __FRAMECLOSE.FRAMECLOSE_SaveIfDirty : __FRAMECLOSE.FRAMECLOSE_NoSave;
            ErrorHandler.ThrowOnFailure(windowFrame.CloseFrame((uint)frameClose));
        }

        /// <summary>
        /// Update the given file if it already exists in the project, otherwise add a new file to the project.
        /// </summary>
        /// <param name="projectName">The project that contains the file.</param>
        /// <param name="fileName">The name of the file to update or add.</param>
        /// <param name="contents">The contents of the file to overwrite if the file already exists or set if the file it created. Empty string is used if null is passed.</param>
        /// <param name="open">Whether to open the file after it has been updated/created.</param>
        public async Task UpdateOrAddFileAsync(string projectName, string fileName, string? contents = null, bool open = false, CancellationToken cancellationToken = default)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var project = await GetProjectAsync(projectName, cancellationToken);
            if (project.ProjectItems.Cast<EnvDTE.ProjectItem>().Any(x => x.Name == fileName))
            {
                await UpdateFileAsync(projectName, fileName, contents, open, cancellationToken);
            }
            else
            {
                await AddFileAsync(projectName, fileName, contents, open, cancellationToken);
            }
        }

        /// <summary>
        /// Update the given file to have the contents given.
        /// </summary>
        /// <param name="projectName">The project that contains the file.</param>
        /// <param name="fileName">The name of the file to update or add.</param>
        /// <param name="contents">The contents of the file to overwrite. Empty string is used if null is passed.</param>
        /// <param name="open">Whether to open the file after it has been updated.</param>
        public async Task UpdateFileAsync(string projectName, string fileName, string? contents = null, bool open = false, CancellationToken cancellationToken = default)
        {
            async Task SetTextAsync(string text, CancellationToken cancellationToken)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // The active text view might not have finished composing yet, waiting for the application to 'idle'
                // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
                await WaitForApplicationIdleAsync(cancellationToken);

                var vsTextManager = await GetRequiredGlobalServiceAsync<SVsTextManager, IVsTextManager>(cancellationToken);
                var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var vsTextView);
                Marshal.ThrowExceptionForHR(hresult);
                var activeVsTextView = (IVsUserData)vsTextView;

                hresult = activeVsTextView.GetData(EditorInProcess.IWpfTextViewId, out var wpfTextViewHost);
                Marshal.ThrowExceptionForHR(hresult);

                var view = ((IWpfTextViewHost)wpfTextViewHost).TextView;
                var textSnapshot = view.TextSnapshot;
                var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
                view.TextBuffer.Replace(replacementSpan, text);
            }

            await OpenFileAsync(projectName, fileName, cancellationToken);
            await SetTextAsync(contents ?? string.Empty, cancellationToken);
            await CloseCodeFileAsync(projectName, fileName, saveFile: true, cancellationToken);
            if (open)
            {
                await OpenFileAsync(projectName, fileName, cancellationToken);
            }
        }

        /// <summary>
        /// Add new file to project.
        /// </summary>
        /// <param name="projectName">The project that contains the file.</param>
        /// <param name="fileName">The name of the file to add.</param>
        /// <param name="contents">The contents of the file to overwrite. An empty file is create if null is passed.</param>
        /// <param name="open">Whether to open the file after it has been updated.</param>
        public async Task AddFileAsync(string projectName, string fileName, string? contents = null, bool open = false, CancellationToken cancellationToken = default)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var project = await GetProjectAsync(projectName, cancellationToken);
            var projectDirectory = Path.GetDirectoryName(project.FullName);
            var filePath = Path.Combine(projectDirectory, fileName);
            var directoryPath = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directoryPath);

            if (contents != null)
            {
                File.WriteAllText(filePath, contents);
            }
            else if (!File.Exists(filePath))
            {
                File.Create(filePath).Dispose();
            }

            _ = project.ProjectItems.AddFromFile(filePath);

            if (open)
            {
                await OpenFileAsync(projectName, fileName, cancellationToken);
            }
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

        private async Task<string> GetAbsolutePathForProjectRelativeFilePathAsync(string projectName, string relativeFilePath, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
            var solution = dte.Solution;
            Assumes.Present(solution);

            var project = solution.Projects.Cast<EnvDTE.Project>().First(x => x.Name == projectName);
            var projectPath = Path.GetDirectoryName(project.FullName);
            return Path.Combine(projectPath, relativeFilePath);
        }

        private async Task<bool> IsSolutionOpenAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            ErrorHandler.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpen));
            return (bool)isOpen;
        }

        /// <summary>
        /// Close the currently open solution without saving.
        /// </summary>
        public async Task CloseSolutionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            if (!await IsSolutionOpenAsync(cancellationToken))
            {
                return;
            }

#pragma warning disable IDE0007 // Use implicit type (implicit type introduces a compiler warning)
            using SemaphoreSlim semaphore = new SemaphoreSlim(1);
#pragma warning restore IDE0007 // Use implicit type
            await using var solutionEvents = new SolutionEvents(JoinableTaskFactory, solution);

            await semaphore.WaitAsync(cancellationToken);

            void HandleAfterCloseSolution(object sender, EventArgs e)
                => semaphore.Release();

            solutionEvents.AfterCloseSolution += HandleAfterCloseSolution;
            try
            {
                ErrorHandler.ThrowOnFailure(solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject | (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave, null, 0));
                await semaphore.WaitAsync(cancellationToken);
            }
            finally
            {
                solutionEvents.AfterCloseSolution -= HandleAfterCloseSolution;
            }
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

        private static string CreateTemporaryPath()
        {
            return Path.Combine(Path.GetTempPath(), "roslyn-test", Path.GetRandomFileName());
        }

        private async Task<EnvDTE.Project> GetProjectAsync(string nameOrFileName, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

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

        private sealed class SolutionEvents : IVsSolutionEvents, IAsyncDisposable
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

            public async ValueTask DisposeAsync()
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                ErrorHandler.ThrowOnFailure(_solution.UnadviseSolutionEvents(_cookie));
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
    }
}
