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

        public void SaveAll()
            => ExecuteCommand(WellKnownCommandNames.File_SaveAll);

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
