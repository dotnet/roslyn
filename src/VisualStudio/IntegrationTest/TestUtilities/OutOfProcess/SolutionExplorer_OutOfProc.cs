// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Creates and loads a new solution in the host process, optionally saving the existing solution if one exists.
        /// </summary>
        public void CreateSolution(string solutionName, bool saveExistingSolutionIfExists = false)
            => _inProc.CreateSolution(solutionName, saveExistingSolutionIfExists);

        public void OpenSolution(string path, bool saveExistingSolutionIfExists = false)
            => _inProc.OpenSolution(path, saveExistingSolutionIfExists);

        public void AddProject(string projectName, string projectTemplate, string languageName)
            => _inProc.AddProject(projectName, projectTemplate, languageName);

        public void CleanUpOpenSolution()
            => _inProc.CleanUpOpenSolution();

        public int ErrorListErrorCount
            => _inProc.GetErrorListErrorCount();

        public void AddFile(string projectName, string fileName, string contents = null, bool open = false)
            => _inProc.AddFile(projectName, fileName, contents, open);

        public void SetFileContents(string projectName, string fileName, string contents)
            => _inProc.SetFileContents(projectName, fileName, contents);

        public string GetFileContents(string projectName, string fileName)
            => _inProc.GetFileContents(projectName, fileName);

        public void BuildSolution(bool waitForBuildToFinish = false)
            => _inProc.BuildSolution(waitForBuildToFinish);

        public void OpenFile(string projectName, string fileName)
            => _inProc.OpenFile(projectName, fileName);

        public void ReloadProject(string projectName)
            => _inProc.ReloadProject(projectName);

        public void RestoreNuGetPackages()
            => _inProc.RestoreNuGetPackages();

        public void SaveAll()
            => _inProc.SaveAll();

        public void ShowErrorList()
            => _inProc.ShowErrorList();

        public void ShowOutputWindow()
            => _inProc.ShowOutputWindow();

        public void UnloadProject(string projectName)
            => _inProc.UnloadProject(projectName);

        public void WaitForNoErrorsInErrorList()
            => _inProc.WaitForNoErrorsInErrorList();
    }
}
