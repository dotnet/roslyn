// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;

namespace PrepareTests;
public class ListTests
{
    /// <summary>
    /// Regex to find test lines from the output of dotnet test.
    /// </summary>
    /// <remarks>
    /// The goal is to match lines that contain a fully qualified test name e.g.
    /// <code>Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedParametersAndValues.RemoveUnusedValueAssignmentTests.UnusedVarPattern_PartOfIs</code>
    /// or
    /// <code><![CDATA[Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles.NamingStylesTests.TestPascalCaseSymbol_ExpectedSymbolAndAccessibility(camelCaseSymbol: "void Outer() { System.Action<int> action = (int [|"..., pascalCaseSymbol: "void Outer() { System.Action<int> action = (int M)"..., symbolKind: Parameter, accessibility: NotApplicable)]]></code>
    /// But not anything else dotnet test --list-tests outputs, like
    /// <code>
    /// Microsoft (R) Test Execution Command Line Tool Version 17.3.0-preview-20220414-05 (x64)
    /// Copyright(c) Microsoft Corporation.  All rights reserved.
    ///
    /// The following Tests are available:
    /// </code>
    /// The regex looks for the namespace names (groups of non-whitespace characters followed by a dot) at the beginning of the line.
    /// </remarks>
    private static readonly Regex TestOutputFormat = new("^(\\S)*\\..*", RegexOptions.Compiled);

    internal static async Task RunAsync(string sourceDirectory, string dotnetPath)
    {
        // Find all test assemblies.
        var binDirectory = Path.Combine(sourceDirectory, "artifacts", "bin");
        var assemblies = GetTestAssemblyFilePaths(binDirectory);
        Console.WriteLine($"Found test assemblies:{Environment.NewLine}{string.Join(Environment.NewLine, assemblies.Select(a => a.AssemblyPath))}");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Discover tests via `dotnet test --list-tests` and write out the test counts to a file so we can use it for partitioning later.
        await GetTypeInfoAsync(assemblies, dotnetPath);
        stopwatch.Stop();
        Console.WriteLine($"Discovered tests in {stopwatch.Elapsed}");
    }

    /// <summary>
    /// Returns the file path of the test data results for a particular assembly.
    /// This is written using the output of dotnet test list in <see cref="GetTypeInfoAsync(ImmutableArray{AssemblyInfo}, string)"/>
    /// and read during each test leg for partitioning.
    /// </summary>
    public static string GetTestDataFilePath(AssemblyInfo assembly)
    {
        var assemblyDirectory = Path.GetDirectoryName(assembly.AssemblyPath)!;
        var fileName = $"{assembly.AssemblyName}_tests.txt";
        var outputPath = Path.Combine(assemblyDirectory, fileName);
        return outputPath;
    }

    /// <summary>
    /// Find all unit test assemblies that we need to discover tests on.
    /// </summary>
    public static ImmutableArray<AssemblyInfo> GetTestAssemblyFilePaths(
        string binDirectory,
        Func<string, bool>? shouldSkipTestDirectory = null,
        string configurationSearchPattern = "*",
        List<string>? targetFrameworks = null)
    {
        var list = new List<AssemblyInfo>();

        // Find all the project folders that fit our naming scheme for unit tests.
        foreach (var project in Directory.EnumerateDirectories(binDirectory, "*.UnitTests", SearchOption.TopDirectoryOnly))
        {
            if (shouldSkipTestDirectory != null && shouldSkipTestDirectory(project))
            {
                continue;
            }

            var name = Path.GetFileName(project);
            var fileName = $"{name}.dll";

            // Find the dlls matching the request configuration and target frameworks.
            var configurationDirectories = Directory.EnumerateDirectories(project, configurationSearchPattern, SearchOption.TopDirectoryOnly);
            foreach (var configuration in configurationDirectories)
            {
                var targetFrameworkDirectories = Directory.EnumerateDirectories(configuration, "*", SearchOption.TopDirectoryOnly);
                if (targetFrameworks != null)
                {
                    targetFrameworkDirectories = targetFrameworks.Select(tfm => Path.Combine(configuration, tfm));
                }

                foreach (var targetFrameworkDirectory in targetFrameworkDirectories)
                {
                    // In multi-targeting scenarios we will build both .net core and .net framework versions of the assembly on unix.
                    // If we're on unix and we see the .net framework assembly, skip it as we can't list or run tests on it anyway.
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Path.GetFileName(targetFrameworkDirectory) == "net472")
                    {
                        Console.WriteLine($"Skipping net472 assembly on unix: {targetFrameworkDirectory}");
                        continue;
                    }

                    var filePath = Path.Combine(targetFrameworkDirectory, fileName);
                    if (File.Exists(filePath))
                    {
                        list.Add(new AssemblyInfo(filePath));
                    }
                    else if (Directory.Exists(targetFrameworkDirectory) && Directory.GetFiles(targetFrameworkDirectory, searchPattern: "*.UnitTests.dll") is { Length: > 0 } matches)
                    {
                        // If the unit test assembly name doesn't match the project folder name, but still matches our "unit test" name pattern, we want to run it.
                        // If more than one such assembly is present in a project output folder, we assume something is wrong with the build configuration.
                        // For example, one unit test project might be referencing another unit test project.
                        if (matches.Length > 1)
                        {
                            var message = $"Multiple unit test assemblies found in '{targetFrameworkDirectory}'. Please adjust the build to prevent this. Matches:{Environment.NewLine}{string.Join(Environment.NewLine, matches)}";
                            throw new Exception(message);
                        }
                        list.Add(new AssemblyInfo(matches[0]));
                    }
                }
            }
        }

