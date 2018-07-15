// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using NuGet.VisualStudio;
using VSLangProj;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public partial class SolutionExplorer_InProc2 : InProcComponent2
    {
        private readonly Solution2 _solution;

        private readonly IDictionary<string, string> _csharpProjectTemplates;
        private readonly IDictionary<string, string> _visualBasicProjectTemplates;

        public SolutionExplorer_InProc2(TestServices testServices)
            : base(testServices)
        {
            var dte = JoinableTaskFactory.Run(GetDTEAsync);
            _solution = (Solution2)dte.Solution;
            var localeID = dte.LocaleID;
            _csharpProjectTemplates = InitializeCSharpProjectTemplates(localeID);
            _visualBasicProjectTemplates = InitializeVisualBasicProjectTemplates(localeID);

            Verify = new Verifier(this);
        }

        public Verifier Verify
        {
            get;
        }

        private static IDictionary<string, string> InitializeCSharpProjectTemplates(int localeID)
        {
            return new Dictionary<string, string>
            {
                [WellKnownProjectTemplates.ClassLibrary] = $@"Windows\{localeID}\ClassLibrary.zip",
                [WellKnownProjectTemplates.ConsoleApplication] = "Microsoft.CSharp.ConsoleApplication",
                [WellKnownProjectTemplates.Website] = "EmptyWeb.zip",
                [WellKnownProjectTemplates.WinFormsApplication] = "WindowsApplication.zip",
                [WellKnownProjectTemplates.WpfApplication] = "WpfApplication.zip",
                [WellKnownProjectTemplates.WebApplication] = "WebApplicationProject40"
            };
        }

        private static IDictionary<string, string> InitializeVisualBasicProjectTemplates(int localeID)
        {
            return new Dictionary<string, string>
            {
                [WellKnownProjectTemplates.ClassLibrary] = $@"Windows\{localeID}\ClassLibrary.zip",
                [WellKnownProjectTemplates.ConsoleApplication] = "Microsoft.VisualBasic.Windows.ConsoleApplication",
                [WellKnownProjectTemplates.Website] = "EmptyWeb.zip",
                [WellKnownProjectTemplates.WinFormsApplication] = "WindowsApplication.zip",
                [WellKnownProjectTemplates.WpfApplication] = "WpfApplication.zip",
                [WellKnownProjectTemplates.WebApplication] = "WebApplicationProject40"
            };
        }

        public async Task AddMetadataReferenceAsync(string assemblyName, string projectName)
        {
            var project = GetProject(projectName);
            var vsproject = ((VSProject)project.Object);
            vsproject.References.Add(assemblyName);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

        public async Task RemoveMetadataReferenceAsync(string assemblyName, string projectName)
        {
            var project = GetProject(projectName);
            var reference = ((VSProject)project.Object).References.Cast<Reference>().Where(x => x.Name == assemblyName).First();
            reference.Remove();

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

        public string DirectoryName => Path.GetDirectoryName(SolutionFileFullPath);

        public string SolutionFileFullPath
        {
            get
            {
                var solutionFullName = _solution.FullName;

                return string.IsNullOrEmpty(solutionFullName)
                    ? throw new InvalidOperationException()
                    : solutionFullName;
            }
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

        /// <summary>
        /// Creates and loads a new solution in the host process.
        /// </summary>
        public async Task CreateSolutionAsync(string solutionName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var solutionPath = IntegrationHelper.CreateTemporaryPath();
            IntegrationHelper.DeleteDirectoryRecursively(solutionPath);
            await CreateSolutionAsync(solutionPath, solutionName);
        }

        /// <summary>
        /// Creates and loads a new solution in the host process.
        /// </summary>
        public async Task CreateSolutionAsync(string solutionPath, string solutionName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            await CloseSolutionAsync();

            var solutionFileName = Path.ChangeExtension(solutionName, ".sln");
            Directory.CreateDirectory(solutionPath);

            var solution = await GetGlobalServiceAsync<SVsSolution, IVsSolution>();
            ErrorHandler.ThrowOnFailure(solution.CreateSolution(solutionPath, solutionFileName, (uint)__VSCREATESOLUTIONFLAGS.CSF_SILENT));
            ErrorHandler.ThrowOnFailure(solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_ForceSave, null, 0));
        }

        public string[] GetAssemblyReferences(string projectName)
        {
            var project = GetProject(projectName);
            var references = ((VSProject)project.Object).References.Cast<Reference>()
                .Where(x => x.SourceProject == null)
                .Select(x => x.Name + "," + x.Version + "," + x.PublicKeyToken).ToArray();
            return references;
        }

        public void RenameFile(string projectName, string oldFileName, string newFileName)
        {
            var projectItem = GetProjectItem(projectName, oldFileName);

            projectItem.Name = newFileName;
        }

        public async Task EditProjectFileAsync(string projectName)
        {
            var solutionExplorer = ((DTE2)(await GetDTEAsync())).ToolWindows.SolutionExplorer;
            solutionExplorer.Parent.Activate();
            var rootHierarchyItems = solutionExplorer.UIHierarchyItems.Cast<EnvDTE.UIHierarchyItem>();
            var solution = rootHierarchyItems.First();
            var solutionHierarchyItems = solution.UIHierarchyItems.Cast<EnvDTE.UIHierarchyItem>();
            var project = solutionHierarchyItems.Where(x => x.Name == projectName).FirstOrDefault();
            if (project == null)
            {
                throw new ArgumentException($"Could not find project file, current hierarchy items '{string.Join(", ", rootHierarchyItems.Select(x => x.Name))}'");
            }

            project.Select(EnvDTE.vsUISelectionType.vsUISelectionTypeSelect);
            await ExecuteCommandAsync("Project.EditProjectFile");
        }

        public string[] GetProjectReferences(string projectName)
        {
            var project = GetProject(projectName);
            var references = ((VSProject)project.Object).References.Cast<Reference>().Where(x => x.SourceProject != null).Select(x => x.Name).ToArray();
            return references;
        }

        public async Task CreateSolutionAsync(string solutionName, XElement solutionElement)
        {
            if (solutionElement.Name != "Solution")
            {
                throw new ArgumentException(nameof(solutionElement));
            }

            await CreateSolutionAsync(solutionName);

            foreach (var projectElement in solutionElement.Elements("Project"))
            {
                await CreateProjectAsync(projectElement);
            }

            foreach (var projectElement in solutionElement.Elements("Project"))
            {
                var projectReferences = projectElement.Attribute("ProjectReferences")?.Value;
                if (projectReferences != null)
                {
                    var projectName = projectElement.Attribute("ProjectName").Value;
                    foreach (var projectReference in projectReferences.Split(';'))
                    {
                        await AddProjectReferenceAsync(projectName, projectReference);
                    }
                }
            }
        }

        private async Task CreateProjectAsync(XElement projectElement)
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

            var projectPath = Path.Combine(DirectoryName, projectName);
            var projectTemplatePath = await GetProjectTemplatePathAsync(projectTemplate, ConvertLanguageName(languageName));

            _solution.AddFromTemplate(projectTemplatePath, projectPath, projectName, Exclusive: false);
            foreach (var documentElement in projectElement.Elements("Document"))
            {
                var fileName = documentElement.Attribute("FileName").Value;
                await UpdateOrAddFileAsync(projectName, fileName, contents: documentElement.Value);
            }
        }

        public async Task AddProjectReferenceAsync(string projectName, string projectToReferenceName)
        {
            var project = GetProject(projectName);
            var projectToReference = GetProject(projectToReferenceName);
            ((VSProject)project.Object).References.AddProject(projectToReference);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

#if false
        public void AddReference(string projectName, string fullyQualifiedAssemblyName)
        {
            var project = GetProject(projectName);
            ((VSProject)project.Object).References.Add(fullyQualifiedAssemblyName);
        }

        public void AddPackageReference(string projectName, string packageName, string version)
        {
            var project = GetProject(projectName);

            if (project is IVsBrowseObjectContext browseObjectContext)
            {
                var threadingService = browseObjectContext.UnconfiguredProject.ProjectService.Services.ThreadingPolicy;

                var result = threadingService.ExecuteSynchronously(async () =>
                {
                    var configuredProject = await browseObjectContext.UnconfiguredProject.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
                    return await configuredProject.Services.PackageReferences.AddAsync(packageName, version).ConfigureAwait(false);
                });
            }
            else
            {
                throw new InvalidOperationException($"'{nameof(AddPackageReference)}' is not supported in project '{projectName}'.");
            }
        }

        public void RemovePackageReference(string projectName, string packageName)
        {
            var project = GetProject(projectName);

            if (project is IVsBrowseObjectContext browseObjectContext)
            {
                var threadingService = browseObjectContext.UnconfiguredProject.ProjectService.Services.ThreadingPolicy;

                threadingService.ExecuteSynchronously(async () =>
                {
                    var configuredProject = await browseObjectContext.UnconfiguredProject.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
                    await configuredProject.Services.PackageReferences.RemoveAsync(packageName).ConfigureAwait(false);
                });
            }
            else
            {
                throw new InvalidOperationException($"'{nameof(RemovePackageReference)}' is not supported in project '{projectName}'.");
            }
        }
#endif

        public async Task RemoveProjectReferenceAsync(string projectName, string projectReferenceName)
        {
            var project = GetProject(projectName);
            var vsproject = (VSProject)project.Object;
            var references = vsproject.References.Cast<Reference>();
            var reference = references.Where(x => x.ContainingProject != null && x.Name == projectReferenceName).FirstOrDefault();
            if (reference == null)
            {
                var projectReference = references.Where(x => x.ContainingProject != null).Select(x => x.Name);
                throw new ArgumentException($"reference to project {projectReferenceName} not found, references: '{string.Join(", ", projectReference)}'");
            }
            reference.Remove();

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

        public async Task OpenSolutionAsync(string path, bool saveExistingSolutionIfExists = false)
        {
            await CloseSolutionAsync(saveExistingSolutionIfExists);

            var solution = await TestServices.VisualStudio.GetGlobalServiceAsync<SVsSolution, IVsSolution>();
            ErrorHandler.ThrowOnFailure(solution.OpenSolutionFile((uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, path));
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

        private static string ConvertLanguageName(string languageName)
        {
            const string CSharp = nameof(CSharp);
            const string VisualBasic = nameof(VisualBasic);

            switch (languageName)
            {
                case LanguageNames.CSharp:
                    return CSharp;
                case LanguageNames.VisualBasic:
                    return VisualBasic;
                default:
                    throw new ArgumentException($"{languageName} is not supported.", nameof(languageName));
            }
        }

        public async Task AddProjectAsync(string projectName, string projectTemplate, string languageName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var projectPath = Path.Combine(DirectoryName, projectName);

            var projectTemplatePath = await GetProjectTemplatePathAsync(projectTemplate, ConvertLanguageName(languageName));

            var solution = await GetGlobalServiceAsync<SVsSolution, IVsSolution6>();
            ErrorHandler.ThrowOnFailure(solution.AddNewProjectFromTemplate(projectTemplatePath, null, null, projectPath, projectName, null, out var newProject));

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

        // TODO: Adjust language name based on whether we are using a web template
        private async Task<string> GetProjectTemplatePathAsync(string projectTemplate, string languageName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            if (languageName.Equals("csharp", StringComparison.OrdinalIgnoreCase) &&
               _csharpProjectTemplates.TryGetValue(projectTemplate, out var csharpProjectTemplate))
            {
                return _solution.GetProjectTemplate(csharpProjectTemplate, languageName);
            }

            if (languageName.Equals("visualbasic", StringComparison.OrdinalIgnoreCase) &&
               _visualBasicProjectTemplates.TryGetValue(projectTemplate, out var visualBasicProjectTemplate))
            {
                return _solution.GetProjectTemplate(visualBasicProjectTemplate, languageName);
            }

            return _solution.GetProjectTemplate(projectTemplate, languageName);
        }

        public async Task CleanUpOpenSolutionAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var directoriesToDelete = new List<string>();

            var dte = await GetDTEAsync();
            if (dte.Solution != null)
            {
                // Save the full path to each project in the solution. This is so we can
                // cleanup any folders after the solution is closed.
                foreach (EnvDTE.Project project in dte.Solution.Projects)
                {
                    if (!string.IsNullOrEmpty(project.FullName))
                    {
                        directoriesToDelete.Add(Path.GetDirectoryName(project.FullName));
                    }
                }

                // Save the full path to the solution. This is so we can cleanup any folders after the solution is closed.
                // The solution might be zero-impact and thus has no name, so deal with that
                var solutionFullName = dte.Solution.FullName;

                if (!string.IsNullOrEmpty(solutionFullName))
                {
                    directoriesToDelete.Add(Path.GetDirectoryName(solutionFullName));
                }
            }

            if (dte.Debugger.CurrentMode != EnvDTE.dbgDebugMode.dbgDesignMode)
            {
                dte.Debugger.TerminateAll();

                // This delay was originally added to address test failures in BasicEditAndContinue. When running
                // multiple tests in sequence, situations were observed where the Edit and Continue state was not reset:
                //
                // 1. Test A runs, starts debugging with Edit and Continue
                // 2. Test A completes, and the debugger is terminated
                // 3. A new project is created for test B
                // 4. Test B attempts to set the text for the document created in step (3), but fails
                //
                // Step (4) was causing test failures because the project created for test B remained in a read-only
                // state believing a debugger session was active.
                //
                // This delay should be replaced with a proper wait condition once the correct one is determined.
                var editAndContinueService = await GetComponentModelServiceAsync<IEditAndContinueService>();
                do
                {
                    await Task.Yield();
                }
                while (editAndContinueService?.DebuggingSession != null);
            }

            await CloseSolutionAsync();

            foreach (var directoryToDelete in directoriesToDelete)
            {
                IntegrationHelper.TryDeleteDirectoryRecursively(directoryToDelete);
            }
        }

        public async Task<bool> IsSolutionOpenAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var solution = await GetGlobalServiceAsync<SVsSolution, IVsSolution>();
            ErrorHandler.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpen));
            return (bool)isOpen;
        }

        public async Task SaveSolutionAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!await IsSolutionOpenAsync())
            {
                throw new InvalidOperationException("Cannot save solution when no solution is open.");
            }

            var solution = await GetGlobalServiceAsync<SVsSolution, IVsSolution>();

            // Make sure the directory exists so the Save dialog doesn't appear
            ErrorHandler.ThrowOnFailure(solution.GetSolutionInfo(out var solutionDirectory, out _, out _));
            Directory.CreateDirectory(solutionDirectory);

            ErrorHandler.ThrowOnFailure(solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_ForceSave, null, 0));
        }

        private async Task CloseSolutionAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var solution = await GetGlobalServiceAsync<SVsSolution, IVsSolution>();
            if (!await IsSolutionOpenAsync())
            {
                return;
            }

            using (var semaphore = new SemaphoreSlim(1))
            using (var solutionEvents = new SolutionEvents(JoinableTaskFactory, solution))
            {
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

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
        }

        private sealed class SolutionEvents : IVsSolutionEvents, IDisposable
        {
            private readonly JoinableTaskFactory _joinableTaskFactory;
            private readonly IVsSolution _solution;
            private readonly uint _cookie;

            public SolutionEvents(JoinableTaskFactory joinableTaskFactory, IVsSolution solution)
            {
                _joinableTaskFactory = joinableTaskFactory;
                _solution = solution;
                ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out _cookie));
            }

            public event EventHandler AfterCloseSolution;

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

        private EnvDTE.Project GetProject(string nameOrFileName)
            => _solution.Projects.OfType<EnvDTE.Project>().First(p
                => string.Compare(p.FileName, nameOrFileName, StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(p.Name, nameOrFileName, StringComparison.OrdinalIgnoreCase) == 0);

        /// <summary>
        /// Update the given file if it already exists in the project, otherwise add a new file to the project.
        /// </summary>
        /// <param name="projectName">The project that contains the file.</param>
        /// <param name="fileName">The name of the file to update or add.</param>
        /// <param name="contents">The contents of the file to overwrite if the file already exists or set if the file it created. Empty string is used if null is passed.</param>
        /// <param name="open">Whether to open the file after it has been updated/created.</param>
        public async Task UpdateOrAddFileAsync(string projectName, string fileName, string contents = null, bool open = false)
        {
            var project = GetProject(projectName);
            if (project.ProjectItems.Cast<EnvDTE.ProjectItem>().Any(x => x.Name == fileName))
            {
                await UpdateFileAsync(projectName, fileName, contents, open);
            }
            else
            {
                await AddFileAsync(projectName, fileName, contents, open);
            }
        }

        /// <summary>
        /// Update the given file to have the contents given.
        /// </summary>
        /// <param name="projectName">The project that contains the file.</param>
        /// <param name="fileName">The name of the file to update or add.</param>
        /// <param name="contents">The contents of the file to overwrite. Empty string is used if null is passed.</param>
        /// <param name="open">Whether to open the file after it has been updated.</param>
        public async Task UpdateFileAsync(string projectName, string fileName, string contents = null, bool open = false)
        {
            async Task SetTextAsync(string text)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                // The active text view might not have finished composing yet, waiting for the application to 'idle'
                // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
                await WaitForApplicationIdleAsync(CancellationToken.None);

                var vsTextManager = await GetGlobalServiceAsync<SVsTextManager, IVsTextManager>();
                var hresult = vsTextManager.GetActiveView(fMustHaveFocus: 1, pBuffer: null, ppView: out var vsTextView);
                Marshal.ThrowExceptionForHR(hresult);
                var activeVsTextView = (IVsUserData)vsTextView;

                var editorGuid = new Guid("8C40265E-9FDB-4F54-A0FD-EBB72B7D0476");
                hresult = activeVsTextView.GetData(editorGuid, out var wpfTextViewHost);
                Marshal.ThrowExceptionForHR(hresult);

                var view = ((IWpfTextViewHost)wpfTextViewHost).TextView;
                var textSnapshot = view.TextSnapshot;
                var replacementSpan = new Text.SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
                view.TextBuffer.Replace(replacementSpan, text);
            }

            await OpenFileAsync(projectName, fileName);
            await SetTextAsync(contents ?? string.Empty);
            await CloseFileAsync(projectName, fileName, saveFile: true);
            if (open)
            {
                await OpenFileAsync(projectName, fileName);
            }
        }

        /// <summary>
        /// Add new file to project.
        /// </summary>
        /// <param name="projectName">The project that contains the file.</param>
        /// <param name="fileName">The name of the file to add.</param>
        /// <param name="contents">The contents of the file to overwrite. An empty file is create if null is passed.</param>
        /// <param name="open">Whether to open the file after it has been updated.</param>
        public async Task AddFileAsync(string projectName, string fileName, string contents = null, bool open = false)
        {
            var project = GetProject(projectName);
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

            var projectItem = project.ProjectItems.AddFromFile(filePath);

            if (open)
            {
                await OpenFileAsync(projectName, fileName);
            }
        }

        /// <summary>
        /// Adds a new standalone file to the Miscellaneous Files workspace.
        /// </summary>
        /// <param name="fileName">The name of the file to add.</param>
        public async Task AddStandaloneFileAsync(string fileName)
        {
            string itemTemplate;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            switch (extension)
            {
                case ".cs":
                    itemTemplate = @"General\Visual C# Class";
                    break;
                case ".csx":
                    itemTemplate = @"Script\Visual C# Script";
                    break;
                case ".vb":
                    itemTemplate = @"General\Visual Basic Class";
                    break;
                case ".txt":
                    itemTemplate = @"General\Text File";
                    break;
                default:
                    throw new NotSupportedException($"File type '{extension}' is not yet supported.");
            }

            (await GetDTEAsync()).ItemOperations.NewFile(itemTemplate, fileName);
        }

        public void SetFileContents(string projectName, string relativeFilePath, string contents)
        {
            var project = GetProject(projectName);
            var projectPath = Path.GetDirectoryName(project.FullName);
            var filePath = Path.Combine(projectPath, relativeFilePath);

            File.WriteAllText(filePath, contents);
        }

        public string GetFileContents(string projectName, string relativeFilePath)
        {
            var project = GetProject(projectName);
            var projectPath = Path.GetDirectoryName(project.FullName);
            var filePath = Path.Combine(projectPath, relativeFilePath);

            return File.ReadAllText(filePath);
        }

        public async Task BuildSolutionAsync(bool waitForBuildToFinish)
        {
            var buildOutputWindowPane = await GetBuildOutputWindowPaneAsync();
            buildOutputWindowPane.Clear();
            await ExecuteCommandAsync(WellKnownCommandNames.Build_BuildSolution);
            await WaitForBuildToFinishAsync(buildOutputWindowPane);
        }

