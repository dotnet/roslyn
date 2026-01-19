// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
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

    private static async Task ShutdownCompilerServerAsync(ProcessResult result)
    {
        var pipeName = Regex.Match(result.Output, @"Named pipe '([^']+)' connected").Groups[1].Value;
        using var logger = new CompilerServerLogger("test");
        await BuildServerConnection.RunServerShutdownRequestAsync(
            pipeName,
            timeoutOverride: null,
            waitForProcess: true,
            logger,
            CancellationToken.None);
    }

    /// <param name="overrideToolExe">
    /// Setting ToolExe to "csc.exe" should use the built-in compiler regardless of apphost being used or not.
    /// </param>
    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2615118")]
    public async Task SdkBuild_Csc(bool useSharedCompilation, bool overrideToolExe, bool useAppHost)
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

        if (useSharedCompilation)
        {
            await ShutdownCompilerServerAsync(result);
        }

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
    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2615118")]
    public async Task SdkBuild_Vbc(bool useSharedCompilation, bool overrideToolExe, bool useAppHost)
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

        if (useSharedCompilation)
        {
            await ShutdownCompilerServerAsync(result);
        }

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
    public async Task StdLib_Csc(bool useSharedCompilation, bool disableSdkPath, bool noConfig)
    {
        if (_msbuildExecutable == null) return;

        var result = RunCommandLineCompiler(
            _msbuildExecutable,
            "/m /nr:false /t:CustomTarget Test.csproj",
            _tempDirectory,
            new Dictionary<string, string>
            {
                { "File.cs", """
                    using System.Linq;
                    System.Console.WriteLine("Hello from file");
                    """ },
                { "Test.csproj", $"""
                    <Project>
                        <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Csc" AssemblyFile="{_buildTaskDll}" />
                        <Target Name="CustomTarget">
                            <Csc Sources="File.cs" UseSharedCompilation="{useSharedCompilation}" DisableSdkPath="{disableSdkPath}" NoConfig="{noConfig}" />
                        </Target>
                    </Project>
                    """ },
            });
        _output.WriteLine(result.Output);

        if (useSharedCompilation)
        {
            await ShutdownCompilerServerAsync(result);
        }

        if (disableSdkPath || noConfig)
        {
            Assert.NotEqual(0, result.ExitCode);
            if (disableSdkPath && noConfig)
            {
                // error CS0246: The type or namespace name 'System' could not be found
                Assert.Contains("error CS0246", result.Output);
            }
            else if (disableSdkPath)
            {
                // error CS0006: Metadata file could not be found
                Assert.Contains("error CS0006", result.Output);
            }
            else
            {
                // error CS0234: The type or namespace name 'Linq' does not exist in the namespace 'System'
                Assert.Contains("error CS0234", result.Output);
            }
        }
        else
        {
            Assert.Equal(0, result.ExitCode);
        }

        Assert.Contains(useSharedCompilation ? "server processed compilation" : "using command line tool by design", result.Output);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public async Task StdLib_Vbc(bool useSharedCompilation, bool disableSdkPath, bool noConfig)
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
                            Console.WriteLine("Hello from file")
                        End Sub
                    End Module
                    """ },
                { "Test.vbproj", $"""
                    <Project>
                        <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Vbc" AssemblyFile="{_buildTaskDll}" />
                        <Target Name="CustomTarget">
                            <Vbc Sources="File.vb" UseSharedCompilation="{useSharedCompilation}" DisableSdkPath="{disableSdkPath}" NoConfig="{noConfig}" />
                        </Target>
                    </Project>
                    """ },
            });
        _output.WriteLine(result.Output);

        if (useSharedCompilation)
        {
            await ShutdownCompilerServerAsync(result);
        }

        if (disableSdkPath || noConfig)
        {
            Assert.NotEqual(0, result.ExitCode);
            if (disableSdkPath)
            {
                // error BC2017: could not find library 'Microsoft.VisualBasic.dll'
                Assert.Contains("error BC2017", result.Output);
            }
            else
            {
                Assert.True(noConfig);
                // error BC30451: 'Console' is not declared. It may be inaccessible due to its protection level.
                Assert.Contains("error BC30451", result.Output);
            }
        }
        else
        {
            Assert.Equal(0, result.ExitCode);
        }

        Assert.Contains(useSharedCompilation ? "server processed compilation" : "using command line tool by design", result.Output);
    }

    /// <summary>
    /// Verifies that both RSPs are included: the default <c>csc.rsp</c> (which has <c>/r:System.Data.OracleClient</c>),
    /// and the custom RSP (which has <c>/warnaserror+</c> so we get an error for using an obsolete type).
    /// </summary>
    [Theory, CombinatorialData]
    public async Task CustomRsp_Csc(bool includeCustomRsp, bool useSharedCompilation, bool noConfig)
    {
        if (_msbuildExecutable == null) return;

        var result = RunCommandLineCompiler(
            _msbuildExecutable,
            "/m /nr:false /t:CustomTarget Test.csproj",
            _tempDirectory,
            new Dictionary<string, string>
            {
                { "File.cs", """
                    new System.Data.OracleClient.OracleConnection("");
                    """ },
                { "custom.rsp", """
                    /warnaserror+
                    """ },
                { "Test.csproj", $"""
                    <Project>
                        <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Csc" AssemblyFile="{_buildTaskDll}" />
                        <Target Name="CustomTarget">
                            <Csc Sources="File.cs" UseSharedCompilation="{useSharedCompilation}" ResponseFiles="{(includeCustomRsp ? "custom.rsp" : "")}" NoConfig="{noConfig}" />
                        </Target>
                    </Project>
                    """ },
            });
        _output.WriteLine(result.Output);

        if (useSharedCompilation)
        {
            await ShutdownCompilerServerAsync(result);
        }

        Assert.Equal(!includeCustomRsp && !noConfig, 0 == result.ExitCode);
        if (noConfig)
        {
            // error CS0234: The type or namespace name 'Data' does not exist in the namespace 'System'
            Assert.Contains("error CS0234", result.Output);
        }
        else
        {
            // warning CS0618: The type is obsolete
            Assert.Contains($"{(includeCustomRsp ? "error" : "warning")} CS0618", result.Output);
        }

        Assert.Contains(useSharedCompilation ? "server processed compilation" : "using command line tool by design", result.Output);
    }

    /// <inheritdoc cref="CustomRsp_Csc"/>
    [Theory, CombinatorialData]
    public async Task CustomRsp_Vbc(bool includeCustomRsp, bool useSharedCompilation, bool noConfig)
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
                            Dim x As Object = New System.Data.OracleClient.OracleConnection("")
                        End Sub
                    End Module
                    """ },
                { "custom.rsp", """
                    /warnaserror+
                    """ },
                { "Test.vbproj", $"""
                    <Project>
                        <UsingTask TaskName="Microsoft.CodeAnalysis.BuildTasks.Vbc" AssemblyFile="{_buildTaskDll}" />
                        <Target Name="CustomTarget">
                            <Vbc Sources="File.vb" UseSharedCompilation="{useSharedCompilation}" ResponseFiles="{(includeCustomRsp ? "custom.rsp" : "")}" NoConfig="{noConfig}" />
                        </Target>
                    </Project>
                    """ },
            });
        _output.WriteLine(result.Output);

        if (useSharedCompilation)
        {
            await ShutdownCompilerServerAsync(result);
        }

        Assert.Equal(!includeCustomRsp && !noConfig, 0 == result.ExitCode);
        if (noConfig)
        {
            // error BC30002: Type 'System.Data.OracleClient.OracleConnection' is not defined.
            Assert.Contains("error BC30002", result.Output);
        }
        else
        {
            // warning BC40000: The type is obsolete
            Assert.Contains($"{(includeCustomRsp ? "error" : "warning")} BC40000", result.Output);
        }

        Assert.Contains(useSharedCompilation ? "server processed compilation" : "using command line tool by design", result.Output);
    }
}
