// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET472

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests;

public abstract class IntegrationTestBase : TestBase
{
    protected static readonly string? s_msbuildDirectory;

    static IntegrationTestBase()
    {
        s_msbuildDirectory = DesktopTestHelpers.GetMSBuildDirectory();
    }

    protected readonly ITestOutputHelper _output;
    protected readonly string _msbuildExecutable;
    protected readonly TempDirectory _tempDirectory;
    protected string _buildTaskDll;

    protected IntegrationTestBase(ITestOutputHelper output)
    {
        if (s_msbuildDirectory == null)
        {
            throw new InvalidOperationException("Could not locate MSBuild");
        }

        _output = output;
        _msbuildExecutable = Path.Combine(s_msbuildDirectory, "MSBuild.exe");
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79907")]
    public void StdLib()
    {
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
                                <Csc Sources="File.cs" />
                            </Target>
                        </Project>
                        """ },
            });
        _output.WriteLine(result.Output);
        Assert.Equal(0, result.ExitCode);
    }
}

#endif
