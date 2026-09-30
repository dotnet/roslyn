// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Mono.Options;

namespace TestRunner;

[Flags]
internal enum TestRuntime
{
    Core = 1,
    Framework = 2,
}

internal abstract class CommonOptions
{
    internal static readonly string[] CompilerTestAssemblyPatterns = new[]
    {
        @"^Microsoft\.CodeAnalysis\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.CompilerServer\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.CSharp\.Syntax\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.CSharp\.Symbol\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.CSharp\.Semantic\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.CSharp\.Emit\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.CSharp\.Emit2\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.CSharp\.Emit3\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.CSharp\.CSharp15\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.CSharp\.IOperation\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.CSharp\.CommandLine\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.VisualBasic\.Syntax\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.VisualBasic\.Symbol\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.VisualBasic\.Semantic\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.VisualBasic\.Emit\.UnitTests$",
        @"^Roslyn\.Compilers\.VisualBasic\.IOperation\.UnitTests$",
        @"^Microsoft\.CodeAnalysis\.VisualBasic\.CommandLine\.UnitTests$",
        @"^Microsoft\.Build\.Tasks\.CodeAnalysis\.UnitTests$",
    };

    private readonly List<string> _testFrameworks = new();
    private string? _testSet;
    private string? _testKind;
    private string? _artifactsPath;
    private string? _dotnetFilePath;
    private bool _showHelp;

