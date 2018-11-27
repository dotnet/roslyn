// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    using System.Collections.Generic;
    using static CSharpHelpers;

    [UseExportProvider]
    public class SourceFileHandlingTests
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddRemoveSourceFile_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<Document> GetCurrentDocuments() => environment.Workspace.CurrentSolution.Projects.Single().Documents;

                Assert.Empty(GetCurrentDocuments());

                // Add source file
                var sourceFileFullPath = @"c:\source.cs";
                project.AddSourceFile(sourceFileFullPath);
                Assert.True(GetCurrentDocuments().Any(s => s.FilePath == sourceFileFullPath));

                // Remove source file
                project.RemoveSourceFile(sourceFileFullPath);
                Assert.Empty(GetCurrentDocuments());
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddRemoveAdditionalFile_CPS()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateCSharpCPSProject(environment, "project1"))
            {
                IEnumerable<TextDocument> GetCurrentAdditionalDocuments() => environment.Workspace.CurrentSolution.Projects.Single().AdditionalDocuments;
                Assert.Empty(GetCurrentAdditionalDocuments());

                // Add additional file
                var additionalFileFullPath = @"c:\source.cs";
                project.AddAdditionalFile(additionalFileFullPath);
                Assert.True(GetCurrentAdditionalDocuments().Any(s => s.FilePath == additionalFileFullPath));

                // Remove additional file
                project.RemoveAdditionalFile(additionalFileFullPath);
                Assert.Empty(GetCurrentAdditionalDocuments());
            }
        }
    }
}
