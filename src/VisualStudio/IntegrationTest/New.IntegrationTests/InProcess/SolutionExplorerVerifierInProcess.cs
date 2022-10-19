// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility.Testing;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class SolutionExplorerVerifierInProcess
    {
        public async Task AssemblyReferencePresentAsync(string projectName, string assemblyName, string assemblyVersion, string assemblyPublicKeyToken, CancellationToken cancellationToken)
        {
            var assemblyReferences = await TestServices.SolutionExplorer.GetAssemblyReferencesAsync(projectName, cancellationToken);
            var expectedAssemblyReference = assemblyName + "," + assemblyVersion + "," + assemblyPublicKeyToken.ToUpper();
            Assert.Contains(expectedAssemblyReference, assemblyReferences);
        }

        public async Task ProjectReferencePresentAsync(string projectName, string referencedProjectName, CancellationToken cancellationToken)
        {
            var projectReferences = await TestServices.SolutionExplorer.GetProjectReferencesAsync(projectName, cancellationToken);
            Assert.Contains(referencedProjectName, projectReferences);
        }
    }
}
