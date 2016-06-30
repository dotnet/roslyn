// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.InProcess;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
{
    public class SolutionExplorer_OutOfProc : OutOfProcComponent
    {
        private readonly SolutionExplorer_InProc _inProc;

        public SolutionExplorer_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            this._inProc = CreateInProcComponent<SolutionExplorer_InProc>(visualStudioInstance);
        }

        public void CloseSolution(bool saveFirst = false)
        {
            _inProc.CloseSolution(saveFirst);
        }

        /// <summary>
        /// Creates and loads a new solution in the host process, optionally saving the existing solution if one exists.
        /// </summary>
        public void CreateSolution(string solutionName, bool saveExistingSolutionIfExists = false)
        {
            _inProc.CreateSolution(solutionName, saveExistingSolutionIfExists);
        }

        public void OpenSolution(string path, bool saveExistingSolutionIfExists = false)
        {
            _inProc.OpenSolution(path, saveExistingSolutionIfExists);
        }

        public void AddProject(string projectName, string projectTemplate, string languageName)
        {
            _inProc.AddProject(projectName, projectTemplate, languageName);
        }

        public void CleanUpOpenSolution()
        {
            _inProc.CleanUpOpenSolution();
        }
    }
}
