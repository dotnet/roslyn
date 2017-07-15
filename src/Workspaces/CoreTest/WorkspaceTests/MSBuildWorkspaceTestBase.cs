// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;

using static Microsoft.CodeAnalysis.UnitTests.SolutionGeneration;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class MSBuildWorkspaceTestBase : WorkspaceTestBase
    {
        protected const string MSBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        protected void AssertCSCompilationOptions<T>(T expected, Func<Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions, T> actual)
        {
            var options = LoadCSharpCompilationOptions();
            Assert.Equal(expected, actual(options));
        }

        protected void AssertCSParseOptions<T>(T expected, Func<Microsoft.CodeAnalysis.CSharp.CSharpParseOptions, T> actual)
        {
            var options = LoadCSharpParseOptions();
            Assert.Equal(expected, actual(options));
        }

        protected void AssertVBCompilationOptions<T>(T expected, Func<Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions, T> actual)
        {
            var options = LoadVisualBasicCompilationOptions();
            Assert.Equal(expected, actual(options));
        }

        protected void AssertVBParseOptions<T>(T expected, Func<Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions, T> actual)
        {
            var options = LoadVisualBasicParseOptions();
            Assert.Equal(expected, actual(options));
        }

        protected Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions LoadCSharpCompilationOptions()
        {
            var sol = MSBuildWorkspace.Create().OpenSolutionAsync(GetSolutionFileName("TestSolution.sln")).Result;
            var project = sol.Projects.First();
            var options = (Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions)project.CompilationOptions;
            return options;
        }

        protected Microsoft.CodeAnalysis.CSharp.CSharpParseOptions LoadCSharpParseOptions()
        {
            var sol = MSBuildWorkspace.Create().OpenSolutionAsync(GetSolutionFileName("TestSolution.sln")).Result;
            var project = sol.Projects.First();
            var options = (Microsoft.CodeAnalysis.CSharp.CSharpParseOptions)project.ParseOptions;
            return options;
        }

        protected Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions LoadVisualBasicCompilationOptions()
        {
            var sol = MSBuildWorkspace.Create().OpenSolutionAsync(GetSolutionFileName("TestSolution.sln")).Result;
            var project = sol.GetProjectsByName("VisualBasicProject").FirstOrDefault();
            var options = (Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilationOptions)project.CompilationOptions;
            return options;
        }

        protected Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions LoadVisualBasicParseOptions()
        {
            var sol = MSBuildWorkspace.Create().OpenSolutionAsync(GetSolutionFileName("TestSolution.sln")).Result;
            var project = sol.GetProjectsByName("VisualBasicProject").FirstOrDefault();
            var options = (Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions)project.ParseOptions;
            return options;
        }

        protected void PrepareCrossLanguageProjectWithEmittedMetadata()
        {
            // Now try variant of CSharpProject that has an emitted assembly 
            CreateFiles(GetMultiProjectSolutionFiles()
                .WithFile(@"CSharpProject\CSharpProject.csproj", GetResourceText("CSharpProject_CSharpProject_ForEmittedOutput.csproj")));

            var sol = MSBuildWorkspace.Create().OpenSolutionAsync(GetSolutionFileName("TestSolution.sln")).Result;
            var p1 = sol.Projects.First(p => p.Language == LanguageNames.CSharp);
            var p2 = sol.Projects.First(p => p.Language == LanguageNames.VisualBasic);

            Assert.NotNull(p1.OutputFilePath);
            Assert.Equal("EmittedCSharpProject.dll", Path.GetFileName(p1.OutputFilePath));

            // if the assembly doesn't already exist, emit it now
            if (!File.Exists(p1.OutputFilePath))
            {
                var c1 = p1.GetCompilationAsync().Result;
                var result = c1.Emit(p1.OutputFilePath);
                Assert.Equal(true, result.Success);
            }
        }

        protected Solution Solution(params IBuilder[] inputs)
        {
            var files = GetSolutionFiles(inputs);
            CreateFiles(files);
            var solutionFileName = files.First(kvp => kvp.Key.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)).Key;
            solutionFileName = GetSolutionFileName(solutionFileName);
            var solution = MSBuildWorkspace.Create().OpenSolutionAsync(solutionFileName).Result;
            return solution;
        }
    }
}
