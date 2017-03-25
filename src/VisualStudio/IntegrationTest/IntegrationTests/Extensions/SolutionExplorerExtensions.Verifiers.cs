// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.VisualStudio.IntegrationTests.Extensions.ErrorList;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer
{
    public static partial class SolutionExplorerExtensions
    {
        public static void VerifyAssemblyReferencePresent(this AbstractIntegrationTest test, ProjectUtils.Project project, string assemblyName, string assemblyVersion, string assemblyPublicKeyToken)
        {
            var assemblyReferences = test.VisualStudio.Instance.SolutionExplorer.GetAssemblyReferences(project.Name);
            var expectedAssemblyReference = assemblyName + "," + assemblyVersion + "," + assemblyPublicKeyToken.ToUpper();
            Assert.Contains(expectedAssemblyReference, assemblyReferences);
        }

        public static void VerifyProjectReferencePresent(this AbstractIntegrationTest test, ProjectUtils.Project project, string referencedProjectName)
        {
            var projectReferences = test.VisualStudio.Instance.SolutionExplorer.GetProjectReferences(project.Name);
            Assert.Contains(referencedProjectName, projectReferences);
        }

        public static void VerifyFileContents(this AbstractIntegrationTest test, ProjectUtils.Project project, string fileName, string expectedContents)
        {
            var actualContents = test.VisualStudio.Instance.SolutionExplorer.GetFileContents(project.Name, fileName);
            Assert.Equal(expectedContents, actualContents);
        }
    }
}
