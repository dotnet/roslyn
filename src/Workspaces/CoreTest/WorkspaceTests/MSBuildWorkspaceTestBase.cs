// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.MSBuild;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

using static Microsoft.CodeAnalysis.UnitTests.SolutionGeneration;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class MSBuildWorkspaceTestBase : WorkspaceTestBase
    {
        protected const string MSBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        protected async Task AssertCSCompilationOptionsAsync<T>(T expected, Func<CS.CSharpCompilationOptions, T> actual)
        {
            var options = await LoadCSharpCompilationOptionsAsync();
            Assert.Equal(expected, actual(options));
        }

        protected async Task AssertCSParseOptionsAsync<T>(T expected, Func<CS.CSharpParseOptions, T> actual)
        {
            var options = await LoadCSharpParseOptionsAsync();
            Assert.Equal(expected, actual(options));
        }

        protected async Task AssertVBCompilationOptionsAsync<T>(T expected, Func<VB.VisualBasicCompilationOptions, T> actual)
        {
            var options = await LoadVisualBasicCompilationOptionsAsync();
            Assert.Equal(expected, actual(options));
        }

        protected async Task AssertVBParseOptionsAsync<T>(T expected, Func<VB.VisualBasicParseOptions, T> actual)
        {
            var options = await LoadVisualBasicParseOptionsAsync();
            Assert.Equal(expected, actual(options));
        }

        protected async Task<CS.CSharpCompilationOptions> LoadCSharpCompilationOptionsAsync()
        {
            var solutionFilePath = GetSolutionFileName("TestSolution.sln");
            using (var workspace = CreateMSBuildWorkspace())
            {
                var sol = await workspace.OpenSolutionAsync(solutionFilePath);
                var project = sol.Projects.First();
                return (CS.CSharpCompilationOptions)project.CompilationOptions;
            }
        }

        protected async Task<CS.CSharpParseOptions> LoadCSharpParseOptionsAsync()
        {
            var solutionFilePath = GetSolutionFileName("TestSolution.sln");
            using (var workspace = CreateMSBuildWorkspace())
            {
                var sol = await workspace.OpenSolutionAsync(solutionFilePath);
                var project = sol.Projects.First();
                return (CS.CSharpParseOptions)project.ParseOptions;
            }
        }

        protected async Task<VB.VisualBasicCompilationOptions> LoadVisualBasicCompilationOptionsAsync()
        {
            var solutionFilePath = GetSolutionFileName("TestSolution.sln");
            using (var workspace = CreateMSBuildWorkspace())
            {
                var sol = await workspace.OpenSolutionAsync(solutionFilePath);
                var project = sol.GetProjectsByName("VisualBasicProject").FirstOrDefault();
                return (VB.VisualBasicCompilationOptions)project.CompilationOptions;
            }
        }

        protected async Task<VB.VisualBasicParseOptions> LoadVisualBasicParseOptionsAsync()
        {
            var solutionFilePath = GetSolutionFileName("TestSolution.sln");
            using (var workspace = CreateMSBuildWorkspace())
            {
                var sol = await workspace.OpenSolutionAsync(solutionFilePath);
                var project = sol.GetProjectsByName("VisualBasicProject").FirstOrDefault();
                return (VB.VisualBasicParseOptions)project.ParseOptions;
            }
        }

        protected async Task PrepareCrossLanguageProjectWithEmittedMetadataAsync()
        {
            // Now try variant of CSharpProject that has an emitted assembly 
            CreateFiles(GetMultiProjectSolutionFiles()
                .WithFile(@"CSharpProject\CSharpProject.csproj", GetResourceText("CSharpProject_CSharpProject_ForEmittedOutput.csproj")));

            var solutionFilePath = GetSolutionFileName("TestSolution.sln");
            using (var workspace = CreateMSBuildWorkspace())
            {
                var sol = await workspace.OpenSolutionAsync(solutionFilePath);
                var p1 = sol.Projects.First(p => p.Language == LanguageNames.CSharp);
                var p2 = sol.Projects.First(p => p.Language == LanguageNames.VisualBasic);

                Assert.NotNull(p1.OutputFilePath);
                Assert.Equal("EmittedCSharpProject.dll", Path.GetFileName(p1.OutputFilePath));

                // if the assembly doesn't already exist, emit it now
                if (!File.Exists(p1.OutputFilePath))
                {
                    var c1 = await p1.GetCompilationAsync();
                    var result = c1.Emit(p1.OutputFilePath);
                    Assert.Equal(true, result.Success);
                }
            }
        }

        protected async Task<Solution> SolutionAsync(params IBuilder[] inputs)
        {
            var files = GetSolutionFiles(inputs);
            CreateFiles(files);
            var solutionFileName = files.First(kvp => kvp.Key.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)).Key;
            solutionFileName = GetSolutionFileName(solutionFileName);
            using (var workspace = CreateMSBuildWorkspace())
            {
                return await workspace.OpenSolutionAsync(solutionFileName);
            }
        }

        protected static MSBuildWorkspace CreateMSBuildWorkspace(params (string key, string value)[] additionalProperties)
        {
            return MSBuildWorkspace.Create(CreateProperties(additionalProperties));
        }

        protected static MSBuildWorkspace CreateMSBuildWorkspace(HostServices hostServices, params (string key, string value)[] additionalProperties)
        {

            return MSBuildWorkspace.Create(CreateProperties(additionalProperties), hostServices);
        }

        private static Dictionary<string, string> CreateProperties((string key, string value)[] additionalProperties)
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // We need to specifically set MSBuildExtensionsPath to the directory that the tests are running in because
            // the MSBuild.exe.config file in that directory (the one included with the Microsoft.Build.Runtime package)
            // does not set it appropriately.
            var properties = new Dictionary<string, string>()
            {
                { "MSBuildExtensionsPath", currentDirectory }
            };

            foreach (var (k, v) in additionalProperties)
            {
                properties.Add(k, v);
            }

            return properties;
        }
    }
}
