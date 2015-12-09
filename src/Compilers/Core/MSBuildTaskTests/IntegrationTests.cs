// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Win32;
using Roslyn.Test.Utilities;
using Xunit;
using System.Xml;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public class IntegrationTests : TestBase
    {
        private static readonly string s_msbuildDirectory;
        private static readonly string s_msbuildExecutable;

        static IntegrationTests()
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\14.0", writable: false);

            if (key != null)
            {
                var toolsPath = key.GetValue("MSBuildToolsPath");
                if (toolsPath != null)
                {
                    s_msbuildDirectory = toolsPath.ToString();
                    s_msbuildExecutable = Path.Combine(s_msbuildDirectory, "MSBuild.exe");
                }
            }
        }

        private readonly TempDirectory _tempDirectory;
        private readonly List<Process> _existingServerList = new List<Process>();
        private readonly string _buildTaskDll;

        public IntegrationTests()
        {
            if (s_msbuildDirectory == null)
            {
                throw new InvalidOperationException("Could not locate MSBuild");
            }

            _tempDirectory = Temp.CreateDirectory();
            _existingServerList = Process.GetProcessesByName(Path.GetFileNameWithoutExtension("VBCSCompiler")).ToList();
            _buildTaskDll = typeof(ManagedCompiler).Assembly.Location;
        }

        private IEnumerable<KeyValuePair<string, string>> AddForLoggingEnvironmentVars(IEnumerable<KeyValuePair<string, string>> vars)
        {
            vars = vars ?? new KeyValuePair<string, string>[] { };
            if (!vars.Where(kvp => kvp.Key == "RoslynCommandLineLogFile").Any())
            {
                var list = vars.ToList();
                list.Add(new KeyValuePair<string, string>(
                    "RoslynCommandLineLogFile",
                    typeof(IntegrationTests).Assembly.Location + ".client-server.log"));
                return list;
            }
            return vars;
        }

        private ProcessResult RunCommandLineCompiler(
            string compilerPath,
            string arguments,
            string currentDirectory,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVars = null)
        {
            return ProcessUtilities.Run(
                compilerPath,
                arguments,
                currentDirectory,
                additionalEnvironmentVars: AddForLoggingEnvironmentVars(additionalEnvironmentVars));
        }

        private ProcessResult RunCommandLineCompiler(
            string compilerPath,
            string arguments,
            TempDirectory currentDirectory,
            IEnumerable<KeyValuePair<string, string>> filesInDirectory,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVars = null)
        {
            foreach (var pair in filesInDirectory)
            {
                TempFile file = currentDirectory.CreateFile(pair.Key);
                file.WriteAllText(pair.Value);
            }

            return RunCommandLineCompiler(
                compilerPath,
                arguments,
                currentDirectory.Path,
                additionalEnvironmentVars: AddForLoggingEnvironmentVars(additionalEnvironmentVars));
        }

        private DisposableFile GetResultFile(TempDirectory directory, string resultFileName)
        {
            return new DisposableFile(Path.Combine(directory.Path, resultFileName));
        }

        private ProcessResult RunCompilerOutput(TempFile file)
        {
            return ProcessUtilities.Run(file.Path, "", Path.GetDirectoryName(file.Path));
        }

        private static void VerifyResult(ProcessResult result)
        {
            Assert.Equal("", result.Output);
            Assert.Equal("", result.Errors);
            Assert.Equal(0, result.ExitCode);
        }

        private void VerifyResultAndOutput(ProcessResult result, TempDirectory path, string expectedOutput)
        {
            using (var resultFile = GetResultFile(path, "hello.exe"))
            {
                VerifyResult(result);

                var runningResult = RunCompilerOutput(resultFile);
                Assert.Equal(expectedOutput, runningResult.Output);
            }
        }

        // A dictionary with name and contents of all the files we want to create for the SimpleMSBuild test.
        private Dictionary<string, string> SimpleMsBuildFiles => new Dictionary<string, string> {
{ "HelloSolution.sln",
@"
Microsoft Visual Studio Solution File, Format Version 11.00
# Visual Studio 2010
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""HelloProj"", ""HelloProj.csproj"", ""{7F4CCBA2-1184-468A-BF3D-30792E4E8003}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""HelloLib"", ""HelloLib.csproj"", ""{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}""
EndProject
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") = ""VBLib"", ""VBLib.vbproj"", ""{F21C894B-28E5-4212-8AF7-C8E0E5455737}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Debug|Mixed Platforms = Debug|Mixed Platforms
		Debug|x86 = Debug|x86
		Release|Any CPU = Release|Any CPU
		Release|Mixed Platforms = Release|Mixed Platforms
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Debug|Any CPU.ActiveCfg = Debug|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Debug|Mixed Platforms.ActiveCfg = Debug|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Debug|Mixed Platforms.Build.0 = Debug|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Debug|x86.ActiveCfg = Debug|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Debug|x86.Build.0 = Debug|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Release|Any CPU.ActiveCfg = Release|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Release|Mixed Platforms.ActiveCfg = Release|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Release|Mixed Platforms.Build.0 = Release|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Release|x86.ActiveCfg = Release|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Release|x86.Build.0 = Release|x86
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Mixed Platforms.ActiveCfg = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Mixed Platforms.Build.0 = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|x86.ActiveCfg = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Any CPU.Build.0 = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Mixed Platforms.ActiveCfg = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Mixed Platforms.Build.0 = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|x86.ActiveCfg = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Mixed Platforms.ActiveCfg = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Mixed Platforms.Build.0 = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|x86.ActiveCfg = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Any CPU.Build.0 = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Mixed Platforms.ActiveCfg = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Mixed Platforms.Build.0 = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|x86.ActiveCfg = Release|Any CPU	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
"},

{ "HelloProj.csproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Csc"" AssemblyFile=""" + _buildTaskDll + @""" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">x86</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{7F4CCBA2-1184-468A-BF3D-30792E4E8003}</ProjectGuid>
    <OutputType>Exe</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>HelloProj</RootNamespace>
    <AssemblyName>HelloProj</AssemblyName>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
    <TargetFrameworkProfile>Client</TargetFrameworkProfile>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|x86' "">
    <PlatformTarget>x86</PlatformTarget>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|x86' "">
    <PlatformTarget>x86</PlatformTarget>
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Xml"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""Program.cs"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""HelloLib.csproj"">
      <Project>{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}</Project>
      <Name>HelloLib</Name>
    </ProjectReference>
    <ProjectReference Include=""VBLib.vbproj"">
      <Project>{F21C894B-28E5-4212-8AF7-C8E0E5455737}</Project>
      <Name>VBLib</Name>
    </ProjectReference>
  </ItemGroup>
  <ItemGroup>
    <Folder Include=""Properties\"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>"},

{ "Program.cs",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HelloLib;
using VBLib;

namespace HelloProj
{
    class Program
    {
        static void Main(string[] args)
        {
            HelloLibClass.SayHello();
            VBLibClass.SayThere();
            Console.WriteLine(""World"");
        }
    }
}
"},

{ "HelloLib.csproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Csc"" AssemblyFile=""" + _buildTaskDll + @""" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>HelloLib</RootNamespace>
    <AssemblyName>HelloLib</AssemblyName>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Xml"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""HelloLib.cs"" />
  </ItemGroup>
  <ItemGroup>
    <Folder Include=""Properties\"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>"},

{ "HelloLib.cs",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HelloLib
{
    public class HelloLibClass
    {
        public static void SayHello()
        {
            Console.WriteLine(""Hello"");
        }
    }
}
"},

 { "VBLib.vbproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Vbc"" AssemblyFile=""" + _buildTaskDll + @""" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>
    </ProductVersion>
    <SchemaVersion>
    </SchemaVersion>
    <ProjectGuid>{F21C894B-28E5-4212-8AF7-C8E0E5455737}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>VBLib</RootNamespace>
    <AssemblyName>VBLib</AssemblyName>
    <FileAlignment>512</FileAlignment>
    <MyType>Windows</MyType>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <DefineDebug>true</DefineDebug>
    <DefineTrace>true</DefineTrace>
    <OutputPath>bin\Debug\</OutputPath>
    <DocumentationFile>VBLib.xml</DocumentationFile>
    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <DefineDebug>false</DefineDebug>
    <DefineTrace>true</DefineTrace>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DocumentationFile>VBLib.xml</DocumentationFile>
    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
  </PropertyGroup>
  <PropertyGroup>
    <OptionExplicit>On</OptionExplicit>
  </PropertyGroup>
  <PropertyGroup>
    <OptionCompare>Binary</OptionCompare>
  </PropertyGroup>
  <PropertyGroup>
    <OptionStrict>Off</OptionStrict>
  </PropertyGroup>
  <PropertyGroup>
    <OptionInfer>On</OptionInfer>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <ItemGroup>
    <Import Include=""Microsoft.VisualBasic"" />
    <Import Include=""System"" />
    <Import Include=""System.Collections.Generic"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""VBLib.vb"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.VisualBasic.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  -->
</Project>"},

 { "VBLib.vb",
@"
Public Class VBLibClass
    Public Shared Sub SayThere()
        Console.WriteLine(""there"")
    End Sub
End Class
"}
            };

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/1445")]
        public void SimpleMSBuild()
        {
            string arguments = string.Format(@"/m /nr:false /t:Rebuild /p:UseSharedCompilation=false /p:UseRoslyn=1 HelloSolution.sln");
            var result = RunCommandLineCompiler(s_msbuildExecutable, arguments, _tempDirectory, SimpleMsBuildFiles);

            using (var resultFile = GetResultFile(_tempDirectory, @"bin\debug\helloproj.exe"))
            {
                // once we stop issuing BC40998 (NYI), we can start making stronger assertions
                // about our output in the general case
                if (result.ExitCode != 0)
                {
                    Assert.Equal("", result.Output);
                    Assert.Equal("", result.Errors);
                }
                Assert.Equal(0, result.ExitCode);
                var runningResult = RunCompilerOutput(resultFile);
                Assert.Equal("Hello\r\nthere\r\nWorld\r\n", runningResult.Output);
            }
        }



        private Dictionary<string, string> GetMultiFileMSBuildFiles()
        {
            // Return a dictionary with name and contents of all the files we want to create for the SimpleMSBuild test.

            return new Dictionary<string, string> {
{"ConsoleApplication1.sln",
@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2012
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") = ""Mod1"", ""Mod1.vbproj"", ""{DEF6D929-FA03-4076-8A05-7BFA33DCC829}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""assem1"", ""assem1.csproj"", ""{1245560C-55E4-49D7-904C-18281B369763}""
EndProject
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") = ""ConsoleApplication1"", ""ConsoleApplication1.vbproj"", ""{52F3466B-DD3F-435C-ADA6-CD023CC82E91}""
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {1245560C-55E4-49D7-904C-18281B369763}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {1245560C-55E4-49D7-904C-18281B369763}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {1245560C-55E4-49D7-904C-18281B369763}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {1245560C-55E4-49D7-904C-18281B369763}.Release|Any CPU.Build.0 = Release|Any CPU
        {52F3466B-DD3F-435C-ADA6-CD023CC82E91}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {52F3466B-DD3F-435C-ADA6-CD023CC82E91}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {52F3466B-DD3F-435C-ADA6-CD023CC82E91}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {52F3466B-DD3F-435C-ADA6-CD023CC82E91}.Release|Any CPU.Build.0 = Release|Any CPU
        {DEF6D929-FA03-4076-8A05-7BFA33DCC829}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {DEF6D929-FA03-4076-8A05-7BFA33DCC829}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {DEF6D929-FA03-4076-8A05-7BFA33DCC829}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {DEF6D929-FA03-4076-8A05-7BFA33DCC829}.Release|Any CPU.Build.0 = Release|Any CPU
    EndGlobalSection
    GlobalSection(SolutionProperties) = preSolution
        HideSolutionNode = FALSE
    EndGlobalSection
EndGlobal
"},
{"ConsoleApplication1.vbproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{52F3466B-DD3F-435C-ADA6-CD023CC82E91}</ProjectGuid>
    <OutputType>Exe</OutputType>
    <StartupObject>ConsoleApplication1.ConsoleApp</StartupObject>
    <RootNamespace>ConsoleApplication1</RootNamespace>
    <AssemblyName>ConsoleApplication1</AssemblyName>
    <FileAlignment>512</FileAlignment>
    <MyType>Console</MyType>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <DefineDebug>true</DefineDebug>
    <DefineTrace>true</DefineTrace>
    <OutputPath>bin\Debug</OutputPath>
    <DocumentationFile>ConsoleApplication1.xml</DocumentationFile>
    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
  </PropertyGroup>
  <PropertyGroup>
    <OptionExplicit>On</OptionExplicit>
  </PropertyGroup>
  <PropertyGroup>
    <OptionCompare>Binary</OptionCompare>
  </PropertyGroup>
  <PropertyGroup>
    <OptionStrict>Off</OptionStrict>
  </PropertyGroup>
  <PropertyGroup>
    <OptionInfer>On</OptionInfer>
  </PropertyGroup>
  <ItemGroup>
    <Import Include=""Microsoft.VisualBasic"" />
    <Import Include=""System"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""ConsoleApp.vb"" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include=""obj\debug\assem1.dll"">
    </Reference>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.VisualBasic.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>
"},
{"ConsoleApp.vb",
@"
Module ConsoleApp
    Sub Main()
        Console.WriteLine(""Hello"")
        Console.WriteLine(AssemClass.GetNames())
        Console.WriteLine(ModClass2.Name)
    End Sub
End Module
"},
{"assem1.csproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{1245560C-55E4-49D7-904C-18281B369763}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>assem1</RootNamespace>
    <AssemblyName>assem1</AssemblyName>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Assem1.cs"" />
  </ItemGroup>
  <ItemGroup>
    <AddModules Include=""obj\Debug\Mod1.netmodule"">
    </AddModules>
  </ItemGroup>
  <ItemGroup>
    <Folder Include=""Properties\"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>
"},
{"Assem1.cs",
@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AssemClass
{
    public static string Name = ""AssemClass"";

    public static string GetNames()
    {
        return Name + "" "" + ModClass.Name;
    }
}
"},
{"Mod1.vbproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{DEF6D929-FA03-4076-8A05-7BFA33DCC829}</ProjectGuid>
    <OutputType>Module</OutputType>
    <RootNamespace>
    </RootNamespace>
    <AssemblyName>Mod1</AssemblyName>
    <FileAlignment>512</FileAlignment>
    <MyType>Windows</MyType>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <DefineDebug>true</DefineDebug>
    <DefineTrace>true</DefineTrace>
    <OutputPath>bin\Debug\</OutputPath>
    <DocumentationFile>Mod1.xml</DocumentationFile>
    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
  </PropertyGroup>
  <PropertyGroup>
    <OptionExplicit>On</OptionExplicit>
  </PropertyGroup>
  <PropertyGroup>
    <OptionCompare>Binary</OptionCompare>
  </PropertyGroup>
  <PropertyGroup>
    <OptionStrict>Off</OptionStrict>
  </PropertyGroup>
  <PropertyGroup>
    <OptionInfer>On</OptionInfer>
  </PropertyGroup>
  <ItemGroup>
    <Import Include=""Microsoft.VisualBasic"" />
    <Import Include=""System"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""Mod1.vb"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.VisualBasic.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>
"},
{"Mod1.vb",
@"
Friend Class ModClass
    Public Shared Name As String = ""ModClass""
End Class

Public Class ModClass2
    Public Shared Name As String = ""ModClass2""
End Class
"}
            };
        }

        // A dictionary with name and contents of all the files we want to create for the ReportAnalyzerMSBuild test.
        private Dictionary<string, string> ReportAnalyzerMsBuildFiles => new Dictionary<string, string> {
{ "HelloSolution.sln",
@"
Microsoft Visual Studio Solution File, Format Version 11.00
# Visual Studio 2010
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""HelloLib"", ""HelloLib.csproj"", ""{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}""
EndProject
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") = ""VBLib"", ""VBLib.vbproj"", ""{F21C894B-28E5-4212-8AF7-C8E0E5455737}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Debug|Mixed Platforms = Debug|Mixed Platforms
		Debug|x86 = Debug|x86
		Release|Any CPU = Release|Any CPU
		Release|Mixed Platforms = Release|Mixed Platforms
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Mixed Platforms.ActiveCfg = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Mixed Platforms.Build.0 = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|x86.ActiveCfg = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Any CPU.Build.0 = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Mixed Platforms.ActiveCfg = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Mixed Platforms.Build.0 = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|x86.ActiveCfg = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Mixed Platforms.ActiveCfg = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Mixed Platforms.Build.0 = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|x86.ActiveCfg = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Any CPU.Build.0 = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Mixed Platforms.ActiveCfg = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Mixed Platforms.Build.0 = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|x86.ActiveCfg = Release|Any CPU	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
"},
{ "HelloLib.csproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Csc"" AssemblyFile=""" + _buildTaskDll + @""" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>HelloLib</RootNamespace>
    <AssemblyName>HelloLib</AssemblyName>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup>
    <ReportAnalyzer>True</ReportAnalyzer>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Xml"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""HelloLib.cs"" />
  </ItemGroup>
  <ItemGroup>
    <Folder Include=""Properties\"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
  <Import Project=""$(MyMSBuildToolsPath)\Microsoft.CSharp.Core.targets"" />
</Project>"},

{ "HelloLib.cs",
@"public class $P {}"},

 { "VBLib.vbproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Vbc"" AssemblyFile=""" + _buildTaskDll + @""" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>
    </ProductVersion>
    <SchemaVersion>
    </SchemaVersion>
    <ProjectGuid>{F21C894B-28E5-4212-8AF7-C8E0E5455737}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>VBLib</RootNamespace>
    <AssemblyName>VBLib</AssemblyName>
    <FileAlignment>512</FileAlignment>
    <MyType>Windows</MyType>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <DefineDebug>true</DefineDebug>
    <DefineTrace>true</DefineTrace>
    <OutputPath>bin\Debug\</OutputPath>
    <DocumentationFile>VBLib.xml</DocumentationFile>
    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <DefineDebug>false</DefineDebug>
    <DefineTrace>true</DefineTrace>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DocumentationFile>VBLib.xml</DocumentationFile>
    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
  </PropertyGroup>
  <PropertyGroup>
    <OptionExplicit>On</OptionExplicit>
  </PropertyGroup>
  <PropertyGroup>
    <OptionCompare>Binary</OptionCompare>
  </PropertyGroup>
  <PropertyGroup>
    <OptionStrict>Off</OptionStrict>
  </PropertyGroup>
  <PropertyGroup>
    <OptionInfer>On</OptionInfer>
  </PropertyGroup>
  <PropertyGroup>
    <ReportAnalyzer>True</ReportAnalyzer>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <ItemGroup>
    <Import Include=""Microsoft.VisualBasic"" />
    <Import Include=""System"" />
    <Import Include=""System.Collections.Generic"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""VBLib.vb"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.VisualBasic.targets"" />
  <Import Project=""$(MyMSBuildToolsPath)\Microsoft.VisualBasic.Core.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  -->
</Project>"},

 { "VBLib.vb",
@"
Public Class $P
End Class
"}
            };

        [Fact]
        public void ReportAnalyzerMSBuild()
        {
            string arguments = string.Format(@"/m /nr:false /t:Rebuild /p:UseSharedCompilation=false /p:UseRoslyn=1 HelloSolution.sln");
            var result = RunCommandLineCompiler(s_msbuildExecutable, arguments, _tempDirectory, ReportAnalyzerMsBuildFiles,
                new Dictionary<string, string>
                { { "MyMSBuildToolsPath", Path.GetDirectoryName(typeof(IntegrationTests).Assembly.Location) } });

            Assert.True(result.ExitCode != 0);
            Assert.Contains("/reportanalyzer", result.Output);
        }

        [Fact(Skip = "failing msbuild")]
        public void SolutionWithPunctuation()
        {
            var testDir = _tempDirectory.CreateDirectory(@"SLN;!@(foo)'^1");
            var slnFile = testDir.CreateFile("Console;!@(foo)'^(Application1.sln").WriteAllText(
    @"
Microsoft Visual Studio Solution File, Format Version 10.00
# Visual Studio 2005
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Cons.ole;!@(foo)'^(Application1"", ""Console;!@(foo)'^(Application1\Cons.ole;!@(foo)'^(Application1.csproj"", ""{770F2381-8C39-49E9-8C96-0538FA4349A7}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Class;!@(foo)'^(Library1"", ""Class;!@(foo)'^(Library1\Class;!@(foo)'^(Library1.csproj"", ""{0B4B78CC-C752-43C2-BE9A-319D20216129}""
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {770F2381-8C39-49E9-8C96-0538FA4349A7}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {770F2381-8C39-49E9-8C96-0538FA4349A7}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {770F2381-8C39-49E9-8C96-0538FA4349A7}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {770F2381-8C39-49E9-8C96-0538FA4349A7}.Release|Any CPU.Build.0 = Release|Any CPU
        {0B4B78CC-C752-43C2-BE9A-319D20216129}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {0B4B78CC-C752-43C2-BE9A-319D20216129}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {0B4B78CC-C752-43C2-BE9A-319D20216129}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {0B4B78CC-C752-43C2-BE9A-319D20216129}.Release|Any CPU.Build.0 = Release|Any CPU
    EndGlobalSection
    GlobalSection(SolutionProperties) = preSolution
        HideSolutionNode = FALSE
    EndGlobalSection
EndGlobal
");
            var appDir = testDir.CreateDirectory(@"Console;!@(foo)'^(Application1");
            var appProjFile = appDir.CreateFile(@"Cons.ole;!@(foo)'^(Application1.csproj").WriteAllText(
    @"
<Project DefaultTargets=""Build"" ToolsVersion=""3.5"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Csc"" AssemblyFile=""" + _buildTaskDll + @""" />
    <PropertyGroup>
        <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
        <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
        <ProductVersion>8.0.50510</ProductVersion>
        <SchemaVersion>2.0</SchemaVersion>
        <ProjectGuid>{770F2381-8C39-49E9-8C96-0538FA4349A7}</ProjectGuid>
        <OutputType>Exe</OutputType>
        <AppDesignerFolder>Properties</AppDesignerFolder>
        <RootNamespace>Console____foo____Application1</RootNamespace>
        <AssemblyName>Console%3b!%40%28foo%29%27^%28Application1</AssemblyName>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
        <DebugSymbols>true</DebugSymbols>
        <DebugType>full</DebugType>
        <Optimize>false</Optimize>
        <OutputPath>bin\Debug\</OutputPath>
        <DefineConstants>DEBUG;TRACE</DefineConstants>
        <ErrorReport>prompt</ErrorReport>
        <WarningLevel>4</WarningLevel>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
        <DebugType>pdbonly</DebugType>
        <Optimize>true</Optimize>
        <OutputPath>bin\Release\</OutputPath>
        <DefineConstants>TRACE</DefineConstants>
        <ErrorReport>prompt</ErrorReport>
        <WarningLevel>4</WarningLevel>
    </PropertyGroup>
    <ItemGroup>
        <Reference Include=""System"" />
        <Reference Include=""System.Data"" />
        <Reference Include=""System.Xml"" />
    </ItemGroup>
    <ItemGroup>
        <Compile Include=""Program.cs"" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include=""..\Class%3b!%40%28foo%29%27^%28Library1\Class%3b!%40%28foo%29%27^%28Library1.csproj"">
            <Project>{0B4B78CC-C752-43C2-BE9A-319D20216129}</Project>
            <Name>Class%3b!%40%28foo%29%27^%28Library1</Name>
        </ProjectReference>
    </ItemGroup>
    <Import Project=""$(MSBuildBinPath)\Microsoft.CSharp.targets"" />
</Project>
");

            var appProgramFile = appDir.CreateFile("Program.cs").WriteAllText(
    @"
using System;
using System.Collections.Generic;
using System.Text;

namespace Console____foo____Application1
{
    class Program
    {
        static void Main(string[] args)
        {
            Class____foo____Library1.Class1 foo = new Class____foo____Library1.Class1();
        }
    }
}");

            var libraryDir = testDir.CreateDirectory(@"Class;!@(foo)'^(Library1");
            var libraryProjFile = libraryDir.CreateFile("Class;!@(foo)'^(Library1.csproj").WriteAllText(
    @"
<Project DefaultTargets=""Build"" ToolsVersion=""3.5"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Csc"" AssemblyFile=""" + _buildTaskDll + @""" />
    <PropertyGroup>
        <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
        <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
        <ProductVersion>8.0.50510</ProductVersion>
        <SchemaVersion>2.0</SchemaVersion>
        <ProjectGuid>{0B4B78CC-C752-43C2-BE9A-319D20216129}</ProjectGuid>
        <OutputType>Library</OutputType>
        <AppDesignerFolder>Properties</AppDesignerFolder>
        <RootNamespace>Class____foo____Library1</RootNamespace>
        <AssemblyName>Class%3b!%40%28foo%29%27^%28Library1</AssemblyName>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
        <DebugSymbols>true</DebugSymbols>
        <DebugType>full</DebugType>
        <Optimize>false</Optimize>
        <OutputPath>bin\Debug\</OutputPath>
        <DefineConstants>DEBUG;TRACE</DefineConstants>
        <ErrorReport>prompt</ErrorReport>
        <WarningLevel>4</WarningLevel>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
        <DebugType>pdbonly</DebugType>
        <Optimize>true</Optimize>
        <OutputPath>bin\Release\</OutputPath>
        <DefineConstants>TRACE</DefineConstants>
        <ErrorReport>prompt</ErrorReport>
        <WarningLevel>4</WarningLevel>
    </PropertyGroup>
    <ItemGroup>
        <Reference Include=""System"" />
        <Reference Include=""System.Data"" />
        <Reference Include=""System.Xml"" />
    </ItemGroup>
    <ItemGroup>
        <Compile Include=""Class1.cs"" />
    </ItemGroup>
    <Import Project=""$(MSBuildBinPath)\Microsoft.CSharp.targets"" />

    <!-- The old OM, which is what this solution is being built under, doesn't understand
         BeforeTargets, so this test was failing, because _AssignManagedMetadata was set 
         up as a BeforeTarget for Build.  Copied here so that build will return the correct
         information again. -->
    <Target Name=""BeforeBuild"">
        <ItemGroup>
            <BuiltTargetPath Include=""$(TargetPath)"">
                <ManagedAssembly>$(ManagedAssembly)</ManagedAssembly>
            </BuiltTargetPath>
        </ItemGroup>
    </Target>
</Project>
");

            var libraryClassFile = libraryDir.CreateFile("Class1.cs").WriteAllText(
    @"
namespace Class____foo____Library1
{
    public class Class1
    {
    }
}
");

            var result = RunCommandLineCompiler(s_msbuildExecutable, "/p:UseSharedCompilation=false", testDir.Path);
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("", result.Errors);
        }
    }
}
