// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using EnvDTE80;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public class SolutionExplorer
    {
        private IntegrationHost _host;
        private Solution _solution;
        // TODO: Integrate with the SolutionExplorer service

        internal SolutionExplorer(IntegrationHost host)
        {
            _host = host;
        }

        public Solution Solution
            => _solution;

        public Solution CreateSolution(string solutionName)
        {
            var dteSolution = _host.Dte.Solution;

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
            _host.Dte.Solution.Close(saveFirst);
        }
    }
}
