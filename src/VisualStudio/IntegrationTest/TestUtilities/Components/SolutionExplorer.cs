// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Components
{
    internal class SolutionExplorer
    {
        private SolutionService _solution;
        private string _fileName;

        private readonly SolutionExplorer_InProc _inProc;
        private readonly VisualStudioHost _visualStudioHost;

        public SolutionExplorer(SolutionExplorer_InProc inProc, VisualStudioHost visualStudioHost)
        {
            _inProc = inProc;
            _visualStudioHost = visualStudioHost;
        }

        public string DirectoryName => Path.GetDirectoryName(SolutionFileFullPath);

        public string SolutionFileFullPath
        {
            get
            {
                var solutionFullName = _solution.Name;

                return string.IsNullOrEmpty(solutionFullName)
                    ? _fileName
                    : solutionFullName;
            }
        }

        public void CreateSolution(string solutionName, XElement solutionElement)
        {
            if (solutionElement.Name != "Solution")
            {
                throw new ArgumentException(nameof(solutionElement));
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

        /// <summary>
        /// Creates and loads a new solution in the host process, optionally saving the existing solution if one exists.
        /// </summary>
        public void CreateSolution(string solutionName, bool saveExistingSolutionIfExists = false)
        {
            var solution = _visualStudioHost.ObjectModel.Solution;

            if (solution.IsOpen)
            {
                CloseSolution(saveExistingSolutionIfExists);
            }

            string solutionPath = IntegrationHelper.CreateTemporaryPath();
            IntegrationHelper.DeleteDirectoryRecursively(solutionPath);

            solution.CreateEmptySolution(solutionName, solutionPath);

            _solution = solution;
            _fileName = Path.Combine(solutionPath, $"{solutionName}.sln");
        }

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
            if (project.ProjectItems.Any(x => x.Name == fileName))
            {
                UpdateFile(projectName, fileName, contents, open);
            }
            else
            {
                AddFile(projectName, fileName, contents, open);
            }
        }

        public void OpenSolution(string path, bool saveExistingSolutionIfExists = false)
        {
            var solution = _visualStudioHost.ObjectModel.Solution;

            if (solution.IsOpen)
            {
                CloseSolution(saveExistingSolutionIfExists);
            }

            solution.Open(path);

            _solution = solution;
            _fileName = path;
        }

        public void CloseSolution(bool saveFirst = false)
        {
            var solution = _visualStudioHost.ObjectModel.Solution;
            if (saveFirst)
            {
                solution.SaveAndClose();
            }
            else
            {
                solution.Close();
            }
        }

        public void AddReference(string projectName, string fullyQualifiedAssemblyName)
        {
            var project = GetProject(projectName);
            project.References.AddDotNetReference(fullyQualifiedAssemblyName);
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
            var project = _solution.Projects[projectName];
            var document = project[fileName].GetDocumentAsTextEditor();

            document.Editor.Selection.SelectAll();
            document.Editor.Edit.InsertText(contents ?? string.Empty);

            document.Save();
            
            if (!open)
            {
                document.Close();
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
            var projectDirectory = project.ProjectDirectory;
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

            var projectItem = project.AddProjectItemFromFile(filePath);

            if (open)
            {
                _inProc.OpenFile(projectName, fileName);
            }
        }

        public void OpenFile(string projectName, string fileName)
        {
            _solution[projectName][fileName].Open();
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
            AddProject(projectName: projectName, projectPath: projectPath, projectTemplate: projectTemplate, languageName: languageName);
            foreach (var documentElement in projectElement.Elements("Document"))
            {
                var fileName = documentElement.Attribute("FileName").Value;
                UpdateOrAddFile(projectName, fileName, contents: documentElement.Value);
            }
        }

        private static ProjectTemplate GetTemplate(string projectTemplate)
        {
            switch(projectTemplate)
            {
                case WellKnownProjectTemplates.ClassLibrary: return ProjectTemplate.ClassLibrary;
                case WellKnownProjectTemplates.ConsoleApplication: return ProjectTemplate.ConsoleApplication;
                case WellKnownProjectTemplates.Website: return ProjectTemplate.WebSite;
                case WellKnownProjectTemplates.WinFormsApplication: return ProjectTemplate.WindowsFormsApplication;
                case WellKnownProjectTemplates.WpfApplication: return ProjectTemplate.WPFApplication;
                case WellKnownProjectTemplates.WebApplication: return ProjectTemplate.WebApplication;
                case WellKnownProjectTemplates.CSharpNetCoreClassLibrary: return ProjectTemplate.NetCoreClassLib; //  "Microsoft.CSharp.NETCore.ClassLibrary";
                case WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary: return ProjectTemplate.NetCoreClassLib; //"Microsoft.VisualBasic.NETCore.ClassLibrary";
                case WellKnownProjectTemplates.CSharpNetCoreConsoleApplication: return ProjectTemplate.NetCoreConsoleApp; // "Microsoft.CSharp.NETCore.ConsoleApplication";
                case WellKnownProjectTemplates.VisualBasicNetCoreConsoleApplication: return ProjectTemplate.NetCoreConsoleApp; //  "Microsoft.VisualBasic.NETCore.ConsoleApplication";
                case WellKnownProjectTemplates.CSharpNetCoreUnitTest: return ProjectTemplate.NetCoreUnitTest;
                case WellKnownProjectTemplates.CSharpNetCoreMSTest: return ProjectTemplate.NetCoreXUnitTest; // ??  "Microsoft.CSharp.NETCore.MSUnitTest"; 
                default: return default;
            }
        }

        public void AddProject(string projectName, string projectTemplate, string languageName)
            => AddProject(projectName: projectName, projectPath: Path.Combine(DirectoryName, projectName), projectTemplate: projectTemplate, languageName: languageName);

        public void AddProject(string projectName, string projectPath, string projectTemplate, string languageName)
        {
            var template = GetTemplate(projectTemplate);
            _solution.AddProject(template: template, language: ConvertLanguageName(languageName), projectName: projectName, projectPath: projectPath);
            _solution.Save();
        }

        public void AddProjectReference(string projectName, string projectToReferenceName)
        {
            var project = GetProject(projectName);
            var projectToReference = GetProject(projectToReferenceName);
            project.References.AddProjectReference(projectToReference);
        }

        private static ProjectLanguage ConvertLanguageName(string languageName)
        {
            const string CSharp = nameof(CSharp);
            const string VisualBasic = nameof(VisualBasic);

            switch (languageName)
            {
                case LanguageNames.CSharp:
                    return ProjectLanguage.CSharp;
                case LanguageNames.VisualBasic:
                    return ProjectLanguage.VB;
                default:
                    throw new ArgumentException($"{languageName} is not supported.", nameof(languageName));
            }
        }

        private ProjectTestExtension GetProject(string nameOrFileName)
            => _solution.Projects[nameOrFileName];
    }
}
