// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Extensibility.Testing;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class SolutionVerifierInProcess
    {
        public async Task AssemblyReferencePresentAsync(string projectName, string assemblyName, string assemblyVersion, string assemblyPublicKeyToken, CancellationToken cancellationToken)
        {
            var assemblyReferences = await TestServices.SolutionExplorer.GetAssemblyReferencesAsync(projectName, cancellationToken);
            var expectedAssemblyReference = (assemblyName, assemblyVersion, assemblyPublicKeyToken.ToUpper());
            if (assemblyReferences.Contains(expectedAssemblyReference))
                return;

            var assemblyReferencesMatchingName = assemblyReferences.WhereAsArray(reference => string.Equals(reference.name, expectedAssemblyReference.assemblyName, StringComparison.OrdinalIgnoreCase));
            if (!assemblyReferencesMatchingName.IsEmpty)
                assemblyReferences = assemblyReferencesMatchingName;

            Assert.Contains(expectedAssemblyReference, assemblyReferences);
        }

        public async Task ProjectReferencePresentAsync(string projectName, string referencedProjectName, CancellationToken cancellationToken)
        {
            var projectReferences = await TestServices.SolutionExplorer.GetProjectReferencesAsync(projectName, cancellationToken);
            Assert.Contains(referencedProjectName, projectReferences);
        }

        public async Task FileContentsAsync(string projectName, string fileName, string expectedContents, CancellationToken cancellationToken)
        {
            var actualContents = await TestServices.SolutionExplorer.GetFileContentsAsync(projectName, fileName, cancellationToken);
            Assert.Equal(expectedContents, actualContents);
        }
    }
}