#if false
        public void ClearBuildOutputWindowPane()
        {
            var buildOutputWindowPane = GetBuildOutputWindowPane();
            buildOutputWindowPane.Clear();
        }
#endif

        public async Task WaitForBuildToFinishAsync()
        {
            var buildOutputWindowPane = await GetBuildOutputWindowPaneAsync();
            await WaitForBuildToFinishAsync(buildOutputWindowPane);
        }

        private async Task<EnvDTE.OutputWindowPane> GetBuildOutputWindowPaneAsync()
        {
            var dte = (DTE2)await GetDTEAsync();
            var outputWindow = dte.ToolWindows.OutputWindow;
            return outputWindow.OutputWindowPanes.Item("Build");
        }

        private async Task WaitForBuildToFinishAsync(EnvDTE.OutputWindowPane buildOutputWindowPane)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var buildManager = await GetGlobalServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>();
            using (var semaphore = new SemaphoreSlim(1))
            using (var solutionEvents = new UpdateSolutionEvents(buildManager))
            {
                await semaphore.WaitAsync();
                void @event(bool succeeded, bool modified, bool canceled) => semaphore.Release();
                solutionEvents.OnUpdateSolutionDone += @event;
                try
                {
                    await semaphore.WaitAsync();
                }
                finally
                {
                    solutionEvents.OnUpdateSolutionDone -= @event;
                }
            }
        }

        internal sealed class UpdateSolutionEvents : IVsUpdateSolutionEvents, IVsUpdateSolutionEvents2, IDisposable
        {
            private uint cookie;
            private IVsSolutionBuildManager2 solutionBuildManager;

            internal delegate void UpdateSolutionDoneEvent(bool succeeded, bool modified, bool canceled);
            internal delegate void UpdateSolutionBeginEvent(ref bool cancel);
            internal delegate void UpdateSolutionStartUpdateEvent(ref bool cancel);
            internal delegate void UpdateProjectConfigDoneEvent(IVsHierarchy projectHierarchy, IVsCfg projectConfig, int success);
            internal delegate void UpdateProjectConfigBeginEvent(IVsHierarchy projectHierarchy, IVsCfg projectConfig);

            public event UpdateSolutionDoneEvent OnUpdateSolutionDone;
            public event UpdateSolutionBeginEvent OnUpdateSolutionBegin;
            public event UpdateSolutionStartUpdateEvent OnUpdateSolutionStartUpdate;
            public event Action OnActiveProjectConfigurationChange;
            public event Action OnUpdateSolutionCancel;
            public event UpdateProjectConfigDoneEvent OnUpdateProjectConfigDone;
            public event UpdateProjectConfigBeginEvent OnUpdateProjectConfigBegin;

            internal UpdateSolutionEvents(IVsSolutionBuildManager2 solutionBuildManager)
            {
                this.solutionBuildManager = solutionBuildManager;
                var hresult = solutionBuildManager.AdviseUpdateSolutionEvents(this, out cookie);
                if (hresult != 0)
                {
                    System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hresult);
                }
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
                OnUpdateSolutionDone = null;
                OnUpdateSolutionBegin = null;
                OnUpdateSolutionStartUpdate = null;
                OnActiveProjectConfigurationChange = null;
                OnUpdateSolutionCancel = null;
                OnUpdateProjectConfigDone = null;
                OnUpdateProjectConfigBegin = null;

                if (cookie != 0)
                {
                    var tempCookie = cookie;
                    cookie = 0;
                    var hresult = solutionBuildManager.UnadviseUpdateSolutionEvents(tempCookie);
                    if (hresult != 0)
                    {
                        System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hresult);
                    }
                }
            }
        }

        public async Task OpenFileWithDesignerAsync(string projectName, string relativeFilePath)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var filePath = GetAbsolutePathForProjectRelativeFilePath(projectName, relativeFilePath);
            VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, filePath, VSConstants.LOGVIEWID.Designer_guid, out _, out _, out var windowFrame, out _);

            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public async Task OpenFileAsync(string projectName, string relativeFilePath)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var filePath = GetAbsolutePathForProjectRelativeFilePath(projectName, relativeFilePath);
            VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, filePath, VSConstants.LOGVIEWID.Code_guid, out _, out _, out _, out var view);

            // Reliably set focus using NavigateToLineAndColumn
            var textManager = await GetGlobalServiceAsync<SVsTextManager, IVsTextManager>();
            ErrorHandler.ThrowOnFailure(view.GetBuffer(out var textLines));
            ErrorHandler.ThrowOnFailure(view.GetCaretPos(out var line, out var column));
            ErrorHandler.ThrowOnFailure(textManager.NavigateToLineAndColumn(textLines, VSConstants.LOGVIEWID.Code_guid, line, column, line, column));
        }

        public async Task CloseFileAsync(string projectName, string relativeFilePath, bool saveFile)
        {
            var document = await GetOpenDocumentAsync(projectName, relativeFilePath);
            if (saveFile)
            {
                SaveFileWithExtraValidation(document);
                document.Close(EnvDTE.vsSaveChanges.vsSaveChangesYes);
            }
            else
            {
                document.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
            }
        }

        private async Task<EnvDTE.Document> GetOpenDocumentAsync(string projectName, string relativeFilePath)
        {
            var filePath = GetAbsolutePathForProjectRelativeFilePath(projectName, relativeFilePath);
            var documents = (await GetDTEAsync()).Documents.Cast<EnvDTE.Document>();
            var document = documents.FirstOrDefault(d => d.FullName == filePath);

            if (document == null)
            {
                throw new InvalidOperationException($"Open document '{filePath} could not be found. Available documents: {string.Join(", ", documents.Select(x => x.FullName))}.");
            }

            return document;
        }

        private EnvDTE.ProjectItem GetProjectItem(string projectName, string relativeFilePath)
        {
            var projects = _solution.Projects.Cast<EnvDTE.Project>();
            var project = projects.FirstOrDefault(x => x.Name == projectName);

            if (project == null)
            {
                throw new InvalidOperationException($"Project '{projectName} could not be found. Available projects: {string.Join(", ", projects.Select(x => x.Name))}.");
            }

            var projectPath = Path.GetDirectoryName(project.FullName);
            var fullFilePath = Path.Combine(projectPath, relativeFilePath);

            var projectItems = project.ProjectItems.Cast<EnvDTE.ProjectItem>();
            var document = projectItems.FirstOrDefault(d => d.FileNames[1].Equals(fullFilePath));

            if (document == null)
            {
                throw new InvalidOperationException($"File '{fullFilePath}' could not be found.  Available files: {string.Join(", ", projectItems.Select(x => x.FileNames[1]))}.");
            }

            return document;
        }

        public async Task SaveFileAsync(string projectName, string relativeFilePath)
        {
            SaveFileWithExtraValidation(await GetOpenDocumentAsync(projectName, relativeFilePath));
        }

        private static void SaveFileWithExtraValidation(EnvDTE.Document document)
        {
            var textDocument = (EnvDTE.TextDocument)document.Object(nameof(EnvDTE.TextDocument));
            var currentTextInDocument = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
            var fullPath = document.FullName;
            document.Save();
            if (File.ReadAllText(fullPath) != currentTextInDocument)
            {
                throw new InvalidOperationException("The text that we thought we were saving isn't what we saved!");
            }
        }

        private string GetAbsolutePathForProjectRelativeFilePath(string projectName, string relativeFilePath)
        {
            var project = _solution.Projects.Cast<EnvDTE.Project>().First(x => x.Name == projectName);
            var projectPath = Path.GetDirectoryName(project.FullName);
            return Path.Combine(projectPath, relativeFilePath);
        }

