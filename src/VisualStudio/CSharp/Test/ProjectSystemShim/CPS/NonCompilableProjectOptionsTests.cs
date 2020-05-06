// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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
            using var environment = new TestEnvironment();
            using var project = CreateNonCompilableProject(environment, "project1", @"C:\project1.fsproj");

            string GetCommandLineOptions() => environment.Workspace.CurrentSolution.Projects.Single().CommandLineOptions;

            Assert.Null(GetCommandLineOptions());

            project.SetOptions("--test");

            Assert.Equal("--test", GetCommandLineOptions());

            Assert.Throws<ArgumentException>(() => project.SetOptions(null));

            Assert.Equal("--test", GetCommandLineOptions());

        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void SetOptionsBatch_Success()
        {
            using var environment = new TestEnvironment();
            using var project = CreateNonCompilableProject(environment, "project1", @"C:\project1.fsproj");
            string GetCommandLineOptions() => environment.Workspace.CurrentSolution.Projects.Single().CommandLineOptions;

            project.StartBatch();

            Assert.Null(GetCommandLineOptions());

            project.SetOptions("--test");

            Assert.Null(GetCommandLineOptions());

            Assert.Throws<ArgumentException>(() => project.SetOptions(null));

            project.EndBatch();

            Assert.Equal("--test", GetCommandLineOptions());
        }
    }
}
