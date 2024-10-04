// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.LegacyProject
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
    public class SourceFileHandlingTests
    {
        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100114")]
        public void IgnoreAdditionsOfXomlFiles()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            project.OnSourceFileAdded("Goo.xoml");

            // Even though we added a source file, since it has a .xoml extension we'll ignore it
            Assert.Empty(environment.Workspace.CurrentSolution.Projects.Single().Documents);

            // Try removing it to make sure it doesn't throw
            project.OnSourceFileRemoved("Goo.xoml");
        }

        [WpfFact]
        public void AddFileExWithLinkPathUsesThatAsAFolder()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            project.AddFileEx(@"C:\Cat.cs", linkMetadata: @"LinkFolder\Cat.cs");

            var document = environment.Workspace.CurrentSolution.Projects.Single().Documents.Single();

            Assert.Equal(["LinkFolder"], document.Folders);
        }

        [WpfFact]
        public void AddFileExWithLinkPathWithoutFolderWorksCorrectly()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            project.AddFileEx(@"C:\Cat.cs", linkMetadata: @"Dog.cs");

            var document = environment.Workspace.CurrentSolution.Projects.Single().Documents.Single();

            Assert.Empty(document.Folders);
        }

        [WpfFact]
        public void AddFileExWithNoLinkPathComputesEmptyFolder()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");
            var projectFolder = Path.GetDirectoryName(environment.Workspace.CurrentSolution.Projects.Single().FilePath);

            project.AddFileEx(Path.Combine(projectFolder, "Cat.cs"), null);

            var document = environment.Workspace.CurrentSolution.Projects.Single().Documents.Single();

            Assert.Empty(document.Folders);
        }

        [WpfFact]
        public void AddFileExWithNoLinkPathComputesRelativeFolderPath()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");
            var projectFolder = Path.GetDirectoryName(environment.Workspace.CurrentSolution.Projects.Single().FilePath);

            project.AddFileEx(Path.Combine(projectFolder, "RelativeFolder", "Cat.cs"), null);

            var document = environment.Workspace.CurrentSolution.Projects.Single().Documents.Single();

            Assert.Equal(["RelativeFolder"], document.Folders);
        }
    }
}
