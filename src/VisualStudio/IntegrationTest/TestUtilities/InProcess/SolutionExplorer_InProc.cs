// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using EnvDTE80;
using Microsoft.CodeAnalysis;

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

            return new Dictionary<string, string> {
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

            return new Dictionary<string, string> {
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

            var solutionPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            IntegrationHelper.DeleteDirectoryRecursively(solutionPath);

            dte.Solution.Create(solutionPath, solutionName);

            _solution = (EnvDTE80.Solution2)dte.Solution;
            _fileName = Path.Combine(solutionPath, $"{solutionName}.sln");
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

        public void AddFile(string projectName, string fileName, string contents = null, bool open = false)
        {
            var project = GetProject(projectName);

            var projectDirectory = Path.GetDirectoryName(project.FullName);
            var filePath = Path.Combine(projectDirectory, fileName);

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

        public void OpenFile(string projectName, string relativeFilePath)
        {
            var project = _solution.Projects.Item(projectName);
            var projectPath = Path.GetDirectoryName(project.FullName);

            var filePath = Path.Combine(projectPath, relativeFilePath);
            ExecuteCommand("File.OpenFile", filePath);

            var dte = GetDTE();
            var fileName = Path.GetFileName(filePath);

            while (!dte.ActiveWindow.Caption.Contains(fileName))
            {
                Thread.Yield();
            }
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
