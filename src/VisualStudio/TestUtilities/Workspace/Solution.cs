// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

using DteProject = EnvDTE.Project;
using DteSolution = EnvDTE80.Solution2;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>Provides a means of interacting with the current solution loaded by the host process.</summary>
    public class Solution
    {
        private static readonly IDictionary<ProjectLanguage, string> ProjectLanguages = new Dictionary<ProjectLanguage, string> {
            [ProjectLanguage.CSharp] = "CSharp",
            [ProjectLanguage.VisualBasic] = "VisualBasic"
        };

        private readonly DteSolution _dteSolution;
        private readonly string _fileName;              // Cache the filename because `_dteSolution` won't expose it unless the solution has been saved
        private readonly IDictionary<ProjectTemplate, string> _projectTemplates;

        internal Solution(DteSolution dteSolution, string fileName)
        {
            if (dteSolution == null)
            {
                throw new ArgumentNullException(nameof(dteSolution));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            _dteSolution = dteSolution;
            _fileName = fileName;

            _projectTemplates = new Dictionary<ProjectTemplate, string> {
                [ProjectTemplate.ClassLibrary] = $@"Windows\{dteSolution.DTE.LocaleID}\ClassLibrary.zip",
                [ProjectTemplate.ConsoleApplication] = "ConsoleApplication.zip",
                [ProjectTemplate.Website] = "EmptyWeb.zip",
                [ProjectTemplate.WinFormsApplication] = "WindowsApplication.zip",
                [ProjectTemplate.WpfApplication] = "WpfApplication.zip",
                [ProjectTemplate.WebApplication] = "WebApplicationProject40"
            };
        }

        public string DirectoryName => Path.GetDirectoryName(FileName);

        public DteSolution DteSolution => _dteSolution;

        public string FileName
        {
            get
            {
                var solutionFullName = IntegrationHelper.RetryRpcCall(() => _dteSolution.FullName);
                return string.IsNullOrEmpty(solutionFullName) ? _fileName : solutionFullName;
            }
        }

        public Project AddProject(string projectName, ProjectTemplate projectTemplate, ProjectLanguage projectLanguage)
        {
            var projectPath = Path.Combine(DirectoryName, projectName);
            var projectTemplatePath = GetProjectTemplatePath(projectTemplate, projectLanguage);

            var dteProject = IntegrationHelper.RetryRpcCall(() => _dteSolution.AddFromTemplate(projectTemplatePath, projectPath, projectName, Exclusive: false));

            if (dteProject == null)
            {
                dteProject = GetDteProject(projectName);
            }

            return new Project(dteProject, this, projectLanguage);
        }

        public Project GetProject(string projectName, ProjectLanguage projectLanguage)
        {
            DteProject dteProject = GetDteProject(projectName);
            return new Project(dteProject, this, projectLanguage);
        }

        private DteProject GetDteProject(string projectName)
        {
            var dteSolutionProjects = IntegrationHelper.RetryRpcCall(() => _dteSolution.Projects);

            foreach (DteProject project in dteSolutionProjects)
            {
                var dteProjectName = IntegrationHelper.RetryRpcCall(() => project.Name);

                if (dteProjectName == projectName)
                {
                    return project;
                }
            }

            throw new Exception($"The specified project could not be found. Project name: '{projectName}'");
        }

        public void Save()
        {
            Directory.CreateDirectory(DirectoryName);
            IntegrationHelper.RetryRpcCall(() => _dteSolution.SaveAs(FileName));
        }

        // TODO: Adjust language name based on whether we are using a web template
        private string GetProjectTemplatePath(ProjectTemplate projectTemplate, ProjectLanguage projectLanguage)
            => IntegrationHelper.RetryRpcCall(() => _dteSolution.GetProjectTemplate(_projectTemplates[projectTemplate], ProjectLanguages[projectLanguage]));
    }
}
