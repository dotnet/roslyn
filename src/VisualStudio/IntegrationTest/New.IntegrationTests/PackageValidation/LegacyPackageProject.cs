// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;

namespace Roslyn.VisualStudio.NewIntegrationTests.PackageValidation;

/// <summary>
/// Creates an isolated scratch directory containing a non-SDK-style (packages.config) net472 project
/// and solution, along with an isolated NuGet configuration. Keeping generation here means the
/// package upgrade test itself only has to drive the Package Manager Console and inspect results.
/// </summary>
internal sealed class LegacyPackageProject
{
    private LegacyPackageProject(
        string rootDirectory,
        string solutionFilePath,
        string projectFilePath,
        string projectName,
        string packagesDirectory,
        string nuGetConfigPath,
        string resultsDirectory)
    {
        RootDirectory = rootDirectory;
        SolutionFilePath = solutionFilePath;
        ProjectFilePath = projectFilePath;
        ProjectName = projectName;
        PackagesDirectory = packagesDirectory;
        NuGetConfigPath = nuGetConfigPath;
        ResultsDirectory = resultsDirectory;
    }

    /// <summary>The isolated scratch directory containing all generated content.</summary>
    public string RootDirectory { get; }

    public string SolutionFilePath { get; }

    public string ProjectFilePath { get; }

    public string ProjectName { get; }

    /// <summary>The solution-local <c>packages</c> directory used by packages.config restore.</summary>
    public string PackagesDirectory { get; }

    /// <summary>The isolated <c>NuGet.config</c>, which only maps the sources given at creation time.</summary>
    public string NuGetConfigPath { get; }

    /// <summary>Directory for command result and diagnostic files produced while running the test.</summary>
    public string ResultsDirectory { get; }

    /// <summary>The <c>packages.config</c> file, which only exists after the first package install.</summary>
    public string PackagesConfigPath => Path.Combine(Path.GetDirectoryName(ProjectFilePath)!, "packages.config");

    /// <summary>
    /// Creates the scratch directory and writes the solution, project, source, and NuGet configuration.
    /// </summary>
    /// <param name="projectName">The name given to the generated project and solution.</param>
    /// <param name="packageSources">
    /// Package sources written to the isolated <c>NuGet.config</c>, in priority order. Local directories
    /// (for example the locally built package payload) and remote feeds may be mixed.
    /// </param>
    /// <param name="rootDirectory">
    /// The scratch directory to create. When not specified, a unique directory under the temp path is used.
    /// </param>
    public static LegacyPackageProject Create(
        string projectName,
        (string Name, string Value)[] packageSources,
        string? rootDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("A project name is required.", nameof(projectName));
        }

        if (packageSources is null || packageSources.Length == 0)
        {
            throw new ArgumentException("At least one package source is required.", nameof(packageSources));
        }

        rootDirectory ??= Path.Combine(Path.GetTempPath(), "RoslynPackageValidation", Guid.NewGuid().ToString("N"));

        var projectDirectory = Path.Combine(rootDirectory, projectName);
        var packagesDirectory = Path.Combine(rootDirectory, "packages");
        var resultsDirectory = Path.Combine(rootDirectory, "results");

        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(packagesDirectory);
        Directory.CreateDirectory(resultsDirectory);

        var projectGuid = Guid.NewGuid();
        var projectFilePath = Path.Combine(projectDirectory, projectName + ".csproj");
        var solutionFilePath = Path.Combine(rootDirectory, projectName + ".sln");
        var nuGetConfigPath = Path.Combine(rootDirectory, "NuGet.config");

        File.WriteAllText(projectFilePath, CreateProjectContent(projectName, projectGuid));
        File.WriteAllText(Path.Combine(projectDirectory, "Class1.cs"), CreateSourceContent(projectName));
        File.WriteAllText(Path.Combine(projectDirectory, "AnalyzerTarget.cs"), CreateAnalyzerTargetSourceContent(projectName));
        File.WriteAllText(solutionFilePath, CreateSolutionContent(projectName, projectGuid));
        File.WriteAllText(nuGetConfigPath, CreateNuGetConfigContent(packageSources, packagesDirectory));

