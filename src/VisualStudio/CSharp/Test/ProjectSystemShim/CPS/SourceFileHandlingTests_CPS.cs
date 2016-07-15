// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
    using static CSharpHelpers;

    public partial class SourceFileHandlingTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddRemoveSourceFile_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var cpsProject = CreateCSharpCPSProject(environment, "project1");
                var project = (AbstractProject)cpsProject;
                Assert.Empty(project.GetCurrentDocuments());

                // Add source file
                var sourceFileFullPath = @"c:\source.cs";
                cpsProject.AddSourceFile(sourceFileFullPath);
                Assert.True(project.GetCurrentDocuments().Any(s => s.FilePath == sourceFileFullPath));

                // Remove source file
                cpsProject.RemoveSourceFile(sourceFileFullPath);
                Assert.Empty(project.GetCurrentDocuments());

                project.Disconnect();
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddRemoveAdditionalFile_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var project = (AbstractProject)CreateCSharpCPSProject(environment, "project1");
                var analyzerHost = (IAnalyzerHost)project;
                Assert.Empty(project.GetCurrentAdditionalDocuments());

                // Add additional file
                var additionalFileFullPath = @"c:\source.cs";
                analyzerHost.AddAdditionalFile(additionalFileFullPath);
                Assert.True(project.GetCurrentAdditionalDocuments().Any(s => s.FilePath == additionalFileFullPath));

                // Remove additional file
                analyzerHost.RemoveAdditionalFile(additionalFileFullPath);
                Assert.Empty(project.GetCurrentAdditionalDocuments());

                project.Disconnect();
            }
        }
    }
}
