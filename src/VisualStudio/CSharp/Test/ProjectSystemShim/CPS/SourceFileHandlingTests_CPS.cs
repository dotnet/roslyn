// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    using static CSharpHelpers;

    public partial class SourceFileHandlingTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddRemoveSourceFile_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var project1Shim = CreateCSharpCPSProject(environment, "project1");
                var project1 = (AbstractProject)project1Shim;
                Assert.Empty(project1.GetCurrentDocuments());

                // Add source file
                var sourceFileFullPath = @"c:\source.cs";
                project1Shim.AddSourceFile(sourceFileFullPath);
                Assert.True(project1.GetCurrentDocuments().Any(s => s.FilePath == sourceFileFullPath));

                // Remove source file
                project1Shim.RemoveSourceFile(sourceFileFullPath);
                Assert.Empty(project1.GetCurrentDocuments());

                project1.Disconnect();
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddRemoveAdditionalFile_CPS()
        {
            using (var environment = new TestEnvironment())
            {
                var project1Shim = CreateCSharpCPSProject(environment, "project1");
                var project1 = (AbstractProject)project1Shim;
                Assert.Empty(project1.GetCurrentAdditionalDocuments());

                // Add additional file
                var additionalFileFullPath = @"c:\source.cs";
                project1Shim.AddAdditionalFile(additionalFileFullPath);
                Assert.True(project1.GetCurrentAdditionalDocuments().Any(s => s.FilePath == additionalFileFullPath));

                // Remove additional file
                project1Shim.RemoveAdditionalFile(additionalFileFullPath);
                Assert.Empty(project1.GetCurrentAdditionalDocuments());

                project1.Disconnect();
            }
        }
    }
}
