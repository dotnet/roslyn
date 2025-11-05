// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using Basic.CompilerLog.Util;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public sealed class SdkIntegrationTests : IDisposable
{
    public const string NetCoreTfm = "net9.0";

    public ITestOutputHelper TestOutputHelper { get; }
    public TempRoot Temp { get; }
    public TempDirectory ProjectDir { get; }
    public ArtifactUploadUtil ArtifactUploadUtil { get; }

    public SdkIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        Assert.NotNull(DotNetSdkTestBase.DotNetInstallDir);
        TestOutputHelper = testOutputHelper;
        Temp = new TempRoot();
        ProjectDir = Temp.CreateDirectory();
        ArtifactUploadUtil = new ArtifactUploadUtil(testOutputHelper);
    }

    public void Dispose()
    {
        Temp.Dispose();
        ArtifactUploadUtil.Dispose();
    }

    public static string GetRoslynTargetsPath()
    {
        var p = typeof(SdkIntegrationTests).Assembly.Location!;
        var dir = Path.GetDirectoryName(p)!;
        var targets = Path.Combine(dir, "Microsoft.CSharp.Core.targets");
        Assert.True(File.Exists(targets));
        return dir;
    }

    /// <summary>
    /// Runs the build and returns the path to the binary log file
    /// </summary>
    private string RunBuild(
        string projectFilePath,
        string? additionalArguments = null,
        IEnumerable<KeyValuePair<string, string>>? additionalEnvironmentVars = null,
        bool succeeds = true)
    {
        var workingDirectory = Path.GetDirectoryName(projectFilePath)!;
        ArtifactUploadUtil.AddDirectory(workingDirectory);

        var args = new StringBuilder();
        args.Append("build /bl ");
        args.Append($"/p:RoslynTargetsPath={GetRoslynTargetsPath()} ");
        args.Append($"/p:RoslynTasksAssembly={typeof(Csc).Assembly.Location} ");
        args.Append($"/p:RoslynCompilerType=Custom ");
        if (additionalArguments is not null)
        {
            args.Append(additionalArguments);
        }

        var result = ProcessUtilities.Run(DotNetSdkTestBase.DotNetExeName, args.ToString(), workingDirectory, additionalEnvironmentVars);
        if (succeeds)
        {
            Assert.True(result.ExitCode == 0, $"MSBuild failed with exit code {result.ExitCode}: {result.Output}");
        }
        else
        {
            Assert.False(result.ExitCode == 0, $"MSBuild failed with exit code {result.ExitCode}: {result.Output}");
        }

        return Path.Combine(workingDirectory, "msbuild.binlog");
    }

    private static List<Compilation> ReadCompilations(string binaryLogPath)
    {
        using var reader = BinaryLogReader.Create(binaryLogPath, BasicAnalyzerKind.None);
        var list = new List<Compilation>();
        foreach (var compilerCall in reader.ReadAllCompilerCalls())
        {
            var compilation = reader.ReadCompilationData(compilerCall).GetCompilationAfterGenerators();
            list.Add(compilation);
        }

        return list;
    }

    [ConditionalFact(typeof(DotNetSdkAvailable))]
    public void Console()
    {
        var projectFile = ProjectDir.CreateFile("console.csproj");
        projectFile.WriteAllText($"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>{NetCoreTfm}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        ProjectDir.CreateFile("hello.cs").WriteAllText("""
            Console.WriteLine("Hello, World!");
            """);

        var binlogPath = RunBuild(projectFile.Path);
        var compilations = ReadCompilations(binlogPath);
        Assert.Single(compilations);
        Assert.True(compilations[0].SyntaxTrees.Any(x => Path.GetFileName(x.FilePath) == "hello.cs"));
        ArtifactUploadUtil.SetSucceeded();
    }

    [ConditionalTheory(typeof(DotNetSdkAvailable))]
    [InlineData(NetCoreTfm, true)]
    [InlineData("net6.0", true)]
    [InlineData("netstandard2.0", false)]
    [InlineData("net472", false)]
    public void StrongNameWarningCSharp(string tfm, bool expectStrongNameSuppression)
    {
        var projectFile = ProjectDir.CreateFile("console.csproj");
        projectFile.WriteAllText($"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <OutputType>Library</OutputType>
                <TargetFramework>{tfm}</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        ProjectDir.CreateFile("hello.cs").WriteAllText("""
            class C { }
            """);

        var binlogPath = RunBuild(projectFile.Path);
        var compilation = ReadCompilations(binlogPath).Single();
        var options = compilation.Options;
        if (expectStrongNameSuppression)
        {
            Assert.True(options.SpecificDiagnosticOptions.TryGetValue("CS8002", out ReportDiagnostic d));
            Assert.Equal(ReportDiagnostic.Suppress, d);
        }
        else
        {
            Assert.False(options.SpecificDiagnosticOptions.TryGetValue("CS8002", out _));
        }

        ArtifactUploadUtil.SetSucceeded();
    }

    [ConditionalTheory(typeof(DotNetSdkAvailable))]
    [InlineData(NetCoreTfm, true)]
    [InlineData("net6.0", true)]
    [InlineData("netstandard2.0", false)]
    [InlineData("net472", false)]
    public void StrongNameWarningVisualBasic(string tfm, bool expectStrongNameSuppression)
    {
        var projectFile = ProjectDir.CreateFile("console.vbproj");
        projectFile.WriteAllText($"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <OutputType>Library</OutputType>
                <TargetFramework>{tfm}</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        ProjectDir.CreateFile("hello.vb").WriteAllText("""
            Module M
            End Module
            """);

        var binlogPath = RunBuild(projectFile.Path);
        var compilation = ReadCompilations(binlogPath).Single();
        var options = compilation.Options;
        if (expectStrongNameSuppression)
        {
            Assert.True(options.SpecificDiagnosticOptions.TryGetValue("BC41997", out ReportDiagnostic d));
            Assert.Equal(ReportDiagnostic.Suppress, d);
        }
        else
        {
            Assert.False(options.SpecificDiagnosticOptions.TryGetValue("BC41997", out _));
        }

        ArtifactUploadUtil.SetSucceeded();
    }
}