        Contract.Assert(list.Count > 0, $"Did not find any test assemblies");

        list.Sort();
        return list.ToImmutableArray();
    }

    /// <summary>
    /// Runs `dotnet test _assembly_ --list-tests` on all test assemblies to get the real count of tests per assembly.
    /// </summary>
    private static async Task GetTypeInfoAsync(ImmutableArray<AssemblyInfo> assemblies, string dotnetFilePath)
    {
        // We run this one assembly at a time because it will not output which test is in which assembly otherwise.
        // It's also faster than making a single call to dotnet test with all the assemblies.
        await Parallel.ForEachAsync(assemblies, async (assembly, cancellationToken) =>
        {
            var assemblyPath = assembly.AssemblyPath;
            var commandArgs = $"test {assemblyPath} --list-tests";
            var processResult = await ProcessRunner.CreateProcess(dotnetFilePath, commandArgs, workingDirectory: Directory.GetCurrentDirectory(), captureOutput: true, displayWindow: false, cancellationToken: cancellationToken).Result;
            if (processResult.ExitCode != 0)
            {
                var errorOutput = string.Join(Environment.NewLine, processResult.ErrorLines);
                var output = string.Join(Environment.NewLine, processResult.OutputLines);
                throw new InvalidOperationException($"dotnet test failed with {processResult.ExitCode} for {assemblyPath}.{Environment.NewLine}Error output: {errorOutput}{Environment.NewLine}Output: {output}");
            }

            var typeInfo = ParseDotnetTestOutput(processResult.OutputLines);
            var testCount = typeInfo.Sum(type => type.TestCount);
            Contract.Assert(testCount > 0, $"Did not find any tests in {assembly}, output was {Environment.NewLine}{string.Join(Environment.NewLine, processResult.OutputLines)}");

            Console.WriteLine($"Found {testCount} tests for {assemblyPath}");

            await WriteTestDataAsync(assembly, typeInfo, cancellationToken);
        });

        return;

        static async Task WriteTestDataAsync(AssemblyInfo assembly, ImmutableArray<TypeInfo> typeInfo, CancellationToken cancellationToken)
        {
            var outputPath = GetTestDataFilePath(assembly);

            using var createStream = File.Create(outputPath);
            await JsonSerializer.SerializeAsync(createStream, typeInfo, cancellationToken: cancellationToken);
            await createStream.DisposeAsync();
        }
    }

    /// <summary>
    /// Parse the output of `dotnet test` to count the number of tests in the assembly by type name.
    /// </summary>
    private static ImmutableArray<TypeInfo> ParseDotnetTestOutput(IEnumerable<string> output)
    {
        // Find all test lines from the output of dotnet test using a regex match.
        var testList = output.Select(line => line.TrimStart()).Where(line => TestOutputFormat.IsMatch(line));

        // Figure out the fully qualified type name for each test.
        var typeList = testList
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(GetFullyQualifiedTypeName);

        // Count all occurences of the type name in the test list to figure out how many tests per type we have.
        var groups = typeList.GroupBy(type => type);
        var result = groups.Select(group => new TypeInfo(GetTypeNameFromFullyQualifiedName(group.Key), group.Key, group.Count())).ToImmutableArray();
        return result;

        static string GetFullyQualifiedTypeName(string testLine)
        {
            // Remove whitespace from the start as the list is always indented.
            var test = testLine.TrimStart();
            // The common case is just a fully qualified method name e.g.
            //     Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedParametersAndValues.RemoveUnusedValueAssignmentTests.UnusedVarPattern_PartOfIs
            //
            // However, we can also have more complex expressions with actual code in them (and periods) like
            //     Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles.NamingStylesTests.TestPascalCaseSymbol_ExpectedSymbolAndAccessibility(camelCaseSymbol: "void Outer() { System.Action<int> action = (int [|"..., pascalCaseSymbol: "void Outer() { System.Action<int> action = (int M)"..., symbolKind: Parameter, accessibility: NotApplicable)
            //
            // So we first split on ( to get the fully qualified method name.  This is valid because the namespace, type name, and method name cannot have parens.
            // The first part of the split gives us the fully qualified method name.  From that we can take everything up until the last period get the fully qualified type name.
            var splitString = test.Split("(");
            var fullyQualifiedMethod = splitString[0];

            var periodBeforeMethodName = fullyQualifiedMethod.LastIndexOf(".");
            var fullyQualifiedType = fullyQualifiedMethod[..periodBeforeMethodName];

            return fullyQualifiedType;
        }

        static string GetTypeNameFromFullyQualifiedName(string fullyQualifiedTypeName)
        {
            var periodBeforeTypeName = fullyQualifiedTypeName.LastIndexOf(".");
            var typeName = fullyQualifiedTypeName[(periodBeforeTypeName + 1)..];
            return typeName;
        }
    }
}
