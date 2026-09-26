// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Basic.CompilerLog.Util;
using Microsoft.Build.Logging.StructuredLogger;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public sealed class SdkIntegrationTests : IDisposable
{
    public const string NetCoreTfm = "net10.0";

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

    [ConditionalFact(typeof(DotNetSdkAvailable))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/82721")]
    public void EditorConfig_EmbeddedInBinlog_Generated()
    {
        var projectFile = ProjectDir.CreateFile("console.csproj");
        projectFile.WriteAllText($"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{NetCoreTfm}</TargetFramework>
              </PropertyGroup>

              <ItemGroup>
                <CompilerVisibleProperty Include="RootNamespace" />
              </ItemGroup>
            </Project>
            """);

        ProjectDir.CreateFile("c.cs").WriteAllText("""
            class C { }
            """);

        var binlogPath = RunBuild(projectFile.Path);

        var build = BinaryLog.ReadBuild(binlogPath);

        string embeddedText = build.SourceFiles
            .Single(static f => f.FullPath.EndsWith("GeneratedMSBuildEditorConfig.editorconfig", StringComparison.OrdinalIgnoreCase))
            .Text;

        Assert.Contains("is_global = true", embeddedText);
        Assert.Contains("build_property.RootNamespace", embeddedText);
        ArtifactUploadUtil.SetSucceeded();
    }

    [ConditionalFact(typeof(DotNetSdkAvailable))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/82721")]
    public void EditorConfig_EmbeddedInBinlog_FromTarget()
    {
        string analyzerGlobalConfigText = """
            is_global = true
            some_prop = some_val
            """;
        var analyzerGlobalConfig = ProjectDir.CreateFile("analyzer.globalconfig").WriteAllText(analyzerGlobalConfigText);

        var projectFile = ProjectDir.CreateFile("console.csproj");
        projectFile.WriteAllText($"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{NetCoreTfm}</TargetFramework>
              </PropertyGroup>

              <Target Name="AddAnalyzerPackageEditorConfigFile" BeforeTargets="CoreCompile">
                <ItemGroup>
                  <EditorConfigFiles Include="{analyzerGlobalConfig.Path}" />
                </ItemGroup>
              </Target>
            </Project>
            """);

        ProjectDir.CreateFile("c.cs").WriteAllText("""
            class C { }
            """);

        var binlogPath = RunBuild(projectFile.Path);

        var build = BinaryLog.ReadBuild(binlogPath);

        string embeddedText = build.SourceFiles
            .Single(f => f.FullPath.Equals(analyzerGlobalConfig.Path, StringComparison.OrdinalIgnoreCase))
            .Text;

        Assert.Equal(analyzerGlobalConfigText, embeddedText);
        ArtifactUploadUtil.SetSucceeded();
    }

    [ConditionalFact(typeof(DotNetSdkAvailable))]
    public void CoreCompile_NestedCSharpCoreCompileTargetState()
    {
        var appConfig = ProjectDir.CreateFile("app.config").WriteAllText("<configuration />");
        var existingBinlogInput = ProjectDir.CreateFile("existing.txt").WriteAllText("existing");
        var editorConfig = ProjectDir.CreateFile(".editorconfig").WriteAllText("is_global = true");
        var resource = ProjectDir.CreateFile("resource.txt").WriteAllText("resource");
        var projectFile = ProjectDir.CreateFile("console.csproj");
        // Changes made in `_CSharpCoreCompile` should be visible to both `TargetsTriggeredAfterCSharpCompilation` and targets with `AfterTargets="CoreCompile"`
        // but remain invisible to targets in `TargetsTriggeredByCompilation`
        projectFile.WriteAllText($"""
                        <Project Sdk="Microsoft.NET.Sdk">
                            <PropertyGroup>
                                <TargetFramework>{NetCoreTfm}</TargetFramework>
                                <UseAppConfigForCompiler>true</UseAppConfigForCompiler>
                                <AppConfigForCompiler></AppConfigForCompiler>
                                <AppConfig>{appConfig.Path}</AppConfig>
                                <DebugSymbols>false</DebugSymbols>
                                <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
                                <ProvideCommandLineArgs>true</ProvideCommandLineArgs>
                                <TargetsTriggeredByCompilation>VerifyTriggeredByCompilation</TargetsTriggeredByCompilation>
                                <TargetsTriggeredAfterCSharpCompilation>VerifyTriggeredAfterCSharpCompilation</TargetsTriggeredAfterCSharpCompilation>
                            </PropertyGroup>
                            <ItemGroup>
                                <EmbedInBinlog Include="{existingBinlogInput.Path}" />
                            </ItemGroup>
                            <Target Name="CompilePreparation" BeforeTargets="CoreCompile">
                                <ItemGroup>
                                    <CscCommandLineArgs Remove="@(CscCommandLineArgs)" />
                                    <_CoreCompileResourceInputs Include="{resource.Path}" WithCulture="false" />

                                    <_NestedExistingBinlogInput1 Include="@(EmbedInBinlog)" Condition="'%(Identity)' == '{existingBinlogInput.Path}'" />
                                    <_NestedEditorConfigBinlogInput1 Include="@(EmbedInBinlog)" Condition="'%(Identity)' == '{editorConfig.Path}'" />
                                </ItemGroup>                                
                                <Error Condition="$([System.String]::Copy(';$(NoWarn);').Contains(';8002;')) == 'true'" Text="8002 was available in before CoreCompile scope." />
                                <Error Condition="'$(AppConfigForCompiler)' != ''" Text="AppConfigForCompiler was set in before CoreCompile  scope." />
                                <Error Condition="'@(CscCommandLineArgs->Count())' != '0'" Text="CscCommandLineArgs were available in before CoreCompile  scope." />
                                <Error Condition="'@(_NestedExistingBinlogInput1->Count())' != '1'" Text="Existing EmbedInBinlog input was not preserved in before CoreCompile  scope." />
                                <Error Condition="'@(_NestedEditorConfigBinlogInput1->Count())' != '0'" Text="EditorConfig EmbedInBinlog input was included in before CoreCompile  scope." />
                                <Error Condition="'@(_CoreCompileResourceInputs->Count())' == '0'" Text="_CoreCompileResourceInputs was empty in before CoreCompile scope." />
                            </Target>
                            <Target Name="VerifyTriggeredByCompilation">
                                <ItemGroup>
                                    <_NestedExistingBinlogInput2 Include="@(EmbedInBinlog)" Condition="'%(Identity)' == '{existingBinlogInput.Path}'" />
                                    <_NestedEditorConfigBinlogInput2 Include="@(EmbedInBinlog)" Condition="'%(Identity)' == '{editorConfig.Path}'" />
                                </ItemGroup>                                
                                <Error Condition="$([System.String]::Copy(';$(NoWarn);').Contains(';8002;')) == 'true'" Text="8002 was available in TriggeredByCompilation scope." />
                                <Error Condition="'$(AppConfigForCompiler)' != ''" Text="AppConfigForCompiler was available in TriggeredByCompilation scope." />
                                <Error Condition="'@(CscCommandLineArgs->Count())' != '0'" Text="CscCommandLineArgs were available in TriggeredByCompilation scope." />
                                <Error Condition="'@(_NestedExistingBinlogInput2->Count())' != '1'" Text="Existing EmbedInBinlog input was not preserved in TriggeredByCompilation scope." />
                                <Error Condition="'@(_NestedEditorConfigBinlogInput2->Count())' != '0'" Text="EditorConfig EmbedInBinlog input was available in TriggeredByCompilation scope." />
                                <Error Condition="'@(_CoreCompileResourceInputs->Count())' == '0'" Text="_CoreCompileResourceInputs was cleared." />
                            </Target>
                            <Target Name="VerifyTriggeredAfterCSharpCompilation">
                                <ItemGroup>
                                    <_NestedExistingBinlogInput3 Include="@(EmbedInBinlog)" Condition="'%(Identity)' == '{existingBinlogInput.Path}'" />
                                    <_NestedEditorConfigBinlogInput3 Include="@(EmbedInBinlog)" Condition="'%(Identity)' == '{editorConfig.Path}'" />
                                </ItemGroup>                                
                                <Error Condition="$([System.String]::Copy(';$(NoWarn);').Contains(';8002;')) != 'true'" Text="8002 was not available in AfterCSharpCompilation scope." />
                                <Error Condition="'$(AppConfigForCompiler)' != '{appConfig.Path}'" Text="AppConfigForCompiler was not available in AfterCSharpCompilation scope." />
                                <Error Condition="'@(CscCommandLineArgs->Count())' == '0'" Text="No CscCommandLineArgs were available in AfterCSharpCompilation scope." />
                                <!--Error Condition="'@(_NestedExistingBinlogInput3->Count())' != '1'" Text="Existing EmbedInBinlog input was not preserved in AfterCSharpCompilation scope." /-->
                                <!--Error Condition="'@(_NestedEditorConfigBinlogInput3->Distinct()->Count())' != '1'" Text="EditorConfig EmbedInBinlog input was not available in AfterCSharpCompilation scope." />-->
                                <Error Condition="'@(_CoreCompileResourceInputs->Count())' != '0'" Text="_CoreCompileResourceInputs was not cleared." />
                            </Target>
                            <Target Name="VerifyOuterCompilerState" AfterTargets="CoreCompile">
                                <ItemGroup>
                                    <_ExistingBinlogInput Include="@(EmbedInBinlog)" Condition="'%(Identity)' == '{existingBinlogInput.Path}'" />
                                    <_EditorConfigBinlogInput Include="@(EmbedInBinlog)" Condition="'%(Identity)' == '{editorConfig.Path}'" />
                                </ItemGroup>
                                <Error Condition="$([System.String]::Copy(';$(NoWarn);').Contains(';8002;')) != 'true'" Text="NoWarn was not marshaled." />
                                <Error Condition="'$(AppConfigForCompiler)' != '{appConfig.Path}'" Text="AppConfigForCompiler was not marshaled." />
                                <Error Condition="'@(CscCommandLineArgs->Count())' == '0'" Text="No CscCommandLineArgs were available in the outer scope." />
                                <Error Condition="'@(_ExistingBinlogInput->Count())' != '1'" Text="Existing EmbedInBinlog input was not preserved in the outer scope." />
                                <Error Condition="'@(_EditorConfigBinlogInput->Count())' != '1'" Text="EditorConfig EmbedInBinlog input was not preserved in the outer scope." />
                                <Error Condition="'@(_CoreCompileResourceInputs->Count())' != '0'" Text="_CoreCompileResourceInputs was not cleared." />
                            </Target>
                        </Project>
                        """);

        ProjectDir.CreateFile("c.cs").WriteAllText("class C { }");
        RunBuild(projectFile.Path, succeeds: true);
    }

    [ConditionalFact(typeof(DotNetSdkAvailable))]
    public void CoreCompile_TargetsTriggeredByCSharpCompilation_ContinueOnError()
    {
        // if `TargetsTriggeredByCSharpCompilationContinueOnError` is set to true,
        // targets in `TargetsTriggeredAfterCSharpCompilation` should run even if the targets in `TargetsTriggeredByCompilation` fail
        var projectFile = ProjectDir.CreateFile("console.csproj");
        projectFile.WriteAllText($"""
                        <Project Sdk="Microsoft.NET.Sdk">
                            <PropertyGroup>
                                <TargetFramework>{NetCoreTfm}</TargetFramework>
                                <TargetsTriggeredByCompilation>VerifyTriggeredByCompilation</TargetsTriggeredByCompilation>
                                <TargetsTriggeredByCSharpCompilationContinueOnError>true</TargetsTriggeredByCSharpCompilationContinueOnError>
                                <TargetsTriggeredAfterCSharpCompilation>VerifyTriggeredAfterCSharpCompilation</TargetsTriggeredAfterCSharpCompilation>
                                <_VerifyTriggeredAfterCSharpCompilationExecuted>false</_VerifyTriggeredAfterCSharpCompilationExecuted>
                            </PropertyGroup>
                            <Target Name="VerifyTriggeredByCompilation">
                                <Error Text="VerifyTriggeredByCompilation error." />
                            </Target>
                            <Target Name="VerifyTriggeredAfterCSharpCompilation">
                                <PropertyGroup>
                                    <_VerifyTriggeredAfterCSharpCompilationExecuted>true</_VerifyTriggeredAfterCSharpCompilationExecuted>
                                </PropertyGroup>
                            </Target>
                            <Target Name="VerifyOuterCompilerState" AfterTargets="CoreCompile">
                                <Error Condition="'$(_VerifyTriggeredAfterCSharpCompilationExecuted)' != 'true'" Text="VerifyTriggeredAfterCSharpCompilation did not execute." />
                            </Target>
                        </Project>
                        """);

        ProjectDir.CreateFile("c.cs").WriteAllText("class C { }");
        var binlogPath = RunBuild(projectFile.Path, succeeds: false);
        var build = BinaryLog.ReadBuild(binlogPath);
        var errors = build.FindChildrenRecursive<Error>(static _ => true);

        Assert.Equal(1, errors.Count);
        Assert.Contains(errors, static error => error.Text == "VerifyTriggeredByCompilation error.");
        Assert.DoesNotContain(errors, static error => error.Text == "VerifyTriggeredAfterCSharpCompilation did not execute.");
    }
}