    public string Configuration { get; set; } = "Debug";
    public TestRuntime TestRuntime { get; set; } = TestRuntime.Core | TestRuntime.Framework;
    public List<string> IncludeFilter { get; } = new();
    public List<string> ExcludeFilter { get; } = new();
    public string ArtifactsDirectory { get; private set; } = "";
    public string DotnetFilePath { get; private set; } = "";
    public string Architecture { get; set; } = Microsoft.CodeAnalysis.Test.Utilities.IlasmUtilities.Architecture;
    public Dictionary<string, string> EnvironmentVariables { get; } = new(
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    protected OptionSet GetOptionSet()
        => new()
        {
            { "h|help|?", "Show this help message and exit", o => _showHelp = o is object },
            { "dotnet=", "Path to dotnet", s => _dotnetFilePath = s },
            { "testConfiguration=", "Configuration to test: Debug or Release", s => Configuration = s },
            { "include=", "Regex for including unit test dlls (can be specified multiple times). Default: .*UnitTests.*", s => IncludeFilter.Add(s) },
            { "exclude=", "Regex for excluding unit test dlls (can be specified multiple times)", s => ExcludeFilter.Add(s) },
            { "testPlatform=", "Architecture to test on: x86, x64 or arm64", s => Architecture = s },
            { "artifactspath=", "Path to the artifacts directory (auto-detected from binary location if not set)", s => _artifactsPath = s },
            { "testFramework=", "Test framework to run: core or desktop (can be specified multiple times)", s => _testFrameworks.Add(s) },
            { "testSet=", "Test set to include: compiler (adds compiler test assembly patterns to any --include patterns)", s => _testSet = s },
            { "testKind=", "Test kind to run: ioperation, runtimeasync, usedassemblies. runtimeasync requires --testFramework:core.", s => _testKind = s },
            { "ci", "Running in CI - sets ROSLYN_TEST_CI=true in test processes", o =>
            {
                if (o is object)
                    EnvironmentVariables["ROSLYN_TEST_CI"] = "true";
            }},
            { "env=", "Set an environment variable in test processes (format: --env:KEY=VALUE or --env:KEY for KEY=true)", s =>
            {
                var eqIndex = s.IndexOf('=');
                if (eqIndex >= 0)
                {
                    EnvironmentVariables[s[..eqIndex]] = s[(eqIndex + 1)..];
                }
                else
                {
                    EnvironmentVariables[s] = "true";
                }
            }},
        };

    protected bool ParseCore(string[] args, OptionSet optionSet, string applicationName, string description, out bool helpShown)
    {
        helpShown = false;
        try
        {
            var remaining = optionSet.Parse(args);
            if (remaining.Count > 0)
            {
                ConsoleUtil.Error($"Unrecognized arguments: {string.Join(" ", remaining)}");
                optionSet.WriteOptionDescriptions(Console.Out);
                return false;
            }
        }
        catch (OptionException e)
        {
            ConsoleUtil.WriteLine($"Error parsing command line arguments: {e.Message}");
            optionSet.WriteOptionDescriptions(Console.Out);
            return false;
        }

        if (_showHelp)
        {
            ConsoleUtil.WriteLine($"Usage: {applicationName} [OPTIONS]");
            ConsoleUtil.WriteLine();
            ConsoleUtil.WriteLine(description);
            ConsoleUtil.WriteLine("Test assemblies are matched by --include/--exclude regex patterns against project folder names.");
            ConsoleUtil.WriteLine();
            ConsoleUtil.WriteLine("Options:");
            optionSet.WriteOptionDescriptions(Console.Out);
            helpShown = true;
            return false;
        }

        if (_testFrameworks.Count > 0)
        {
            TestRuntime = 0;
            foreach (var tf in _testFrameworks)
            {
                if (string.Equals(tf, "core", StringComparison.OrdinalIgnoreCase))
                {
                    TestRuntime |= TestRuntime.Core;
                }
                else if (string.Equals(tf, "desktop", StringComparison.OrdinalIgnoreCase))
                {
                    TestRuntime |= TestRuntime.Framework;
                }
                else
                {
                    ConsoleUtil.WriteLine($"Invalid --testFramework value '{tf}'. Must be 'core' or 'desktop'.");
                    return false;
                }
            }
        }

        var testDesktop = (TestRuntime & TestRuntime.Framework) != 0;
        var testCompilerOnly = string.Equals(_testSet, "compiler", StringComparison.OrdinalIgnoreCase);
        if (_testSet is not null && !testCompilerOnly)
        {
            ConsoleUtil.WriteLine($"Invalid --testSet value '{_testSet}'. Must be 'compiler'.");
            return false;
        }

        if (_testKind is not null)
        {
            if (string.Equals(_testKind, "ioperation", StringComparison.OrdinalIgnoreCase))
            {
                EnvironmentVariables["ROSLYN_TEST_IOPERATION"] = "true";
            }
            else if (string.Equals(_testKind, "runtimeasync", StringComparison.OrdinalIgnoreCase))
            {
                EnvironmentVariables["DOTNET_RuntimeAsync"] = "1";
            }
            else if (string.Equals(_testKind, "usedassemblies", StringComparison.OrdinalIgnoreCase))
            {
                EnvironmentVariables["ROSLYN_TEST_USEDASSEMBLIES"] = "true";
            }
            else
            {
                ConsoleUtil.WriteLine($"Invalid --testKind value '{_testKind}'. Must be 'ioperation', 'runtimeasync', or 'usedassemblies'.");
                return false;
            }
        }

        if (testDesktop && EnvironmentVariables.ContainsKey("DOTNET_RuntimeAsync"))
        {
            ConsoleUtil.WriteLine("Runtime async validation is supported only on Core. Specify --testFramework:core and do not select desktop.");
            return false;
        }

        if (testCompilerOnly)
        {
            IncludeFilter.AddRange(CompilerTestAssemblyPatterns);
        }

        // Desktop x64 excludes InteractiveHost tests.
        if (testDesktop && Architecture != "x86")
        {
            ExcludeFilter.Add(@"\.InteractiveHost");
        }

        if (IncludeFilter.Count == 0)
        {
            IncludeFilter.Add(".*UnitTests.*");
        }

        var artifactsPath = _artifactsPath ?? TryGetArtifactsPath();
        if (artifactsPath is null || !Directory.Exists(artifactsPath))
        {
            ConsoleUtil.WriteLine($"Did not find artifacts directory at {artifactsPath}");
            return false;
        }

        var dotnetFilePath = _dotnetFilePath ?? TryGetDotNetPath();
        if (dotnetFilePath is null || !File.Exists(dotnetFilePath))
        {
            ConsoleUtil.WriteLine($"Did not find 'dotnet' at {dotnetFilePath}");
            return false;
        }

        ArtifactsDirectory = artifactsPath;
        DotnetFilePath = dotnetFilePath;
        return true;
    }

    private static string? TryGetArtifactsPath()
    {
        var path = AppContext.BaseDirectory;
        while (path is object && Path.GetFileName(path) != "artifacts")
        {
            path = Path.GetDirectoryName(path);
        }

        return path;
    }

    private static string? TryGetDotNetPath()
    {
        var dir = RuntimeEnvironment.GetRuntimeDirectory();
        var programName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
        while (dir != null && !File.Exists(Path.Combine(dir, programName)))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return dir == null ? null : Path.Combine(dir, programName);
    }
}