        return new LegacyPackageProject(
            rootDirectory,
            solutionFilePath,
            projectFilePath,
            projectName,
            packagesDirectory,
            nuGetConfigPath,
            resultsDirectory);
    }

    /// <summary>
    /// Gets a path under <see cref="ResultsDirectory"/> for a result or diagnostic file.
    /// </summary>
    public string GetResultPath(string fileName)
        => Path.Combine(ResultsDirectory, fileName);

    /// <summary>
    /// Deletes the scratch directory, ignoring failures caused by files still held by Visual Studio.
    /// </summary>
    public void TryDelete()
    {
        try
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string CreateProjectContent(string projectName, Guid projectGuid)
        => string.Format(
            CultureInfo.InvariantCulture,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <Project ToolsVersion="15.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
              <PropertyGroup>
                <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
                <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
                <ProjectGuid>{{{0}}}</ProjectGuid>
                <OutputType>Library</OutputType>
                <RootNamespace>{1}</RootNamespace>
                <AssemblyName>{1}</AssemblyName>
                <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
                <FileAlignment>512</FileAlignment>
              </PropertyGroup>
              <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
                <DebugSymbols>true</DebugSymbols>
                <DebugType>full</DebugType>
                <Optimize>false</Optimize>
                <OutputPath>bin\Debug\</OutputPath>
                <DefineConstants>DEBUG;TRACE</DefineConstants>
              </PropertyGroup>
              <ItemGroup>
                <Reference Include="System" />
                <Reference Include="System.Core" />
              </ItemGroup>
              <ItemGroup>
                <Compile Include="AnalyzerTarget.cs" />
                <Compile Include="Class1.cs" />
              </ItemGroup>
              <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
            </Project>
            """,
            projectGuid.ToString("D").ToUpperInvariant(),
            projectName);

    private static string CreateSourceContent(string projectName)
        => string.Format(
            CultureInfo.InvariantCulture,
            """
            namespace {0}
            {{
                public class Class1
                {{
                    public int Add(int first, int second)
                    {{
                        return first + second;
                    }}
                }}
            }}
            """,
            projectName);

    /// <summary>
    /// Source which is representative of code the Roslyn analyzer packages report on, so an upgrade
    /// which fails to deploy analyzers is observable.
    /// </summary>
    private static string CreateAnalyzerTargetSourceContent(string projectName)
        => string.Format(
            CultureInfo.InvariantCulture,
            """
            using System;
            using System.Collections.Generic;

            namespace {0}
            {{
                public class AnalyzerTarget
                {{
                    public IEnumerable<string> GetNames(IEnumerable<string> values)
                    {{
                        var result = new List<string>();
                        foreach (var value in values)
                        {{
                            if (!string.IsNullOrEmpty(value))
                            {{
                                result.Add(value.Trim());
                            }}
                        }}

                        return result;
                    }}
                }}
            }}
            """,
            projectName);

    private static string CreateSolutionContent(string projectName, Guid projectGuid)
        => string.Format(
            CultureInfo.InvariantCulture,
            """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}") = "{0}", "{0}\{0}.csproj", "{{{1}}}"
            EndProject
            Global
            	GlobalSection(SolutionConfigurationPlatforms) = preSolution
            		Debug|Any CPU = Debug|Any CPU
            		Release|Any CPU = Release|Any CPU
            	EndGlobalSection
            	GlobalSection(ProjectConfigurationPlatforms) = postSolution
            		{{{1}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
            		{{{1}}}.Debug|Any CPU.Build.0 = Debug|Any CPU
            		{{{1}}}.Release|Any CPU.ActiveCfg = Release|Any CPU
            		{{{1}}}.Release|Any CPU.Build.0 = Release|Any CPU
            	EndGlobalSection
            EndGlobal
            """,
            projectName,
            projectGuid.ToString("D").ToUpperInvariant());

    private static string CreateNuGetConfigContent((string Name, string Value)[] packageSources, string packagesDirectory)
    {
        var sources = new StringBuilder();
        foreach (var (name, value) in packageSources)
        {
            sources.AppendLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "    <add key=\"{0}\" value=\"{1}\" />",
                    SecurityElement.Escape(name),
                    SecurityElement.Escape(value)));
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <config>
                <add key="repositoryPath" value="{0}" />
              </config>
              <packageSources>
                <clear />
            {1}  </packageSources>
              <disabledPackageSources>
                <clear />
              </disabledPackageSources>
            </configuration>
            """,
            SecurityElement.Escape(packagesDirectory),
            sources.ToString());
    }
}
