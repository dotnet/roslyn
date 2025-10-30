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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests;

public class NewlyCreatedProjectsFromDotNetNew : MSBuildWorkspaceTestBase
{
    // When running on Helix the machine will only have the expected SDK
    // installed. However, when running on developer machines there could
    // be any number of SDKs installed. We will locate the Roslyn global.json
    // and use it to ensure our tests are run with the proper SDK.
    private static readonly string? s_globalJsonPath;

    // The Maui templates require additional dotnet workloads to be installed.
    // Running `dotnet workload restore` will install workloads but may require
    // admin permissions. Additionally, a restart may be required after workload
    // installation.
    private const bool ExcludeMauiTemplates = true;

    static NewlyCreatedProjectsFromDotNetNew()
    {
        // When running on developer machines we will try and use the same global.json
        // as we use for our own build.
        var globalJsonPath = Path.Combine(GetSolutionFolder(), "global.json");

        // When running on Helix we will not locate a global.json file.
        if (File.Exists(globalJsonPath))
        {
            s_globalJsonPath = globalJsonPath;
        }

        static string GetSolutionFolder()
        {
            // Expected assembly path:
            //  <solutionFolder>\artifacts\bin\Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests\<Configuration>\<TFM>\Microsoft.CodeAnalysis.Workspaces.MSBuild.UnitTests.dll
            var assemblyLocation = typeof(DotNetSdkMSBuildInstalled).Assembly.Location;
            var solutionFolder = Directory.GetParent(assemblyLocation)
                ?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;
            Assumes.NotNull(solutionFolder);
            return solutionFolder;
        }
    }

