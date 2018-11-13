// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using VSLangProj;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class SolutionExplorer_InProc : InProcComponent
    {
        private Solution2 _solution;
        private string _fileName;

        private static readonly IDictionary<string, string> _csharpProjectTemplates = InitializeCSharpProjectTemplates();
        private static readonly IDictionary<string, string> _visualBasicProjectTemplates = InitializeVisualBasicProjectTemplates();

        private SolutionExplorer_InProc() { }

        public static SolutionExplorer_InProc Create()
            => new SolutionExplorer_InProc();

        private static IDictionary<string, string> InitializeCSharpProjectTemplates()
        {
            var localeID = GetDTE().LocaleID;

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

        private static IDictionary<string, string> InitializeVisualBasicProjectTemplates()
        {
            var localeID = GetDTE().LocaleID;

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

        public void AddMetadataReference(string assemblyName, string projectName)
        {
            var project = GetProject(projectName);
            var vsproject = ((VSProject)project.Object);
            vsproject.References.Add(assemblyName);
        }

        public void RemoveMetadataReference(string assemblyName, string projectName)
        {
            var project = GetProject(projectName);
            var reference = ((VSProject)project.Object).References.Cast<Reference>().Where(x => x.Name == assemblyName).First();
            reference.Remove();
        }

        public string DirectoryName => Path.GetDirectoryName(SolutionFileFullPath);

        public string SolutionFileFullPath
        {
            get
            {
                var solutionFullName = _solution.FullName;

                return string.IsNullOrEmpty(solutionFullName)
                    ? _fileName
                    : solutionFullName;
            }
        }

        public void CloseSolution(bool saveFirst = false)
            => GetDTE().Solution.Close(saveFirst);

        /// <summary>
        /// Creates and loads a new solution in the host process, optionally saving the existing solution if one exists.
        /// </summary>
        public void CreateSolution(string solutionName, bool saveExistingSolutionIfExists = false)
        {
            var dte = GetDTE();

            if (dte.Solution.IsOpen)
            {
                CloseSolution(saveExistingSolutionIfExists);
            }

            string solutionPath = IntegrationHelper.CreateTemporaryPath();
            IntegrationHelper.DeleteDirectoryRecursively(solutionPath);

            dte.Solution.Create(solutionPath, solutionName);

            _solution = (Solution2)dte.Solution;
            _fileName = Path.Combine(solutionPath, $"{solutionName}.sln");
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

        public void EditProjectFile(string projectName)
        {
            var solutionExplorer = ((DTE2)GetDTE()).ToolWindows.SolutionExplorer;
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
            ExecuteCommand("Project.EditProjectFile");
        }

        public string[] GetProjectReferences(string projectName)
        {
            var project = GetProject(projectName);
            var references = ((VSProject)project.Object).References.Cast<Reference>().Where(x => x.SourceProject != null).Select(x => x.Name).ToArray();
            return references;
        }

        public void CreateSolution(string solutionName, string solutionElementString)
        {
            var solutionElement = XElement.Parse(solutionElementString);
            if (solutionElement.Name != "Solution")
            {
                throw new ArgumentException(nameof(solutionElementString));
            }
            CreateSolution(solutionName);

            foreach (var projectElement in solutionElement.Elements("Project"))
            {
                CreateProject(projectElement);
            }

            foreach (var projectElement in solutionElement.Elements("Project"))
            {
                var projectReferences = projectElement.Attribute("ProjectReferences")?.Value;
                if (projectReferences != null)
                {
                    var projectName = projectElement.Attribute("ProjectName").Value;
                    foreach (var projectReference in projectReferences.Split(';'))
                    {
                        AddProjectReference(projectName, projectReference);
                    }
                }
            }
        }

        private void CreateProject(XElement projectElement)
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
            var projectTemplatePath = GetProjectTemplatePath(projectTemplate, ConvertLanguageName(languageName));

            _solution.AddFromTemplate(projectTemplatePath, projectPath, projectName, Exclusive: false);
            foreach (var documentElement in projectElement.Elements("Document"))
            {
                var fileName = documentElement.Attribute("FileName").Value;
                UpdateOrAddFile(projectName, fileName, contents: documentElement.Value);
            }
        }

        public void AddProjectReference(string projectName, string projectToReferenceName)
        {
            var project = GetProject(projectName);
            var projectToReference = GetProject(projectToReferenceName);
            ((VSProject)project.Object).References.AddProject(projectToReference);
        }

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

        public void RemoveProjectReference(string projectName, string projectReferenceName)
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
        }

        public void OpenSolution(string path, bool saveExistingSolutionIfExists = false)
        {
            var dte = GetDTE();

            if (dte.Solution.IsOpen)
            {
                CloseSolution(saveExistingSolutionIfExists);
            }
            dte.Solution.Open(path);

            _solution = (EnvDTE80.Solution2)dte.Solution;
            _fileName = path;
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

        public void AddProject(string projectName, string projectTemplate, string languageName)
        {
            var projectPath = Path.Combine(DirectoryName, projectName);

            var projectTemplatePath = GetProjectTemplatePath(projectTemplate, ConvertLanguageName(languageName));

            _solution.AddFromTemplate(projectTemplatePath, projectPath, projectName, Exclusive: false);
        }

        // TODO: Adjust language name based on whether we are using a web template
        private string GetProjectTemplatePath(string projectTemplate, string languageName)
        {
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

        public void CleanUpOpenSolution()
            => InvokeOnUIThread(() =>
            {
                var dte = GetDTE();
                dte.Documents.CloseAll(EnvDTE.vsSaveChanges.vsSaveChangesNo);

                if (dte.Solution != null)
                {
                    var directoriesToDelete = new List<string>();

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

                    dte.Solution.Close(SaveFirst: false);

                    foreach (var directoryToDelete in directoriesToDelete)
                    {
                        IntegrationHelper.TryDeleteDirectoryRecursively(directoryToDelete);
                    }
                }
            });

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
        public void UpdateOrAddFile(string projectName, string fileName, string contents = null, bool open = false)
        {
            var project = GetProject(projectName);
            if (project.ProjectItems.Cast<EnvDTE.ProjectItem>().Any(x => x.Name == fileName))
            {
                UpdateFile(projectName, fileName, contents, open);
            }
            else
            {
                AddFile(projectName, fileName, contents, open);
            }
        }

        /// <summary>
        /// Update the given file to have the contents given.
        /// </summary>
        /// <param name="projectName">The project that contains the file.</param>
        /// <param name="fileName">The name of the file to update or add.</param>
        /// <param name="contents">The contents of the file to overwrite. Empty string is used if null is passed.</param>
        /// <param name="open">Whether to open the file after it has been updated.</param>
        public void UpdateFile(string projectName, string fileName, string contents = null, bool open = false)
        {
            void SetText(string text)
            {
                InvokeOnUIThread(() =>
                {
                    // The active text view might not have finished composing yet, waiting for the application to 'idle'
                    // means that it is done pumping messages (including WM_PAINT) and the window should return the correct text view
                    WaitForApplicationIdle();

                    var vsTextManager = GetGlobalService<SVsTextManager, IVsTextManager>();
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
                });
            }

            OpenFile(projectName, fileName);
            SetText(contents ?? string.Empty);
            CloseFile(projectName, fileName, saveFile: true);
            if (open)
            {
                OpenFile(projectName, fileName);
            }
        }

        /// <summary>
        /// Add new file to project.
        /// </summary>
        /// <param name="projectName">The project that contains the file.</param>
        /// <param name="fileName">The name of the file to add.</param>
        /// <param name="contents">The contents of the file to overwrite. An empty file is create if null is passed.</param>
        /// <param name="open">Whether to open the file after it has been updated.</param>
        public void AddFile(string projectName, string fileName, string contents = null, bool open = false)
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
                OpenFile(projectName, fileName);
            }
        }

        /// <summary>
        /// Adds a new standalone file to the Miscellaneous Files workspace.
        /// </summary>
        /// <param name="fileName">The name of the file to add.</param>
        public void AddStandaloneFile(string fileName)
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

            GetDTE().ItemOperations.NewFile(itemTemplate, fileName);
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

        public void BuildSolution(bool waitForBuildToFinish)
        {
            var buildOutputWindowPane = GetBuildOutputWindowPane();
            buildOutputWindowPane.Clear();
            ExecuteCommand(WellKnownCommandNames.Build_BuildSolution);
            WaitForBuildToFinish(buildOutputWindowPane);
        }

        public void ClearBuildOutputWindowPane()
        {
            var buildOutputWindowPane = GetBuildOutputWindowPane();
            buildOutputWindowPane.Clear();
        }

        public void WaitForBuildToFinish()
        {
            var buildOutputWindowPane = GetBuildOutputWindowPane();
            WaitForBuildToFinish(buildOutputWindowPane);
        }

        private EnvDTE.OutputWindowPane GetBuildOutputWindowPane()
        {
            var dte = (DTE2)GetDTE();
            var outputWindow = dte.ToolWindows.OutputWindow;
            return outputWindow.OutputWindowPanes.Item("Build");
        }

        private void WaitForBuildToFinish(EnvDTE.OutputWindowPane buildOutputWindowPane)
        {
            var buildManager = GetGlobalService<SVsSolutionBuildManager, IVsSolutionBuildManager2>();
            using (var semaphore = new SemaphoreSlim(1))
            using (var solutionEvents = new UpdateSolutionEvents(buildManager))
            {
                semaphore.Wait();
                void @event(bool succeeded, bool modified, bool canceled) => semaphore.Release();
                solutionEvents.OnUpdateSolutionDone += @event;
                try
                {
                    semaphore.Wait();
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

        public void OpenFileWithDesigner(string projectName, string relativeFilePath)
        {
            var projectItem = GetProjectItem(projectName, relativeFilePath);
            var window = projectItem.Open(EnvDTE.Constants.vsViewKindDesigner);
            window.Activate();

            var dte = GetDTE();
            while (!dte.ActiveWindow.Caption.Contains(projectItem.Name))
            {
                Thread.Yield();
            }
        }

        public void OpenFile(string projectName, string relativeFilePath)
        {
            var filePath = GetAbsolutePathForProjectRelativeFilePath(projectName, relativeFilePath);
            VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, filePath, VSConstants.LOGVIEWID.Code_guid, out _, out _, out _, out var view);

            // Reliably set focus using NavigateToLineAndColumn
            var textManager = GetGlobalService<SVsTextManager, IVsTextManager>();
            ErrorHandler.ThrowOnFailure(view.GetBuffer(out var textLines));
            ErrorHandler.ThrowOnFailure(view.GetCaretPos(out var line, out var column));
            ErrorHandler.ThrowOnFailure(textManager.NavigateToLineAndColumn(textLines, VSConstants.LOGVIEWID.Code_guid, line, column, line, column));
        }

        public void CloseFile(string projectName, string relativeFilePath, bool saveFile)
        {
            var document = GetOpenDocument(projectName, relativeFilePath);
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

        private EnvDTE.Document GetOpenDocument(string projectName, string relativeFilePath)
        {
            var filePath = GetAbsolutePathForProjectRelativeFilePath(projectName, relativeFilePath);
            var documents = GetDTE().Documents.Cast<EnvDTE.Document>();
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

        public void SaveFile(string projectName, string relativeFilePath)
        {
            SaveFileWithExtraValidation(GetOpenDocument(projectName, relativeFilePath));
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

        public void ReloadProject(string projectRelativePath)
        {
            var solutionPath = Path.GetDirectoryName(_solution.FullName);
            var projectPath = Path.Combine(solutionPath, projectRelativePath);
            _solution.AddFromFile(projectPath);
        }

        public void RestoreNuGetPackages()
            => ExecuteCommand(WellKnownCommandNames.ProjectAndSolutionContextMenus_Solution_RestoreNuGetPackages);

        public void SaveAll()
            => ExecuteCommand(WellKnownCommandNames.File_SaveAll);

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

        public void SelectItem(string itemName)
        {
            var dte = (DTE2)GetDTE();
            var solutionExplorer = dte.ToolWindows.SolutionExplorer;

            var item = FindFirstItemRecursively(solutionExplorer.UIHierarchyItems, itemName);
            item.Select(EnvDTE.vsUISelectionType.vsUISelectionTypeSelect);
            solutionExplorer.Parent.Activate();
        }

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
