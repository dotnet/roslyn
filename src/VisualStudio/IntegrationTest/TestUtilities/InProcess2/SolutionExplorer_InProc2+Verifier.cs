// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    partial class SolutionExplorer_InProc2
    {
        public class Verifier
        {
            private readonly SolutionExplorer_InProc2 _solutionExplorer;

            public Verifier(SolutionExplorer_InProc2 solutionExplorer)
            {
                _solutionExplorer = solutionExplorer;
            }

            public void AssemblyReferencePresent(string projectName, string assemblyName, string assemblyVersion, string assemblyPublicKeyToken)
            {
                var assemblyReferences = _solutionExplorer.GetAssemblyReferences(projectName);
                var expectedAssemblyReference = assemblyName + "," + assemblyVersion + "," + assemblyPublicKeyToken.ToUpper();
                Assert.Contains(expectedAssemblyReference, assemblyReferences);
            }

            public void ProjectReferencePresent(string projectName, string referencedProjectName)
            {
                var projectReferences = _solutionExplorer.GetProjectReferences(projectName);
                Assert.Contains(referencedProjectName, projectReferences);
            }

            public void FileContents(string projectName, string fileName, string expectedContents)
            {
                var actualContents = _solutionExplorer.GetFileContents(projectName, fileName);
                Assert.Equal(expectedContents, actualContents);
            }
        }
    }
}
