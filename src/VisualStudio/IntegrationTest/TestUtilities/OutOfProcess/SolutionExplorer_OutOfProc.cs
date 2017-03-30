// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class SolutionExplorer_OutOfProc : OutOfProcComponent
    {
        private readonly SolutionExplorer_InProc _inProc;

        public SolutionExplorer_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<SolutionExplorer_InProc>(visualStudioInstance);
        }

        public void CloseSolution(bool saveFirst = false)
            => _inProc.CloseSolution(saveFirst);

        /// <summary>
        /// The full file path to the solution file.
        /// </summary>
        public string SolutionFileFullPath => _inProc.SolutionFileFullPath;

        /// <summary>
        /// Creates and loads a new solution in the host process, optionally saving the existing solution if one exists.
        /// </summary>
        public void CreateSolution(string solutionName, bool saveExistingSolutionIfExists = false)
            => _inProc.CreateSolution(solutionName, saveExistingSolutionIfExists);

        public void CreateSolution(string solutionName, XElement solutionElement)
            => _inProc.CreateSolution(solutionName, solutionElement.ToString());

        public void OpenSolution(string path, bool saveExistingSolutionIfExists = false)
            => _inProc.OpenSolution(path, saveExistingSolutionIfExists);

        public void AddProject(string projectName, string projectTemplate, string languageName)
            => _inProc.AddProject(projectName, projectTemplate, languageName);

        public void AddProjectReference(string fromProjectName, string toProjectName)
            => _inProc.AddProjectReference(fromProjectName, toProjectName);

        public void AddReference(string projectName, string fullyQualifiedAssemblyName)
            => _inProc.AddReference(projectName, fullyQualifiedAssemblyName);

        public void RemoveProjectReference(string projectName, string projectReferenceName)
            => _inProc.RemoveProjectReference(projectName, projectReferenceName);

        public void AddMetadataReference(string assemblyName, string projectName)
            => _inProc.AddMetadataReference(assemblyName, projectName);

        public void RemoveMetadataReference(string assemblyName, string projectName)
            => _inProc.RemoveMetadataReference(assemblyName, projectName);

        public void CleanUpOpenSolution()
            => _inProc.CleanUpOpenSolution();

        public void AddFile(string projectName, string fileName, string contents = null, bool open = false)
            => _inProc.AddFile(projectName, fileName, contents, open);

        public void SetFileContents(string projectName, string fileName, string contents)
            => _inProc.SetFileContents(projectName, fileName, contents);

        public string GetFileContents(string projectName, string fileName)
            => _inProc.GetFileContents(projectName, fileName);

        public void BuildSolution(bool waitForBuildToFinish)
            => _inProc.BuildSolution(waitForBuildToFinish);

        public void OpenFileWithDesigner(string projectName, string fileName)
            => _inProc.OpenFileWithDesigner(projectName, fileName);

        public void OpenFile(string projectName, string fileName)
            => _inProc.OpenFile(projectName, fileName);

        public void CloseFile(string projectName, string fileName, bool saveFile)
            => _inProc.CloseFile(projectName, fileName, saveFile);

        public void SaveFile(string projectName, string fileName)
            => _inProc.SaveFile(projectName, fileName);

        public void ReloadProject(string projectName)
            => _inProc.ReloadProject(projectName);

        public void RestoreNuGetPackages()
            => _inProc.RestoreNuGetPackages();

        public void SaveAll()
            => _inProc.SaveAll();

        public void ShowOutputWindow()
            => _inProc.ShowOutputWindow();

        public void UnloadProject(string projectName)
            => _inProc.UnloadProject(projectName);

        public string[] GetProjectReferences(string projectName)
            => _inProc.GetProjectReferences(projectName);

        public string[] GetAssemblyReferences(string projectName)
            => _inProc.GetAssemblyReferences(projectName);

        public void SelectItem(string itemName)
            => _inProc.SelectItem(itemName);

        public void ClearBuildOutputWindowPane()
            => _inProc.ClearBuildOutputWindowPane();

        public void WaitForBuildToFinish()
            => _inProc.WaitForBuildToFinish();

        public void EditProjectFile(string projectName)
            => _inProc.EditProjectFile(projectName);
    }
}