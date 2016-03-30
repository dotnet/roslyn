// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using EnvDTE80;

namespace Roslyn.VisualStudio.Test.Utilities
{
    /// <summary>Provides a means of interacting with the Solution Explorer in the Visual Studio host.</summary>
    public class SolutionExplorer
    {
        private readonly VisualStudioInstance _visualStudio;
        private Solution _solution;
        // TODO: Integrate with the SolutionExplorer service

        internal SolutionExplorer(VisualStudioInstance visualStudio)
        {
            _visualStudio = visualStudio;
        }

        /// <summary>Gets the solution currently loaded in the host process or <c>null</c> if no solution is currently loaded.</summary>
        public Solution Solution
            => _solution;

        /// <summary>Creates and loads a new solution in the host process, closing the existing solution without saving if one exists.</summary>
        public Solution CreateSolution(string solutionName)
        {
            var dteSolution = _visualStudio.Dte.Solution;

            if (dteSolution.IsOpen)
            {
                CloseSolution(saveFirst: false);
            }

            var solutionPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            dteSolution.Create(solutionPath, solutionName);

            _solution = new Solution((Solution2)(dteSolution), Path.Combine(solutionPath, $"{solutionName}.sln"));
            return _solution;
        }

        public void CloseSolution(bool saveFirst = false)
        {
            _visualStudio.Dte.Solution.Close(saveFirst);
        }
    }
}
