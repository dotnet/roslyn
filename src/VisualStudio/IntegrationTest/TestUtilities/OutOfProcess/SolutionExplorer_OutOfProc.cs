// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class SolutionExplorer_OutOfProc : OutOfProcComponent
    {
        private readonly SolutionExplorer_InProc _inProc;
        private readonly VisualStudioInstance _instance;

        public SolutionExplorer_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _inProc = CreateInProcComponent<SolutionExplorer_InProc>(visualStudioInstance);
        }

        /// <summary>
        /// The full file path to the solution file.
        /// </summary>
        public string SolutionFileFullPath => _inProc.SolutionFileFullPath;

        /// <summary>
        /// Creates and loads a new solution in the host process, optionally saving the existing solution if one exists.
        /// </summary>
        public void CreateSolution(string solutionName, bool saveExistingSolutionIfExists = false)
            => _inProc.CreateSolution(solutionName, saveExistingSolutionIfExists);

        public void AddProject(ProjectUtils.Project projectName, string projectTemplate, string languageName)
            => _inProc.AddProject(projectName.Name, projectTemplate, languageName);

        public void CleanUpOpenSolution()
            => _inProc.CleanUpOpenSolution();

        public void AddFile(ProjectUtils.Project project, string fileName, string contents = null, bool open = false)
            => _inProc.AddFile(project.Name, fileName, contents, open);

        public void OpenFile(ProjectUtils.Project project, string fileName)
            => _inProc.OpenFile(project.Name, fileName);

        public void CloseFile(ProjectUtils.Project project, string fileName, bool saveFile)
            => _inProc.CloseFile(project.Name, fileName, saveFile);

        public void SaveAll()
            => _inProc.SaveAll();
    }
}
