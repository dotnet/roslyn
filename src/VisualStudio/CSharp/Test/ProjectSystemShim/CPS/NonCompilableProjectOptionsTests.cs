// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    using static CSharpHelpers;

    [UseExportProvider]
    public class NonCompilableProjectOptionsTests : TestBase
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void SetOptions_Success()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateNonCompilableProject(environment, "project1", @"C:\project1.fsproj"))
            {
                string GetCommandLineOptions() => environment.Workspace.CurrentSolution.Projects.Single().CommandLineOptions;

                Assert.Equal(null, GetCommandLineOptions());

                project.SetOptions("--test");

                Assert.Equal("--test", GetCommandLineOptions());

                Assert.Throws<ArgumentNullException>(() => project.SetOptions(null));

                Assert.Equal("--test", GetCommandLineOptions());
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void SetOptionsBatch_Success()
        {
            using (var environment = new TestEnvironment())
            using (var project = CreateNonCompilableProject(environment, "project1", @"C:\project1.fsproj"))
            {
                string GetCommandLineOptions() => environment.Workspace.CurrentSolution.Projects.Single().CommandLineOptions;

                project.StartBatch();

                Assert.Equal(null, GetCommandLineOptions());

                project.SetOptions("--test");

                Assert.Equal(null, GetCommandLineOptions());

                Assert.Throws<ArgumentNullException>(() => project.SetOptions(null));

                project.EndBatch();

                Assert.Equal("--test", GetCommandLineOptions());
            }
        }
    }
}
