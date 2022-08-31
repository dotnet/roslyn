// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.Text.Json;
using System.Text.RegularExpressions;

// Setup cancellation for ctrl-c key presses
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += delegate
{
    cts.Cancel();
};

var rootCommand = new RootCommand("Looks for test assemblies based on the input filters");

// Required options
rootCommand.AddOption(new Option<string>("--artifactsDirectory", "Path to the artifacts directory") { IsRequired = true });
rootCommand.AddOption(new Option<string[]>("--targetFrameworks", "Target frameworks to test") { IsRequired = true });
rootCommand.AddOption(new Option<string>(alias: "--configuration", "Configuration to test: Debug or Release") { IsRequired = true });
rootCommand.AddOption(new Option<string>("--outputFilePath", "File path to write the partition information to.") { IsRequired = true });

// Optional options
rootCommand.AddOption(new Option<string[]>("--include", () => new string[] { ".*UnitTests.*" }, "Expression for including unit test dlls: default *.UnitTests.*"));
rootCommand.AddOption(new Option<string[]>("--exclude", () => Array.Empty<string>(), "Expression for excluding unit test dlls: default is empty"));

rootCommand.Handler = CommandHandler.Create(Handle);

return await rootCommand.InvokeAsync(args);

int Handle(TestAssemblyFinderOptions finderOptions)
{
    try
    {
        Console.WriteLine($"Input options: {finderOptions}");

        // Find the assemblies to partition.
        var assemblies = GetAssemblyFilePaths(finderOptions.ArtifactsDirectory, finderOptions.TargetFrameworks, finderOptions.Configuration, finderOptions.Include, finderOptions.Exclude);

        // Write the assembly information to a file so we can get back to it in subsequent steps.
        File.WriteAllText(finderOptions.OutputFilePath, string.Join(Environment.NewLine, assemblies));
        Console.WriteLine($"Wrote {assemblies.Length} paths to {finderOptions.OutputFilePath}");

        return 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"##[Error]Failed to find test assemblies.");
        Console.WriteLine(ex.ToString());
        return 1;
    }
}

static ImmutableArray<string> GetAssemblyFilePaths(string artifactsDirectory, string[] targetFrameworks, string configuration, string[] include, string[] exclude)
{
    var list = new List<string>();
    var binDirectory = Path.Combine(artifactsDirectory, "bin");
    foreach (var project in Directory.EnumerateDirectories(binDirectory, "*", SearchOption.TopDirectoryOnly))
    {
        var name = Path.GetFileName(project);
        if (!shouldInclude(name, include) || shouldExclude(name, exclude))
        {
            continue;
        }

        var fileName = $"{name}.dll";
        // Find the dlls matching the request configuration and target frameworks.
        foreach (var targetFramework in targetFrameworks)
        {
            var targetFrameworkDirectory = Path.Combine(project, configuration, targetFramework);
            var filePath = Path.Combine(targetFrameworkDirectory, fileName);
            if (File.Exists(filePath))
            {
                list.Add(filePath);
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
                list.Add(matches[0]);
            }
        }
    }

    if (list.Count == 0)
    {
        throw new InvalidOperationException($"Did not find any test assemblies");
    }

    list.Sort();
    return list.ToImmutableArray();

    static bool shouldInclude(string name, string[] includeFilter)
    {
        foreach (var pattern in includeFilter)
        {
            if (Regex.IsMatch(name, pattern.Trim('\'', '"')))
            {
                return true;
            }
        }

        return false;
    }

    static bool shouldExclude(string name, string[] excludeFilter)
    {
        foreach (var pattern in excludeFilter)
        {
            if (Regex.IsMatch(name, pattern.Trim('\'', '"')))
            {
                return true;
            }
        }

        return false;
    }
}

record TestAssemblyFinderOptions(
    string ArtifactsDirectory,
    string[] TargetFrameworks,
    string Configuration,
    string OutputFilePath,
    string[] Include,
    string[] Exclude)
{
    public override string ToString()
    {
        return $"{nameof(ArtifactsDirectory)} = {ArtifactsDirectory}; {nameof(TargetFrameworks)} = {string.Join(",", TargetFrameworks)}; {nameof(Configuration)} = {Configuration};" +
            $" {nameof(OutputFilePath)} = {OutputFilePath}; {nameof(Include)} = {string.Join(",", Include)}; {nameof(Exclude)} = {string.Join(",", Exclude)}";
    }
}
