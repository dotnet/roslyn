// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace CompilerBenchmarks
{
    public class FixedCsProjGenerator : CsProjGenerator
    {
        private const string Template = @"
<Project ToolsVersion=""15.0"">
  <PropertyGroup>
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
  </PropertyGroup>

  <Import Project=""Sdk.props"" Sdk=""Microsoft.Net.Sdk"" />

  <PropertyGroup>
    <AssemblyTitle>$PROGRAMNAME$</AssemblyTitle>
    <TargetFramework>$TFM$</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <PlatformTarget>$PLATFORM$</PlatformTarget>
    <AssemblyName>$PROGRAMNAME$</AssemblyName>
    <OutputType>Exe</OutputType>
    <OutputPath>bin\$CONFIGURATIONNAME$</OutputPath>
    <TreatWarningsAsErrors>False</TreatWarningsAsErrors>
    <DebugType>pdbonly</DebugType>
    <DebugSymbols>true</DebugSymbols>
    $COPIEDSETTINGS$
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""$CODEFILENAME$"" Exclude=""bin\**;obj\**;**\*.xproj;packages\**"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""$CSPROJPATH$"" />
  </ItemGroup>
  $RUNTIMESETTINGS$

  <Import Project=""Sdk.targets"" Sdk=""Microsoft.Net.Sdk"" />
</Project>";

        public FixedCsProjGenerator(string targetFrameworkMoniker, Func<Platform, string> platformProvider, string runtimeFrameworkVersion = null)
            : base(targetFrameworkMoniker, platformProvider, runtimeFrameworkVersion)
        { }

        protected override string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition, string programName)
        {
            string directoryName = Path.GetDirectoryName(buildPartition.AssemblyLocation)
                ?? throw new DirectoryNotFoundException(buildPartition.AssemblyLocation);
            return Path.Combine(directoryName, programName);
        }

        public new static string PlatformProvider(Platform p) => p.ToString();

        public static IToolchain Default => new Toolchain(
            "FixedCsProjToolchain",
            new FixedCsProjGenerator("netcoreapp2.0", PlatformProvider),
            new DotNetCliBuilder("netcoreapp2.0"),
            new DotNetCliExecutor(null));

        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            string template = Template;
            var benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

            string platform = PlatformProvider(buildPartition.Platform);
            string content = SetPlatform(template, platform);
            content = SetCodeFileName(content, Path.GetFileName(artifactsPaths.ProgramCodePath));
            content = content.Replace("$CSPROJPATH$", projectFile.FullName);
            content = SetTargetFrameworkMoniker(content, TargetFrameworkMoniker);
            content = content.Replace("$PROGRAMNAME$", artifactsPaths.ProgramName);
            content = content.Replace("$RUNTIMESETTINGS$", GetRuntimeSettings(benchmark.Job.Environment.Gc, buildPartition.Resolver));
            content = content.Replace("$COPIEDSETTINGS$", GetSettingsThatNeedsToBeCopied(projectFile));
            content = content.Replace("$CONFIGURATIONNAME$", buildPartition.BuildConfiguration);

            File.WriteAllText(artifactsPaths.ProjectFilePath, content);
        }

        // the host project or one of the .props file that it imports might contain some custom settings that needs to be copied, sth like
        // <NetCoreAppImplicitPackageVersion>2.0.0-beta-001607-00</NetCoreAppImplicitPackageVersion>
        // <RuntimeFrameworkVersion>2.0.0-beta-001607-00</RuntimeFrameworkVersion>
        private string GetSettingsThatNeedsToBeCopied(FileInfo projectFile)
        {
            if (!string.IsNullOrEmpty(RuntimeFrameworkVersion)) // some power users knows what to configure, just do it and copy nothing more
                return $"<RuntimeFrameworkVersion>{RuntimeFrameworkVersion}</RuntimeFrameworkVersion>";

            var customSettings = new StringBuilder();
            using (var file = new StreamReader(File.OpenRead(projectFile.FullName)))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains("NetCoreAppImplicitPackageVersion") || line.Contains("RuntimeFrameworkVersion") || line.Contains("PackageTargetFallback") || line.Contains("LangVersion"))
                    {
                        customSettings.Append(line);
                    }
                    else if (line.Contains("<Import Project"))
                    {
                        string propsFilePath = line.Trim().Split('"')[1]; // its sth like   <Import Project="..\..\build\common.props" />
                        var directoryName = projectFile.DirectoryName ?? throw new DirectoryNotFoundException(projectFile.DirectoryName);
                        string absolutePath = File.Exists(propsFilePath)
                            ? propsFilePath // absolute path or relative to current dir
                            : Path.Combine(directoryName, propsFilePath); // relative to csproj

                        if (File.Exists(absolutePath))
                        {
                            customSettings.Append(GetSettingsThatNeedsToBeCopied(new FileInfo(absolutePath)));
                        }
                    }
                }
            }

            return customSettings.ToString();
        }
    }
}
