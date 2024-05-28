// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Extensibility.Testing;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

[TestService]
internal partial class SolutionVerifierInProcess
{
    public async Task AssemblyReferencePresentAsync(string projectName, string assemblyName, string assemblyVersion, string assemblyPublicKeyToken, CancellationToken cancellationToken)
    {
        var assemblyReferences = await TestServices.SolutionExplorer.GetAssemblyReferencesAsync(projectName, cancellationToken);
        var expectedAssemblyReference = (assemblyName, assemblyVersion, assemblyPublicKeyToken: assemblyPublicKeyToken.ToUpper());
        if (assemblyReferences.Contains(expectedAssemblyReference))
            return;

        var assemblyReferencesMatchingName = assemblyReferences.WhereAsArray(reference => string.Equals(reference.name, expectedAssemblyReference.assemblyName, StringComparison.OrdinalIgnoreCase));
        if (!assemblyReferencesMatchingName.IsEmpty)
            assemblyReferences = assemblyReferencesMatchingName;

        // 17.9.0 Preview 2.0 through 17.9.0 Preview 2.1
        if (await TestServices.Shell.GetVersionAsync(cancellationToken) >= Version.Parse("17.9.34407.89")
            && await TestServices.Shell.GetVersionAsync(cancellationToken) <= Version.Parse("17.9.34414.90")
            && !assemblyReferencesMatchingName.IsEmpty)
        {
            // The actual assembly version has a number like:
            //   0.0.527041792.678
            //   0.0.528033936.678
            //   0.0.292991664.480
            //   0.0.205444464.480
            //
            // The actual public key token is empty in these test cases.
            //
            // Since we can't predict the exact outcome, only validate the assembly name (indirectly by knowing
            // assemblyReferencesMatchingName is not empty).
            return;
        }

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
