// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.UnitTests.TestFiles;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.MSBuild.UnitTests.SolutionGeneration;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    public class MSBuildWorkspaceTestBase : WorkspaceTestBase
    {
        protected const string MSBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        protected static void AssertFailures(MSBuildWorkspace workspace, params string[] expectedFailures)
        {
            AssertEx.Equal(expectedFailures, workspace.Diagnostics.Where(d => d.Kind == WorkspaceDiagnosticKind.Failure).Select(d => d.Message));
        }

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

        protected static int GetMethodInsertionPoint(VB.Syntax.ClassBlockSyntax classBlock)
        {
            if (classBlock.Implements.Count > 0)
            {
                return classBlock.Implements[classBlock.Implements.Count - 1].FullSpan.End;
            }
            else if (classBlock.Inherits.Count > 0)
            {
                return classBlock.Inherits[classBlock.Inherits.Count - 1].FullSpan.End;
            }
            else
            {
                return classBlock.BlockStatement.FullSpan.End;
            }
        }

        protected async Task PrepareCrossLanguageProjectWithEmittedMetadataAsync()
        {
            // Now try variant of CSharpProject that has an emitted assembly 
            CreateFiles(GetMultiProjectSolutionFiles()
                .WithFile(@"CSharpProject\CSharpProject.csproj", Resources.ProjectFiles.CSharp.ForEmittedOutput));

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
                    Assert.True(result.Success);
                }
            }
        }

        protected async Task<Solution> SolutionAsync(params IBuilder[] inputs)
        {
            var files = GetSolutionFiles(inputs);
            CreateFiles(files);
            var solutionFileName = files.First(t => t.fileName.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)).fileName;
            solutionFileName = GetSolutionFileName(solutionFileName);
            using (var workspace = CreateMSBuildWorkspace())
            {
                return await workspace.OpenSolutionAsync(solutionFileName);
            }
        }

        protected static MSBuildWorkspace CreateMSBuildWorkspace(params (string key, string value)[] additionalProperties)
            => CreateMSBuildWorkspace(throwOnWorkspaceFailed: true, skipUnrecognizedProjects: false, additionalProperties: additionalProperties);

        protected static MSBuildWorkspace CreateMSBuildWorkspace(
            bool throwOnWorkspaceFailed = true,
            bool skipUnrecognizedProjects = false,
            (string key, string value)[] additionalProperties = null)
        {
            additionalProperties ??= Array.Empty<(string key, string value)>();
            var workspace = MSBuildWorkspace.Create(CreateProperties(additionalProperties));
            if (throwOnWorkspaceFailed)
            {
                workspace.WorkspaceFailed += (s, e) => throw new Exception($"Workspace failure {e.Diagnostic.Kind}:{e.Diagnostic.Message}");
            }

            if (skipUnrecognizedProjects)
            {
                workspace.SkipUnrecognizedProjects = true;
            }

            return workspace;
        }

        protected static MSBuildWorkspace CreateMSBuildWorkspace(HostServices hostServices, params (string key, string value)[] additionalProperties)
        {

            return MSBuildWorkspace.Create(CreateProperties(additionalProperties), hostServices);
        }

        private static Dictionary<string, string> CreateProperties((string key, string value)[] additionalProperties)
        {
            var properties = new Dictionary<string, string>();

            foreach (var (k, v) in additionalProperties)
            {
                properties.Add(k, v);
            }

            return properties;
        }

        protected static async Task AssertThrowsExceptionForInvalidPath(Func<Task> testCode)
        {
#if NET

            // On .NET Core, invalid file paths don't throw exceptions when calling Path manipulation APIs, they just throw FileNotFound once you
            // actually try to use the path
            await Assert.ThrowsAsync<FileNotFoundException>(testCode);

#else

            // On .NET Framework, invalid file paths throw exceptions that we caught as IOExceptions we re-raise an InvalidOperationException.
            // We'll assert the paths we test with contain "Invalid" to have some confidence this isn't an unrelated exception being thrown.
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(testCode);
            Assert.Contains("Invalid", exception.Message);

#endif
        }
    }
}
