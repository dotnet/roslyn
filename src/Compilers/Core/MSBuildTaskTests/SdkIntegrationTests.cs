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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
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

    [ConditionalTheory(typeof(DotNetSdkAvailable)), CombinatorialData]
    public void RoslynCompilerCache(
        bool visualBasic,
        bool? useCache,
        [CombinatorialValues(null, "explicit-cache")] string? cachePath)
    {
        const string informationalVersion = "1.2.3+custom";
        var extension = visualBasic ? "vb" : "cs";
        var worktreeCount = useCache == true ? 2 : 1;
        for (var i = 0; i < worktreeCount; i++)
        {
            var sourceRevisionId = $"revision{i}";
            var worktree = ProjectDir.CreateDirectory($"worktree{i}=a, b");
            var projectFile = worktree.CreateFile($"test.{extension}proj").WriteAllText($"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{NetCoreTfm}</TargetFramework>
                    <UseRoslynCompilerCache>{useCache}</UseRoslynCompilerCache>
                    <Deterministic>false</Deterministic>
                    <InformationalVersion>{informationalVersion}</InformationalVersion>
                    <SourceRevisionId>{sourceRevisionId}</SourceRevisionId>
                    <EnableSourceControlManagerQueries>false</EnableSourceControlManagerQueries>
                    <SkipCompilerExecution>true</SkipCompilerExecution>
                    <ProvideCommandLineArgs>true</ProvideCommandLineArgs>
                  </PropertyGroup>
                  <ItemGroup>
                    <SourceRoot Include="$(MSBuildProjectDirectory){Path.DirectorySeparatorChar}" SourceControl="git" SourceLinkUrl="https://example.invalid/sources/*" />
                  </ItemGroup>
                  <Target Name="TestCompilerCache" DependsOnTargets="CoreCompile">
                    <WriteLinesToFile File="args.txt" Lines="@(CscCommandLineArgs);@(VbcCommandLineArgs)" Overwrite="true" />
                  </Target>
                </Project>
                """);
            worktree.CreateFile($"test.{extension}").WriteAllText(visualBasic ? "Public Class C\nEnd Class" : "public class C { }");

            var features = cachePath is null ? "existing-feature" : $"existing-feature%3Buse-global-cache={cachePath}";
            RunBuild(
                projectFile.Path,
                additionalArguments: $"/t:TestCompilerCache /p:Features={features}",
                additionalEnvironmentVars: new Dictionary<string, string>
                {
                    ["ROSLYN_CACHE_PATH"] = "",
                });

            var args = File.ReadAllLines(Path.Combine(worktree.Path, "args.txt"));
            Assert.Contains(useCache == true ? "/deterministic+" : "/deterministic-", args);
            CommandLineArguments arguments = visualBasic
                ? VisualBasicCommandLineParser.Default.Parse(args, worktree.Path, sdkDirectory: null, additionalReferenceDirectories: null)
                : CSharpCommandLineParser.Default.Parse(args, worktree.Path, sdkDirectory: null, additionalReferenceDirectories: null);

            var assemblyInfoPath = arguments.SourceFiles.Single(source => Path.GetFileName(source.Path) == $"test.AssemblyInfo.{extension}").Path;
            var expectedInformationalVersion = useCache == true ? informationalVersion : $"{informationalVersion}.{sourceRevisionId}";
            Assert.Contains(
                $"AssemblyInformationalVersionAttribute(\"{expectedInformationalVersion}\")",
                File.ReadAllText(assemblyInfoPath));

            Assert.Equal("true", arguments.ParseOptions.Features["existing-feature"]);
            if (useCache == true || cachePath is not null)
            {
                Assert.Equal(cachePath ?? "true", arguments.ParseOptions.Features["use-global-cache"]);
            }
            else
            {
                Assert.DoesNotContain("use-global-cache", arguments.ParseOptions.Features.Keys);
            }

            if (useCache == true)
            {
                Assert.Contains(arguments.PathMap, mapping => mapping.Key == worktree.Path + Path.DirectorySeparatorChar && mapping.Value == "/_/");
                Assert.Null(arguments.SourceLink);
            }
            else
            {
                Assert.Empty(arguments.PathMap);
                Assert.NotNull(arguments.SourceLink);
                Assert.True(File.Exists(arguments.SourceLink));
            }
        }

        ArtifactUploadUtil.SetSucceeded();
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

    [ConditionalTheory(typeof(DotNetSdkAvailable))]
    [InlineData("")]
    [InlineData("<MemorySafetyRulesVersion></MemorySafetyRulesVersion>")]
    [InlineData("<MemorySafetyRulesVersion>1</MemorySafetyRulesVersion>")]
    public void MemorySafetyRules_Version1(string property)
    {
        var projectFile = ProjectDir.CreateFile("console.csproj");
        projectFile.WriteAllText($"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net11.0</TargetFramework>
                    <LangVersion>preview</LangVersion>
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                    {property}
                </PropertyGroup>
            </Project>
            """);

        ProjectDir.CreateFile("C.cs").WriteAllText("""
            class C
            {
                void M1()
                {
                    M2();
                }
                unsafe void M2() { }
            }
            """);

        var binlogPath = RunBuild(projectFile.Path);
        var compilation = ReadCompilations(binlogPath).Single();

        compilation.VerifyDiagnostics();

        var options = (CSharpCompilationOptions)compilation.Options;
        Assert.Equal(MemorySafetyRulesVersion.Version1, options.MemorySafetyRulesVersion);

        ArtifactUploadUtil.SetSucceeded();
    }

    [ConditionalFact(typeof(DotNetSdkAvailable))]
    public void MemorySafetyRules_FeatureFlag()
    {
        var projectFile = ProjectDir.CreateFile("console.csproj");
        projectFile.WriteAllText($"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net11.0</TargetFramework>
                    <LangVersion>preview</LangVersion>
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                    <Features>$(Features);updated-memory-safety-rules</Features>
                </PropertyGroup>
            </Project>
            """);

        ProjectDir.CreateFile("C.cs").WriteAllText("""
            class C
            {
                void M1()
                {
                    M2();
                }
                unsafe void M2() { }
            }
            """);

        var binlogPath = RunBuild(projectFile.Path, succeeds: false);
        var compilation = ReadCompilations(binlogPath).Single();

        compilation.VerifyDiagnostics(
            // warning CS9402: Feature flag 'updated-memory-safety-rules' is deprecated and will be removed in a future version. Use MSBuild property 'MemorySafetyRulesVersion=2' or compiler option '/memorysafetyrulesversion:2' instead.
            TestHelpers.Diagnostic(ErrorCode.WRN_DeprecatedFeatureFlag).WithArguments("updated-memory-safety-rules", "MemorySafetyRulesVersion=2", "/memorysafetyrulesversion:2").WithLocation(1, 1),
            // C.cs(5,9): error CS9362: 'C.M2()' must be used in an unsafe context because it is marked as 'unsafe'
            //         M2();
            TestHelpers.Diagnostic(9362, "M2()").WithArguments("C.M2()").WithLocation(5, 9));

        var options = (CSharpCompilationOptions)compilation.Options;
        Assert.Equal(MemorySafetyRulesVersion.Version2, options.MemorySafetyRulesVersion);

        ArtifactUploadUtil.SetSucceeded();
    }

    [ConditionalFact(typeof(DotNetSdkAvailable))]
    public void MemorySafetyRules_Version2()
    {
        var projectFile = ProjectDir.CreateFile("console.csproj");
        projectFile.WriteAllText($"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net11.0</TargetFramework>
                    <LangVersion>preview</LangVersion>
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                    <MemorySafetyRulesVersion>2</MemorySafetyRulesVersion>
                </PropertyGroup>
            </Project>
            """);

        ProjectDir.CreateFile("C.cs").WriteAllText("""
            class C
            {
                void M1()
                {
                    M2();
                }
                unsafe void M2() { }
            }
            """);

        var binlogPath = RunBuild(projectFile.Path, succeeds: false);
        var compilation = ReadCompilations(binlogPath).Single();

        compilation.VerifyDiagnostics(
            // C.cs(5,9): error CS9362: 'C.M2()' must be used in an unsafe context because it is marked as 'unsafe'
            //         M2();
            TestHelpers.Diagnostic(9362, "M2()").WithArguments("C.M2()").WithLocation(5, 9));

        var options = (CSharpCompilationOptions)compilation.Options;
        Assert.Equal(MemorySafetyRulesVersion.Version2, options.MemorySafetyRulesVersion);

        ArtifactUploadUtil.SetSucceeded();
    }

    [ConditionalFact(typeof(DotNetSdkAvailable))]
    public void MemorySafetyRules_InvalidVersion()
    {
        var projectFile = ProjectDir.CreateFile("console.csproj");
        projectFile.WriteAllText($"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net11.0</TargetFramework>
                    <LangVersion>preview</LangVersion>
                    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                    <MemorySafetyRulesVersion>999</MemorySafetyRulesVersion>
                </PropertyGroup>
            </Project>
            """);

        ProjectDir.CreateFile("C.cs").WriteAllText("""
            class C
            {
                void M1()
                {
                    M2();
                }
                unsafe void M2() { }
            }
            """);

        var binlogPath = RunBuild(projectFile.Path, succeeds: false);
        var compilation = ReadCompilations(binlogPath).Single();

        compilation.VerifyDiagnostics(
            // error CS9400: Invalid 'MemorySafetyRulesVersion' value: '999'. Accepted values are: 1, 2
            TestHelpers.Diagnostic(9400).WithArguments("MemorySafetyRulesVersion", "999", "1, 2").WithLocation(1, 1));

        var options = (CSharpCompilationOptions)compilation.Options;
        Assert.Equal((MemorySafetyRulesVersion)999, options.MemorySafetyRulesVersion);

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
                        <_NestedNoWarnCommandLineArgs Include="@(CscCommandLineArgs)" Condition="$([System.String]::Copy('%(Identity)').StartsWith('/nowarn'))" />
                    </ItemGroup>                                
                    <Error Condition="$([System.String]::Copy(';$(NoWarn);').Contains(';8002;')) != 'true'" Text="8002 was not available in AfterCSharpCompilation scope." />
                    <Error Condition="'$(AppConfigForCompiler)' != '{appConfig.Path}'" Text="AppConfigForCompiler was not available in AfterCSharpCompilation scope." />
                    <Error Condition="'@(CscCommandLineArgs->Count())' == '0'" Text="No CscCommandLineArgs were available in AfterCSharpCompilation scope." />
                    <Error Condition="'@(_NestedExistingBinlogInput3->Count())' != '1'" Text="Existing EmbedInBinlog input was not preserved in AfterCSharpCompilation scope." />
                    <Error Condition="'@(_NestedEditorConfigBinlogInput3->Distinct()->Count())' != '1'" Text="EditorConfig EmbedInBinlog input was not available in AfterCSharpCompilation scope." />
                    <Error Condition="'@(_CoreCompileResourceInputs->Count())' != '0'" Text="_CoreCompileResourceInputs was not cleared." />
                    <Error Condition="'@(_NestedNoWarnCommandLineArgs->Count())' != '1'" Text="Expected exactly one /nowarn switch, but found @(_NestedNoWarnCommandLineArgs->Count()): @(_NestedNoWarnCommandLineArgs, ' ')" />
                </Target>
                <Target Name="VerifyOuterCompilerState" AfterTargets="CoreCompile">
                    <ItemGroup>
                        <_ExistingBinlogInput Include="@(EmbedInBinlog)" Condition="'%(Identity)' == '{existingBinlogInput.Path}'" />
                        <_EditorConfigBinlogInput Include="@(EmbedInBinlog)" Condition="'%(Identity)' == '{editorConfig.Path}'" />
                        <_NoWarnCommandLineArgs Include="@(CscCommandLineArgs)" Condition="$([System.String]::Copy('%(Identity)').StartsWith('/nowarn'))" />
                    </ItemGroup>
                    <Error Condition="$([System.String]::Copy(';$(NoWarn);').Contains(';8002;')) != 'true'" Text="NoWarn was not marshaled." />
                    <Error Condition="'$(AppConfigForCompiler)' != '{appConfig.Path}'" Text="AppConfigForCompiler was not marshaled." />
                    <Error Condition="'@(CscCommandLineArgs->Count())' == '0'" Text="No CscCommandLineArgs were available in the outer scope." />
                    <Error Condition="'@(_ExistingBinlogInput->Count())' != '1'" Text="Existing EmbedInBinlog input was not preserved in the outer scope." />
                    <Error Condition="'@(_EditorConfigBinlogInput->Count())' != '1'" Text="EditorConfig EmbedInBinlog input was not preserved in the outer scope." />
                    <Error Condition="'@(_CoreCompileResourceInputs->Count())' != '0'" Text="_CoreCompileResourceInputs was not cleared." />
                    <Error Condition="'@(_NoWarnCommandLineArgs->Count())' != '1'" Text="Expected exactly one /nowarn switch, but found @(_NoWarnCommandLineArgs->Count()): @(_NoWarnCommandLineArgs, ' ')" />
                </Target>
            </Project>
            """);

        ProjectDir.CreateFile("c.cs").WriteAllText("class C { }");
        RunBuild(projectFile.Path, succeeds: true);

        ArtifactUploadUtil.SetSucceeded();
    }

    [ConditionalTheory(typeof(DotNetSdkAvailable))]
    [InlineData(true, true, new[] { "VerifyTriggeredByCompilation", "VerifyTriggeredAfterCSharpCompilation", "VerifyOuterCompilerState" })]
    [InlineData(true, false, new[] { "VerifyTriggeredByCompilation", "VerifyTriggeredAfterCSharpCompilation" })]
    [InlineData(false, false, new[] { "VerifyTriggeredByCompilation" })]
    public void CoreCompile_TargetsTriggeredByCSharpCompilation_ContinueOnError(
        bool triggeredByCompilationContinueOnError,
        bool triggeredAfterCompilationContinueOnError,
        string[] expectedErrors)
    {
        var projectFile = ProjectDir.CreateFile("console.csproj");
        projectFile.WriteAllText($"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>{NetCoreTfm}</TargetFramework>
                    <TargetsTriggeredByCompilation>VerifyTriggeredByCompilation</TargetsTriggeredByCompilation>
                    <TargetsTriggeredByCSharpCompilationContinueOnError>{triggeredByCompilationContinueOnError}</TargetsTriggeredByCSharpCompilationContinueOnError>
                    <TargetsTriggeredAfterCSharpCompilation>VerifyTriggeredAfterCSharpCompilation</TargetsTriggeredAfterCSharpCompilation>
                    <TargetsTriggeredAfterCSharpCompilationContinueOnError>{triggeredAfterCompilationContinueOnError}</TargetsTriggeredAfterCSharpCompilationContinueOnError>
                </PropertyGroup>
                <Target Name="VerifyTriggeredByCompilation">
                    <Error Text="VerifyTriggeredByCompilation" />
                </Target>
                <Target Name="VerifyTriggeredAfterCSharpCompilation">
                    <Error Text="VerifyTriggeredAfterCSharpCompilation" />
                </Target>
                <Target Name="VerifyOuterCompilerState" AfterTargets="CoreCompile">
                    <Error Text="VerifyOuterCompilerState" />
                </Target>
            </Project>
            """);

        ProjectDir.CreateFile("c.cs").WriteAllText("class C { }");
        var binlogPath = RunBuild(projectFile.Path, succeeds: false);
        var build = BinaryLog.ReadBuild(binlogPath);
        var errors = build.FindChildrenRecursive<Error>(static _ => true);

        Assert.Equal(expectedErrors.Length, errors.Count);
        foreach (var expectedError in expectedErrors)
        {
            Assert.Contains(errors, error => error.Text == expectedError);
        }

        ArtifactUploadUtil.SetSucceeded();
    }
}
