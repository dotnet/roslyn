// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.Test.Utilities.InProcess;

namespace Roslyn.VisualStudio.Test.Utilities.OutOfProcess
{
    public class SolutionExplorer_OutOfProc : OutOfProcComponent<SolutionExplorer_InProc>
    {
        public SolutionExplorer_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
        }

        public void CloseSolution(bool saveFirst = false) => InProc.CloseSolution(saveFirst);

        /// <summary>
        /// Creates and loads a new solution in the host process, optionally saving the existing solution if one exists.
        /// </summary>
        public void CreateSolution(string solutionName, bool saveExistingSolutionIfExists = false)
        {
            InProc.CreateSolution(solutionName, saveExistingSolutionIfExists);
        }

        public void OpenSolution(string path, bool saveExistingSolutionIfExists = false)
        {
            InProc.OpenSolution(path, saveExistingSolutionIfExists);
        }

        public void AddProject(string projectName, string projectTemplate, string languageName)
        {
            InProc.AddProject(projectName, projectTemplate, languageName);
        }

        public void CleanUpOpenSolution()
        {
            InProc.CleanUpOpenSolution();
        }
    }
}
