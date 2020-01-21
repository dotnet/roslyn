// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpBuild : AbstractIntegrationTest
    {
        public CSharpBuild(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudio.SolutionExplorer.CreateSolution(nameof(CSharpBuild));
            VisualStudio.SolutionExplorer.AddProject(new ProjectUtils.Project("TestProj"), WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Build)]
        public void BuildProject()
        {
            var editorText = @"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(""Hello, World!"");
    }
}";

            VisualStudio.Editor.SetText(editorText);

            // TODO: Validate build works as expected
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Build)]
        public void BuildWithCommandLine()
        {
            VisualStudio.SolutionExplorer.SaveAll();

            var pathToDevenv = Path.Combine(VisualStudio.InstallationPath, @"Common7\IDE\devenv.exe");
            var pathToSolution = VisualStudio.SolutionExplorer.SolutionFileFullPath;
            var logFileName = pathToSolution + ".log";

            File.Delete(logFileName);

            var commandLine = $"\"{pathToSolution}\" /Rebuild Debug /Out \"{logFileName}\" {VisualStudioInstanceFactory.VsLaunchArgs}";

            var process = Process.Start(pathToDevenv, commandLine);
            process.WaitForExit();

            Assert.Contains("Rebuild All: 1 succeeded, 0 failed, 0 skipped", File.ReadAllText(logFileName));

            Assert.Equal(0, process.ExitCode);
        }
    }
}