    public NewlyCreatedProjectsFromDotNetNew(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [ConditionalTheory(typeof(DotNetSdkMSBuildInstalled))]
    [MemberData(nameof(GetCSharpProjectTemplateNames), DisableDiscoveryEnumeration = false)]
    public Task ValidateCSharpTemplateProjects(string templateName)
    {
        if (templateName is "blazor" or "blazorwasm")
        {
            // https://github.com/dotnet/roslyn/issues/80263
            return Task.CompletedTask;
        }
        return AssertTemplateProjectLoadsCleanlyAsync(templateName, LanguageNames.CSharp);
    }

    [ConditionalTheory(typeof(DotNetSdkMSBuildInstalled))]
    [MemberData(nameof(GetVisualBasicProjectTemplateNames), DisableDiscoveryEnumeration = false)]
    public async Task ValidateVisualBasicTemplateProjects(string templateName)
    {
        var ignoredDiagnostics = !ExecutionConditionUtil.IsWindows
            ? [
                // Type 'Global.Microsoft.VisualBasic.ApplicationServices.ApplicationBase' is not defined.
                // Bug: https://github.com/dotnet/roslyn/issues/72014
                "BC30002",
            ]
            : Array.Empty<string>();

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

        var result = RunDotNet($"new list --type project --language {language}", loggerFactory: null);

        var lines = result.Output.Split(["\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

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

            var columns = line.Split(["  "], StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .ToArray();

            // Some templates may list multiple short names for the same template. It
            // will suffice to take the first short name.
            var templateShortName = columns[1].Split(',').First();

            if (ExcludeMauiTemplates && templateShortName.StartsWith("maui"))
                continue;

            templateNames.Add(templateShortName);
        }

        Assert.True(foundDivider);

        return templateNames;
    }

    private async Task AssertTemplateProjectLoadsCleanlyAsync(string templateName, string languageName, string[]? ignoredDiagnostics = null)
    {
        if (ignoredDiagnostics?.Length > 0)
        {
            TestOutput.WriteLine($"""
                Ignoring compiler diagnostics: "{string.Join("\", \"", ignoredDiagnostics)}"
                """);
        }

        var projectDirectory = SolutionDirectory.Path;
        var projectFilePath = GetProjectFilePath(projectDirectory, languageName);

        CreateNewProject(templateName, projectDirectory, languageName);

        await AssertProjectLoadsCleanlyAsync(projectFilePath, ignoredDiagnostics ?? []);

        return;

        static string GetProjectFilePath(string projectDirectory, string languageName)
        {
            var projectName = new DirectoryInfo(projectDirectory).Name;
            var projectExtension = languageName switch
            {
                LanguageNames.CSharp => "csproj",
                LanguageNames.VisualBasic => "vbproj",
                _ => throw new ArgumentOutOfRangeException(nameof(languageName), actualValue: languageName, message: "Only C# and VB.NET projects are supported.")
            };
            return Path.Combine(projectDirectory, $"{projectName}.{projectExtension}");
        }

        void CreateNewProject(string templateName, string outputDirectory, string languageName)
        {
            var language = languageName switch
            {
                LanguageNames.CSharp => "C#",
                LanguageNames.VisualBasic => "VB",
                _ => throw new ArgumentOutOfRangeException(nameof(languageName), actualValue: languageName, message: "Only C# and VB.NET projects are supported.")
            };

            TryCopyGlobalJson(outputDirectory);

            var newResult = RunDotNet($"""
                new "{templateName}" -o "{outputDirectory}" --language "{language}"
                """, LoggerFactory, outputDirectory);

            // Most templates invoke restore as a post-creation action. However, some, like the
            // Maui templates, do not run restore since they require additional workloads to be
            // installed.
            if (newResult.Output.Contains("Restoring"))
            {
                return;
            }

            try
            {
                // Attempt a restore and see if we are instructed to install additional workloads.
                var restoreResult = RunDotNet($"restore", LoggerFactory, outputDirectory);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("command: dotnet workload restore"))
            {
                throw new InvalidOperationException($"The '{templateName}' template requires additional dotnet workloads to be installed. It should be excluded during template discovery. " + ex.Message);
            }
        }

        static void TryCopyGlobalJson(string outputDirectory)
        {
            // When running in Helix we will not find a global.json to copy.
            if (s_globalJsonPath is null)
            {
                return;
            }

            var tempGlobalJsonPath = Path.Combine(outputDirectory, "global.json");
            File.Copy(s_globalJsonPath, tempGlobalJsonPath);
        }

        async Task AssertProjectLoadsCleanlyAsync(string projectFilePath, string[] ignoredDiagnostics)
        {
            using var workspace = CreateMSBuildWorkspace();
            var project = await workspace.OpenProjectAsync(projectFilePath, cancellationToken: CancellationToken.None);

            AssertEx.Empty(workspace.Diagnostics, $"The following workspace diagnostics are being reported for the template.");

            var compilation = await project.GetRequiredCompilationAsync(CancellationToken.None);
            AssertEx.Empty(await project.GetSourceGeneratorDiagnosticsAsync(CancellationToken.None), $"The following source generator diagnostics are being reported for the template.");

            // Unnecessary using directives are reported with a severity of Hidden
            var nonHiddenDiagnostics = compilation.GetDiagnostics()
                .WhereAsArray(diagnostic => diagnostic.Severity > DiagnosticSeverity.Hidden);

            // For good test hygiene lets ensure that all ignored diagnostics were actually reported.
            var reportedDiagnosticIds = nonHiddenDiagnostics
                .Select(diagnostic => diagnostic.Id)
                .ToImmutableHashSet();
            var unnecessaryIgnoreDiagnostics = ignoredDiagnostics
                .Where(id => !reportedDiagnosticIds.Contains(id));

            AssertEx.Empty(unnecessaryIgnoreDiagnostics, $"The following diagnostics are unnecessarily being ignored for the template.");

            var filteredDiagnostics = nonHiddenDiagnostics
                .Where(diagnostic => !ignoredDiagnostics.Contains(diagnostic.Id));

            AssertEx.Empty(filteredDiagnostics, $"The following compiler diagnostics are being reported for the template.");
        }
    }

    private static ProcessResult RunDotNet(string arguments, ILoggerFactory? loggerFactory, string? workingDirectory = null)
    {
        var dotNetExeName = "dotnet" + (Path.DirectorySeparatorChar == '/' ? "" : ".exe");

        // Ensure output is in english since we will be parsing values from it.
        Dictionary<string, string> additionalEnvironmentVars = new()
        {
            ["DOTNET_CLI_UI_LANGUAGE"] = "en"
        };

        var result = ProcessUtilities.Run(dotNetExeName, arguments, workingDirectory, additionalEnvironmentVars);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine,
                [
                    $"`dotnet {arguments}` returned a non-zero exit code.",
                    "Output:",
                    result.Output,
                    "Error:",
                    result.Errors
                ]));
        }

        var logger = loggerFactory?.CreateLogger("dotnet.exe output");

        logger?.LogTrace(result.Output);

        return result;
    }
}
