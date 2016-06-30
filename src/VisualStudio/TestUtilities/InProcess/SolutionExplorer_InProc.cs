// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Roslyn.VisualStudio.Test.Utilities.InProcess
{
    internal class SolutionExplorer_InProc : InProcComponent
    {
        private EnvDTE80.Solution2 _solution;
        private string _fileName;

        private static readonly IDictionary<string, string> _projectTemplates = InitializeProjectTemplates();

        private SolutionExplorer_InProc() { }

        public static SolutionExplorer_InProc Create()
        {
            return new SolutionExplorer_InProc();
        }

        private static IDictionary<string, string> InitializeProjectTemplates()
        {
            var localeID = GetDTE().LocaleID;

            return new Dictionary<string, string>
            {
                [WellKnownProjectTemplates.ClassLibrary] = $@"Windows\{localeID}\ClassLibrary.zip",
                [WellKnownProjectTemplates.ConsoleApplication] = "ConsoleApplication.zip",
                [WellKnownProjectTemplates.Website] = "EmptyWeb.zip",
                [WellKnownProjectTemplates.WinFormsApplication] = "WindowsApplication.zip",
                [WellKnownProjectTemplates.WpfApplication] = "WpfApplication.zip",
                [WellKnownProjectTemplates.WebApplication] = "WebApplicationProject40"
            };
        }

        public string DirectoryName
        {
            get { return Path.GetDirectoryName(FileName); }
        }

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
        {
            GetDTE().Solution.Close(saveFirst);
        }

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
            return _solution.GetProjectTemplate(_projectTemplates[projectTemplate], languageName);
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
    }
}
