// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public abstract class IntegrationTestBase : TestBase
{
    protected static readonly string? s_msbuildDirectory;

#if NET472
    static IntegrationTestBase()
    {
        s_msbuildDirectory = DesktopTestHelpers.GetMSBuildDirectory();
    }
#endif

    protected readonly ITestOutputHelper _output;
    protected readonly string? _msbuildExecutable;
    protected readonly TempDirectory _tempDirectory;
    protected string _buildTaskDll;

    protected IntegrationTestBase(ITestOutputHelper output)
    {
        if (s_msbuildDirectory == null)
        {
            output.WriteLine("Could not locate MSBuild");
        }

        _output = output;
        _msbuildExecutable = s_msbuildDirectory == null ? null : Path.Combine(s_msbuildDirectory, "MSBuild.exe");
        _tempDirectory = Temp.CreateDirectory();
        _buildTaskDll = typeof(ManagedCompiler).Assembly.Location;
    }

    private static IEnumerable<KeyValuePair<string, string>> AddForLoggingEnvironmentVars(IEnumerable<KeyValuePair<string, string>>? vars)
    {
        vars = vars ?? new KeyValuePair<string, string>[] { };
        if (!vars.Where(kvp => kvp.Key == "RoslynCommandLineLogFile").Any())
        {
            var list = vars.ToList();
            list.Add(new KeyValuePair<string, string>(
                "RoslynCommandLineLogFile",
                typeof(IntegrationTestBase).Assembly.Location + ".client-server.log"));
            return list;
        }
        return vars;
    }

    protected static ProcessResult RunCommandLineCompiler(
        string compilerPath,
        string arguments,
        string currentDirectory,
        IEnumerable<KeyValuePair<string, string>>? additionalEnvironmentVars = null)
    {
        return ProcessUtilities.Run(
            compilerPath,
            arguments,
            currentDirectory,
            additionalEnvironmentVars: AddForLoggingEnvironmentVars(additionalEnvironmentVars));
    }

    protected static ProcessResult RunCommandLineCompiler(
        string compilerPath,
        string arguments,
        TempDirectory currentDirectory,
        IEnumerable<KeyValuePair<string, string>> filesInDirectory,
        IEnumerable<KeyValuePair<string, string>>? additionalEnvironmentVars = null)
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

    protected ProcessResult? RunMsbuild(
        string arguments,
        TempDirectory currentDirectory,
        IEnumerable<KeyValuePair<string, string>> filesInDirectory,
        IEnumerable<KeyValuePair<string, string>>? additionalEnvironmentVars = null)
    {
        if (_msbuildExecutable != null)
        {
            return RunCommandLineCompiler(
                _msbuildExecutable,
                arguments,
                currentDirectory,
                filesInDirectory,
                additionalEnvironmentVars);
        }

        if (ExecutionConditionUtil.IsDesktop)
        {
            _output.WriteLine("Skipping because Framework MSBuild is missing, this is a desktop test, " +
                "and we cannot use the desktop Csc/Vbc task from 'dotnet msbuild', i.e., Core MSBuild.");
            return null;
        }

        return RunCommandLineCompiler(
            "dotnet",
            $"msbuild {arguments}",
            currentDirectory,
            filesInDirectory,
            additionalEnvironmentVars);
    }

    /// <param name="overrideToolExe">
    /// Setting ToolExe to "csc.exe" should use the built-in compiler regardless of apphost being used or not.
    /// </param>
    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/80991"), CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2615118")]
    public void SdkBuild_Csc(bool useSharedCompilation, bool overrideToolExe, bool useAppHost)
    {
        if (!ManagedToolTask.IsBuiltinToolRunningOnCoreClr && !useAppHost)
        {
            _output.WriteLine("Skipping test case: netfx compiler always uses apphost.");
            return;
        }

        var originalAppHost = Path.Combine(ManagedToolTask.GetToolDirectory(), $"csc{PlatformInformation.ExeExtension}");
        var backupAppHost = originalAppHost + ".bak";
        if (!useAppHost)
        {
            _output.WriteLine($"Apphost: {originalAppHost}");
            File.Move(originalAppHost, backupAppHost);
        }

        ProcessResult? result;

        try
        {
            result = RunMsbuild(
                "/v:n /m /nr:false /t:Build /restore Test.csproj" +
                    (overrideToolExe ? $" /p:CscToolExe=csc{PlatformInformation.ExeExtension}" : ""),
                _tempDirectory,
                new Dictionary<string, string>
                {
                    { "File.cs", """
                        class Program { static void Main() { System.Console.WriteLine("Hello from file"); } }
                        """ },
                    { "Test.csproj", $"""
                        <Project Sdk="Microsoft.NET.Sdk">
                            <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Csc" AssemblyFile="{_buildTaskDll}" />
                            <PropertyGroup>
                                <TargetFramework>netstandard2.0</TargetFramework>
                                <UseSharedCompilation>{useSharedCompilation}</UseSharedCompilation>
                            </PropertyGroup>
                        </Project>
                        """ },
                });
        }
        finally
        {
            if (!useAppHost)
            {
                File.Move(backupAppHost, originalAppHost);
            }
        }

        if (result == null) return;

        _output.WriteLine(result.Output);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(useSharedCompilation ? "server processed compilation" : "using command line tool by design", result.Output);

        if (useAppHost)
        {
            Assert.DoesNotContain("csc.dll", result.Output);
            Assert.Contains($"csc{PlatformInformation.ExeExtension} ", result.Output);
        }
        else
        {
            Assert.Contains("csc.dll", result.Output);
            Assert.DoesNotContain($"csc{PlatformInformation.ExeExtension} ", result.Output);
        }
    }

    /// <param name="overrideToolExe">
    /// Setting ToolExe to "vbc.exe" should use the built-in compiler regardless of apphost being used or not.
    /// </param>
    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/80991"), CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2615118")]
    public void SdkBuild_Vbc(bool useSharedCompilation, bool overrideToolExe, bool useAppHost)
    {
        if (!ManagedToolTask.IsBuiltinToolRunningOnCoreClr && !useAppHost)
        {
            _output.WriteLine("Skipping test case: netfx compiler always uses apphost.");
            return;
        }

        var originalAppHost = Path.Combine(ManagedToolTask.GetToolDirectory(), $"vbc{PlatformInformation.ExeExtension}");
        var backupAppHost = originalAppHost + ".bak";
        if (!useAppHost)
        {
            File.Move(originalAppHost, backupAppHost);
        }

        ProcessResult? result;

        try
        {
            result = RunMsbuild(
                "/v:n /m /nr:false /t:Build /restore Test.vbproj" +
                    (overrideToolExe ? $" /p:VbcToolExe=vbc{PlatformInformation.ExeExtension}" : ""),
                _tempDirectory,
                new Dictionary<string, string>
                {
                    { "File.vb", """
                        Public Module Program
                            Public Sub Main()
                                System.Console.WriteLine("Hello from file")
                            End Sub
                        End Module
                        """ },
                    { "Test.vbproj", $"""
                        <Project Sdk="Microsoft.NET.Sdk">
                            <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Vbc" AssemblyFile="{_buildTaskDll}" />
                            <PropertyGroup>
                                <TargetFramework>netstandard2.0</TargetFramework>
                                <UseSharedCompilation>{useSharedCompilation}</UseSharedCompilation>
                            </PropertyGroup>
                        </Project>
                        """ },
                });
        }
        finally
        {
            if (!useAppHost)
            {
                File.Move(backupAppHost, originalAppHost);
            }
        }

        if (result == null) return;

        _output.WriteLine(result.Output);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(useSharedCompilation ? "server processed compilation" : "using command line tool by design", result.Output);

        if (useAppHost)
        {
            Assert.DoesNotContain("vbc.dll", result.Output);
            Assert.Contains($"vbc{PlatformInformation.ExeExtension} ", result.Output);
        }
        else
        {
            Assert.Contains("vbc.dll", result.Output);
            Assert.DoesNotContain($"vbc{PlatformInformation.ExeExtension} ", result.Output);
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public void StdLib_Csc(bool useSharedCompilation, bool disableSdkPath)
    {
        if (_msbuildExecutable == null) return;

        var result = RunCommandLineCompiler(
            _msbuildExecutable,
            "/m /nr:false /t:CustomTarget Test.csproj",
            _tempDirectory,
            new Dictionary<string, string>
            {
                { "File.cs", """
                    System.Console.WriteLine("Hello from file");
                    """ },
                { "Test.csproj", $"""
                    <Project>
                        <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Csc" AssemblyFile="{_buildTaskDll}" />
                        <Target Name="CustomTarget">
                            <Csc Sources="File.cs" UseSharedCompilation="{useSharedCompilation}" DisableSdkPath="{disableSdkPath}" />
                        </Target>
                    </Project>
                    """ },
            });
        _output.WriteLine(result.Output);

        if (disableSdkPath)
        {
            Assert.NotEqual(0, result.ExitCode);
            // Either error CS0006: Metadata file could not be found
            // or error CS0518: Predefined type is not defined or imported
            Assert.Contains("error CS", result.Output);
        }
        else
        {
            Assert.Equal(0, result.ExitCode);
            Assert.Contains(useSharedCompilation ? "server processed compilation" : "using command line tool by design", result.Output);
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public void StdLib_Vbc(bool useSharedCompilation, bool disableSdkPath)
    {
        if (_msbuildExecutable == null) return;

        var result = RunCommandLineCompiler(
            _msbuildExecutable,
            "/m /nr:false /t:CustomTarget Test.vbproj",
            _tempDirectory,
            new Dictionary<string, string>
            {
                { "File.vb", """
                    Public Module Program
                        Public Sub Main()
                            System.Console.WriteLine("Hello from file")
                        End Sub
                    End Module
                    """ },
                { "Test.vbproj", $"""
                    <Project>
                        <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Vbc" AssemblyFile="{_buildTaskDll}" />
                        <Target Name="CustomTarget">
                            <Vbc Sources="File.vb" UseSharedCompilation="{useSharedCompilation}" DisableSdkPath="{disableSdkPath}" />
                        </Target>
                    </Project>
                    """ },
            });
        _output.WriteLine(result.Output);

        if (disableSdkPath)
        {
            Assert.NotEqual(0, result.ExitCode);
            // error BC2017: could not find library 'Microsoft.VisualBasic.dll'
            Assert.Contains("error BC2017", result.Output);
        }
        else
        {
            Assert.Equal(0, result.ExitCode);
            Assert.Contains(useSharedCompilation ? "server processed compilation" : "using command line tool by design", result.Output);
        }
    }
}
