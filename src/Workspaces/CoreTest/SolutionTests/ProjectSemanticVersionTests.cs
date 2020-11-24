// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.UnitTests.SolutionTestHelpers;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class ProjectSemanticVersionTests
    {
        [Fact]
        public async Task AddingDocumentWithNewClassChangesVersion()
        {
            using var workspace = CreateWorkspace();
            var project = AddEmptyProject(workspace.CurrentSolution);

            await AssertSemanticVersionChangedAsync(project, project.AddDocument("Hello.cs", "class C { }").Project);
        }

        [Fact]
        public async Task RemovingDocumentWithNewClassChangesVersion()
        {
            using var workspace = CreateWorkspace();
            var project = AddEmptyProject(workspace.CurrentSolution)
                .AddDocument("Hello.cs", "class C { }").Project;

            await AssertSemanticVersionChangedAsync(project, project.RemoveDocument(project.DocumentIds.Single()));
        }

        private static async Task AssertSemanticVersionChangedAsync(Project project1, Project project2)
        {
            Assert.NotEqual(await project1.GetSemanticVersionAsync(), await project2.GetSemanticVersionAsync());
        }
    }
}