#if false
        public void ReloadProject(string projectRelativePath)
        {
            var solutionPath = Path.GetDirectoryName(_solution.FullName);
            var projectPath = Path.Combine(solutionPath, projectRelativePath);
            _solution.AddFromFile(projectPath);
        }

        public void RestoreNuGetPackages()
            => ExecuteCommand(WellKnownCommandNames.ProjectAndSolutionContextMenus_Solution_RestoreNuGetPackages);
#endif

        public async Task RestoreNuGetPackagesAsync(string projectName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var packageRestorer = (await GetComponentModelAsync()).GetExtensions<IVsPackageRestorer>().First();
            packageRestorer.RestorePackages(GetProject(projectName));
        }

        public async Task SaveAllAsync()
            => await ExecuteCommandAsync(WellKnownCommandNames.File_SaveAll);

#if false
        public void ShowErrorList()
            => ExecuteCommand(WellKnownCommandNames.View_ErrorList);

        public void ShowOutputWindow()
            => ExecuteCommand(WellKnownCommandNames.View_Output);

        public void UnloadProject(string projectName)
        {
            var projects = _solution.Projects;
            EnvDTE.Project project = null;
            for (int i = 1; i <= projects.Count; i++)
            {
                project = projects.Item(i);
                if (string.Compare(project.Name, projectName, StringComparison.Ordinal) == 0)
                {
                    break;
                }
            }

            _solution.Remove(project);
        }
