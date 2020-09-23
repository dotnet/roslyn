// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public CSharpBuild(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
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
            Assert.True(process.WaitForExit((int)Helper.HangMitigatingTimeout.TotalMilliseconds));

            Assert.Contains("Rebuild All: 1 succeeded, 0 failed, 0 skipped", File.ReadAllText(logFileName));

            Assert.Equal(0, process.ExitCode);
        }
    }
}
