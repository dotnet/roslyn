// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.LegacyProject;

[UseExportProvider]
public sealed class OutputPathTests
{
    [WpfTheory]
    [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
    [InlineData(@"Z:\ref\WithRefPath.dll")]
    [InlineData(null)]
    public void RefPathPassedToWorkspace(string expectedRefPath)
    {
        using var environment = new TestEnvironment();

        var hierarchyWithRefPath =
            environment.CreateHierarchy(
                "WithRefPath",
                @"Z:\WithRefPath.dll",
                expectedRefPath,
                "CSharp");

        var project = CSharpHelpers.CreateCSharpProject(environment, "WithRefPath", hierarchyWithRefPath);
        var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();

        Assert.Equal(expectedRefPath, workspaceProject.OutputRefFilePath);
    }

    [WpfFact]
    public void OutputPathPassedToCompilationOutputInfoWhenCompilerOutputPathIsUnavailable()
    {
        using var environment = new TestEnvironment();

        const string projectBinPath = @"Z:\bin";
        var hierarchy = environment.CreateHierarchy("WithRefPath", projectBinPath, projectRefPath: null, "CSharp");

        _ = CSharpHelpers.CreateCSharpProject(environment, "WithRefPath", hierarchy);

        var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
        Assert.NotNull(workspaceProject.OutputFilePath);
        Assert.Equal(workspaceProject.OutputFilePath, workspaceProject.CompilationOutputInfo.AssemblyPath);
        Assert.True(workspaceProject.CompilationOutputInfo.HasEffectiveGeneratedFilesOutputDirectory);
    }

    [WpfFact]
    public void CompilationOutputInfoFollowsOutputPathWhenCompilerOutputPathIsUnavailable()
    {
        using var environment = new TestEnvironment();

        var hierarchy = environment.CreateHierarchy("Test", @"Z:\bin\Debug", projectRefPath: null, "CSharp");
        var project = CSharpHelpers.CreateCSharpProject(environment, "Test", hierarchy);

        string getCompilationOutputAssemblyPath()
            => environment.Workspace.CurrentSolution.Projects.Single().CompilationOutputInfo.AssemblyPath;

        Assert.Equal(@"Z:\bin\Debug\Test.dll", getCompilationOutputAssemblyPath());

        ((IVsBuildPropertyStorage)hierarchy).SetPropertyValue("OutDir", null, (uint)_PersistStorageType.PST_PROJECT_FILE, @"Z:\bin\Release");
        project.SetOutputFileName("Test.dll");

        Assert.Equal(@"Z:\bin\Release\Test.dll", getCompilationOutputAssemblyPath());

        // An explicitly provided compiler output path is not replaced by the bin output path.
        project.SetOutputFileName(@"Z:\obj\Test.dll");
        ((IVsBuildPropertyStorage)hierarchy).SetPropertyValue("OutDir", null, (uint)_PersistStorageType.PST_PROJECT_FILE, @"Z:\bin\Debug");
        project.SetOutputFileName("Test.dll");

        Assert.Equal(@"Z:\obj\Test.dll", getCompilationOutputAssemblyPath());
    }
}