#endif

        public async Task SelectItemAsync(string itemName)
        {
            var dte = (DTE2)await GetDTEAsync();
            var solutionExplorer = dte.ToolWindows.SolutionExplorer;

            var item = FindFirstItemRecursively(solutionExplorer.UIHierarchyItems, itemName);
            item.Select(EnvDTE.vsUISelectionType.vsUISelectionTypeSelect);
            solutionExplorer.Parent.Activate();
        }

#if false
        public void SelectItemAtPath(params string[] path)
        {
            var dte = (DTE2)GetDTE();
            var solutionExplorer = dte.ToolWindows.SolutionExplorer;

            var item = FindItemAtPath(solutionExplorer.UIHierarchyItems, path);
            item.Select(EnvDTE.vsUISelectionType.vsUISelectionTypeSelect);
            solutionExplorer.Parent.Activate();
        }

        public string[] GetChildrenOfItem(string itemName)
        {
            var dte = (DTE2)GetDTE();
            var solutionExplorer = dte.ToolWindows.SolutionExplorer;

            var item = FindFirstItemRecursively(solutionExplorer.UIHierarchyItems, itemName);

            return item.UIHierarchyItems
                .Cast<EnvDTE.UIHierarchyItem>()
                .Select(i => i.Name)
                .ToArray();
        }

        public string[] GetChildrenOfItemAtPath(params string[] path)
        {
            var dte = (DTE2)GetDTE();
            var solutionExplorer = dte.ToolWindows.SolutionExplorer;

            var item = FindItemAtPath(solutionExplorer.UIHierarchyItems, path);

            return item.UIHierarchyItems
                .Cast<EnvDTE.UIHierarchyItem>()
                .Select(i => i.Name)
                .ToArray();
        }

        private static EnvDTE.UIHierarchyItem FindItemAtPath(
            EnvDTE.UIHierarchyItems currentItems,
            string[] path)
        {
            EnvDTE.UIHierarchyItem item = null;
            foreach (var name in path)
            {
                item = currentItems.Cast<EnvDTE.UIHierarchyItem>().FirstOrDefault(i => i.Name == name);

                if (item == null)
                {
                    return null;
                }

                currentItems = item.UIHierarchyItems;
            }

            return item;
        }
#endif

        private static EnvDTE.UIHierarchyItem FindFirstItemRecursively(
            EnvDTE.UIHierarchyItems currentItems,
            string itemName)
        {
            if (currentItems == null)
            {
                return null;
            }

            foreach (var item in currentItems.Cast<EnvDTE.UIHierarchyItem>())
            {
                if (item.Name == itemName)
                {
                    return item;
                }

                var result = FindFirstItemRecursively(item.UIHierarchyItems, itemName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
