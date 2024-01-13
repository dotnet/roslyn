// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.LegacyProject
{
    [UseExportProvider]
    public class OutputPathTests
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
    }
}
