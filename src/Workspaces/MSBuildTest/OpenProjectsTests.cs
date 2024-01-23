// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    public class OpenProjectsTests : MSBuildWorkspaceTestBase
    {
        // The Maui templates require additional dotnet workloads to be installed.
        // Running `dotnet workload restore` will install workloads but may require
        // admin permissions. In addition a restart may be required after workload 
        // installation. 
        private const bool ExcludeMauiTemplates = true;

        protected ITestOutputHelper TestOutputHelper { get; set; }

        public OpenProjectsTests(ITestOutputHelper output)
        {
            TestOutputHelper = output;
        }

        [ConditionalTheory(typeof(DotNetSdkMSBuildInstalled))]
        [MemberData(nameof(GetCSharpProjectTemplateNames), DisableDiscoveryEnumeration = false)]
        public async Task ValidateCSharpTemplateProjects(string templateName)
        {
            var ignoredDiagnostics = templateName switch
            {
                "blazor" or "blazorwasm" or "blazorwasm-empty" =>
                    [
                        "CS0246", // The type or namespace name {'csharp_blazor_project'|'App'} could not be found (are you missing a using directive or an assembly reference?)
                    ],
                "wpf" =>
                    [
                        "CS5001", // Program does not contain a static 'Main' method suitable for an entry point
                        "CS0103", // The name 'InitializeComponent' does not exist in the current context"
                    ],
                "wpfusercontrollib" =>
                    [
                        "CS0103", // The name 'InitializeComponent' does not exist in the current context"
                    ],
                _ => Array.Empty<string>(),
            };

            await AssertTemplateProjectLoadsCleanlyAsync(templateName, LanguageNames.CSharp, ignoredDiagnostics);
        }

        [ConditionalTheory(typeof(DotNetSdkMSBuildInstalled))]
        [MemberData(nameof(GetVisualBasicProjectTemplateNames), DisableDiscoveryEnumeration = false)]
        public async Task ValidateVisualBasicTemplateProjects(string templateName)
        {
            var ignoredDiagnostics = templateName switch
            {
                "wpf" =>
                    [
                        "BC30420", // 'Sub Main' was not found in 'visual_basic_wpf_project'.
                    ],
                _ => Array.Empty<string>(),
            };

            await AssertTemplateProjectLoadsCleanlyAsync(templateName, LanguageNames.VisualBasic, ignoredDiagnostics);
        }

        public static TheoryData<string> GetCSharpProjectTemplateNames()
            => GetProjectTemplateNames("c#");

        public static TheoryData<string> GetVisualBasicProjectTemplateNames()
            => GetProjectTemplateNames("vb");

        public static TheoryData<string> GetProjectTemplateNames(string language)
        {
            // The expected output from the list command is as follows.

            // These templates matched your input: --language='vb', --type='project'
            //
            // Template Name                  Short Name           Language  Tags
            // -----------------------------  -------------------  --------  ---------------
            // Class Library                  classlib             C#,F#,VB  Common/Library
            // Console App                    console              C#,F#,VB  Common/Console
            // ...

            var result = RunDotNet($"new list --type project --language {language}");
            Assert.Equal(0, result.ExitCode);

            var lines = result.Output
                .Replace("\r\n", "\n")
                .Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            TheoryData<string> templateNames = [];
            var foundDivider = false;

            foreach (var line in lines)
            {
                if (!foundDivider)
                {
                    if (line.StartsWith("----"))
                    {
                        foundDivider = true;
                    }
                    continue;
                }

                var columns = line.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .ToArray();
                var templateShortName = columns[1].Split(',').First();

                if (ExcludeMauiTemplates && templateShortName.StartsWith("maui"))
                    continue;

                templateNames.Add(templateShortName);
            }

            return templateNames;
        }

        private async Task AssertTemplateProjectLoadsCleanlyAsync(string templateName, string languageName, string[] ignoredDiagnostics)
        {
            if (ignoredDiagnostics.Length > 0)
            {
                TestOutputHelper.WriteLine($"Ignoring compiler diagnostics: \"{string.Join("\", \"", ignoredDiagnostics)}\"");
            }

            // Clean up previous run
            CleanupProject(templateName, languageName);

            var projectFilePath = GenerateProjectFromTemplate(templateName, languageName, TestOutputHelper);

            await AssertProjectLoadsCleanlyAsync(projectFilePath, ignoredDiagnostics);

            // Clean up successful run
            CleanupProject(templateName, languageName);

            return;

            void CleanupProject(string templateName, string languageName)
            {
                var projectPath = GetProjectPath(templateName, languageName);

                if (Directory.Exists(projectPath))
                {
                    Directory.Delete(projectPath, recursive: true);
                }
            }

            string GetProjectPath(string templateName, string languageName)
            {
                var languagePrefix = languageName.Replace("#", "Sharp").Replace(' ', '_').ToLower();
                var projectName = $"{languagePrefix}_{templateName}_project";
                return Path.Combine(SolutionDirectory.Path, projectName);
            }

            string GenerateProjectFromTemplate(string templateName, string languageName, ITestOutputHelper outputHelper)
            {
                var projectPath = GetProjectPath(templateName, languageName);
                var projectFilePath = GetProjectFilePath(projectPath, languageName);

                CreateNewProject(templateName, projectPath, languageName, outputHelper);

                return projectFilePath;
            }

            static string GetProjectFilePath(string projectPath, string languageName)
            {
                var projectName = Path.GetFileName(projectPath);
                var projectExtension = languageName switch
                {
                    LanguageNames.CSharp => "csproj",
                    LanguageNames.VisualBasic => "vbproj",
                    _ => throw new ArgumentOutOfRangeException(nameof(languageName), actualValue: languageName, message: "Only C# and VB.Net project are supported.")
                };
                return Path.Combine(projectPath, $"{projectName}.{projectExtension}");
            }

            static int CreateNewProject(string templateName, string outputPath, string languageName, ITestOutputHelper output)
            {
                var language = languageName switch
                {
                    LanguageNames.CSharp => "C#",
                    LanguageNames.VisualBasic => "VB",
                    _ => throw new ArgumentOutOfRangeException(nameof(languageName), actualValue: languageName, message: "Only C#, F# and VB.NET project are supported.")
                };

                var newResult = RunDotNet($"new \"{templateName}\" -o \"{outputPath}\" --language \"{language}\"");
                Assert.Equal(0, newResult.ExitCode);

                output.WriteLine(string.Join(Environment.NewLine, newResult.Output));

                // Most templates invoke restore as a post-creation action. However, some, like the
                // Maui templates, do not run restore since they require additional workloads to be
                // installed.
                if (newResult.Output.Contains("Restoring"))
                {
                    return newResult.ExitCode;
                }

                // Attempt a restore and see if we are instructed to install additional workloads.
                var restoreResult = RunDotNet($"restore", workingDirectory: outputPath);

                output.WriteLine(string.Join(Environment.NewLine, restoreResult.Output));

                if (restoreResult.ExitCode == 0)
                    return restoreResult.ExitCode;

                if (restoreResult.Output.Contains("command: dotnet workload restore"))
                    throw new InvalidOperationException($"Since the '{templateName}' template requires additional dotnet workloads to be installed, it should likely be excluded during template discovery.");

                throw new InvalidOperationException($"The dotnet restore operation failed.");
            }

            static async Task AssertProjectLoadsCleanlyAsync(string projectFilePath, string[] ignoredDiagnostics)
            {
                using var workspace = CreateMSBuildWorkspace();
                var project = await workspace.OpenProjectAsync(projectFilePath, cancellationToken: CancellationToken.None);

                AssertEx.Empty(workspace.Diagnostics, "(Workspace Diagnostics)");

                var compilation = await project.GetCompilationAsync();
                Assert.NotNull(compilation);

                // Unnecessary using directives are reported with a severity of Hidden
                var diagnostics = compilation!.GetDiagnostics()
                    .Where(diagnostic => diagnostic.Severity > DiagnosticSeverity.Hidden && ignoredDiagnostics.Contains(diagnostic.Id) != true);

                AssertEx.Empty(diagnostics, "(Compiler Diagnostics)");
            }
        }

        private static ProcessResult RunDotNet(string arguments, string? workingDirectory = null)
        {
            Assert.NotNull(DotNetSdkLocator.SdkPath);

            var dotNetExeName = "dotnet" + (Path.DirectorySeparatorChar == '/' ? "" : ".exe");
            var fileName = Path.Combine(DotNetSdkLocator.SdkPath!, dotNetExeName);

            return ProcessUtilities.Run(fileName, arguments, workingDirectory, additionalEnvironmentVars: [new KeyValuePair<string, string>("DOTNET_CLI_UI_LANGUAGE", "en")]);
        }
    }
}
