﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using EnvDTE80;
using Microsoft.CodeAnalysis;
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

        public string DirectoryName => Path.GetDirectoryName(FileName);

        public string FileName
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
                    directoriesToDelete.Add(Path.GetDirectoryName(project.FullName));
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
                projectItem.Open();
            }
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
            var dte = (DTE2)GetDTE();
            var outputWindow = dte.ToolWindows.OutputWindow;

            var buildOutputWindowPane = outputWindow.OutputWindowPanes.Item("Build");
            buildOutputWindowPane.Clear();

            ExecuteCommand("Build.BuildSolution");

            if (waitForBuildToFinish)
            {
                var textDocument = buildOutputWindowPane.TextDocument;
                var textDocumentSelection = textDocument.Selection;

                var buildFinishedRegex = new Regex(@"^========== Build: \d+ succeeded, \d+ failed, \d+ up-to-date, \d+ skipped ==========$", RegexOptions.Compiled);

                do
                {
                    Thread.Yield();

                    try
                    {
                        textDocumentSelection.GotoLine(textDocument.EndPoint.Line - 1, Select: true);
                    }
                    catch (ArgumentException)
                    {
                        // Its possible that the window will still be initializing, clearing,
                        // etc... We should ignore those errors and just try again
                    }
                }
                while (!buildFinishedRegex.IsMatch(textDocumentSelection.Text));
            }
        }

        public int GetErrorListErrorCount()
        {
            var dte = (DTE2)GetDTE();
            var errorList = dte.ToolWindows.ErrorList;

            var errorItems = errorList.ErrorItems;
            var errorItemsCount = errorItems.Count;

            var errorCount = 0;

            try
            {
                for (var index = 1; index <= errorItemsCount; index++)
                {
                    var errorItem = errorItems.Item(index);

                    if (errorItem.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh)
                    {
                        errorCount += 1;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // It is entirely possible that the items in the error list are modified
                // after we start iterating, in which case we want to try again.
                return GetErrorListErrorCount();
            }

            return errorCount;
        }

        public void OpenFileWithDesigner(string projectName, string relativeFilePath)
        {
            var filePath = GetFilePath(projectName, relativeFilePath);
            var fileName = Path.GetFileName(filePath);
            var project = _solution.Projects.Cast<EnvDTE.Project>().First(x => x.Name == projectName);
            var window = project.ProjectItems.Item(fileName).Open(EnvDTE.Constants.vsViewKindDesigner);
            window.Activate();

            var dte = GetDTE();
            while (!dte.ActiveWindow.Caption.Contains(fileName))
            {
                Thread.Yield();
            }
        }

        public void OpenFile(string projectName, string relativeFilePath)
        {
            var filePath = GetFilePath(projectName, relativeFilePath);
            var fileName = Path.GetFileName(filePath);

            ExecuteCommand("File.OpenFile", filePath);

            var dte = GetDTE();
            while (!dte.ActiveWindow.Caption.Contains(fileName))
            {
                Thread.Yield();
            }
        }

        public void CloseFile(string projectName, string relativeFilePath, bool saveFile)
        {
            var filePath = GetFilePath(projectName, relativeFilePath);
            var fileName = Path.GetFileName(filePath);

            var dte = GetDTE();
            var documents = dte.Documents.Cast<EnvDTE.Document>();
            var fileToClose = documents.FirstOrDefault(document => document.Name.Equals(fileName));
            if (fileToClose == null)
            {
                throw new InvalidOperationException($"File '{fileName}' not closed because it couldn't be found.  Available files: {string.Join(", ", documents.Select(x => x.Name))}.");
            }
            if (saveFile)
            {
                SaveFile(fileName);
                fileToClose.Close(EnvDTE.vsSaveChanges.vsSaveChangesYes);
            }
            else
            {
                fileToClose.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
            }
        }

        public void SaveFile(string projectName, string relativeFilePath)
        {
            var filePath = GetFilePath(projectName, relativeFilePath);
            var fileName = Path.GetFileName(filePath);
            SaveFile(fileName);
        }

        private static void SaveFile(string fileName)
        {
            var dte = GetDTE();
            var fileToSave = dte.Documents.Cast<EnvDTE.Document>().FirstOrDefault(document => document.Name.Equals(fileName));
            if (fileToSave == null)
            {
                var fileNames = dte.Documents.Cast<EnvDTE.Document>().Select(d => d.Name);
                throw new InvalidOperationException($"File '{fileName}' not saved because it couldn't be found.  Available files: {string.Join(", ", fileNames)}.");
            }
            var textDocument = (EnvDTE.TextDocument)fileToSave.Object(nameof(EnvDTE.TextDocument));
            var currentTextInDocument = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
            var fullPath = fileToSave.FullName;
            fileToSave.Save();
            if (File.ReadAllText(fullPath) != currentTextInDocument)
            {
                throw new InvalidOperationException("The text that we thought we were saving isn't what we saved!");
            }
        }

        private string GetFilePath(string projectName, string relativeFilePath)
        {
            var project = _solution.Projects.Cast<EnvDTE.Project>().First(x => x.Name == projectName);
            var projectPath = Path.GetDirectoryName(project.FullName);
            return Path.Combine(projectPath, relativeFilePath);
        }

        public void ReloadProject(string projectName)
        {
            var solutionPath = Path.GetDirectoryName(_solution.FullName);
            var projectPath = Path.Combine(solutionPath, projectName);
            _solution.AddFromFile(projectPath);
        }

        public void RestoreNuGetPackages()
            => ExecuteCommand("ProjectAndSolutionContextMenus.Solution.RestoreNuGetPackages");

        public void SaveAll()
            => ExecuteCommand("File.SaveAll");

        public void ShowErrorList()
            => ExecuteCommand("View.ErrorList");

        public void ShowOutputWindow()
            => ExecuteCommand("View.Output");

        public void UnloadProject(string projectName)
        {
            var project = _solution.Projects.Item(projectName);
            _solution.Remove(project);
        }

        public void WaitForNoErrorsInErrorList()
        {
            while (GetErrorListErrorCount() != 0)
            {
                Thread.Yield();
            }
        }
    }
}
