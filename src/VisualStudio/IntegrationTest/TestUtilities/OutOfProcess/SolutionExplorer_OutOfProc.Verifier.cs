// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class SolutionExplorer_OutOfProc : OutOfProcComponent
    {
        public class Verifier
        {
            private readonly SolutionExplorer_OutOfProc _solutionExplorer;

            public Verifier(SolutionExplorer_OutOfProc solutionExplorer)
            {
                _solutionExplorer = solutionExplorer;
            }

            public void AssemblyReferencePresent(ProjectUtils.Project project, string assemblyName, string assemblyVersion, string assemblyPublicKeyToken)
            {
                var assemblyReferences = _solutionExplorer.GetAssemblyReferences(project);
                var expectedAssemblyReference = assemblyName + "," + assemblyVersion + "," + assemblyPublicKeyToken.ToUpper();
                Assert.Contains(expectedAssemblyReference, assemblyReferences);
            }

            public void ProjectReferencePresent(ProjectUtils.Project project, string referencedProjectName)
            {
                var projectReferences = _solutionExplorer.GetProjectReferences(project);
                Assert.Contains(referencedProjectName, projectReferences);
            }

            public void FileContents(ProjectUtils.Project project, string fileName, string expectedContents)
            {
                var actualContents = _solutionExplorer.GetFileContents(project, fileName);
                Assert.Equal(expectedContents, actualContents);
            }
        }
    }
}
