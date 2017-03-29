// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpBuild : AbstractIntegrationTest
    {
        public CSharpBuild(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, _ => null)
        {
            VisualStudio.Instance.SolutionExplorer.CreateSolution(nameof(CSharpBuild));
            VisualStudio.Instance.SolutionExplorer.AddProject("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Build)]
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

            VisualStudio.Instance.Editor.SetText(editorText);

            // TODO: Validate build works as expected
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Build)]
        public void BuildWithCommandLine()
        {
            VisualStudio.Instance.SolutionExplorer.SaveAll();

            var pathToDevenv = Path.Combine(VisualStudio.Instance.InstallationPath, @"Common7\IDE\devenv.exe");
            var pathToSolution = VisualStudio.Instance.SolutionExplorer.SolutionFileFullPath;
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
