// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

public static class TestProject
{
    public enum Layer
    {
        Compiler, Tooling
    }

    private static string GetLayerFolderName(Layer layer) => layer switch
    {
        Layer.Compiler => "Compiler",
        Layer.Tooling => "Razor",
        _ => throw TestExceptionUtilities.UnexpectedValue(layer)
    };

    public static string GetProjectDirectory(string directoryHint, Layer layer, bool testDirectoryFirst = false)
    {
        var repoRoot = SearchUp(AppContext.BaseDirectory, "global.json");
        var razorRepoRoot = Directory.Exists(Path.Combine(repoRoot, "src", "Razor", "src"))
            ? Path.Combine(repoRoot, "src", "Razor")
            : repoRoot;
        var layerFolderName = GetLayerFolderName(layer);
        var normalizedDirectoryHint = layer == Layer.Compiler && testDirectoryFirst && directoryHint.EndsWith(".Tests", StringComparison.Ordinal)
            ? directoryHint[..^".Tests".Length] + ".UnitTests"
            : directoryHint;

        Debug.Assert(!testDirectoryFirst || layer != Layer.Tooling, "If testDirectoryFirst is true and we're in the tooling layer, that means the project directory ternary needs to be updated to handle the false case");
        var projectDirectory = testDirectoryFirst || layer == Layer.Tooling
            ? Path.Combine(razorRepoRoot, "src", layerFolderName, "test", normalizedDirectoryHint)
            : Path.Combine(razorRepoRoot, "src", layerFolderName, normalizedDirectoryHint, "test");

        if (string.Equals(directoryHint, "Microsoft.AspNetCore.Razor.Language.Test", StringComparison.Ordinal))
        {
            Debug.Assert(!testDirectoryFirst);
            Debug.Assert(layer == Layer.Compiler);
            projectDirectory = Path.Combine(razorRepoRoot, "src", "Compiler", "Microsoft.AspNetCore.Razor.Language", "test");
        }

        if (!Directory.Exists(projectDirectory))
        {
            var outputRelativeProjectDirectory = TryGetAppContextTestProjectDirectory();
            if (outputRelativeProjectDirectory is not null)
            {
                projectDirectory = outputRelativeProjectDirectory;
            }
        }

        if (!Directory.Exists(projectDirectory))
        {
            throw new InvalidOperationException(
                $@"Could not locate project directory for type {directoryHint}. Directory probe path: {projectDirectory}.");
        }

        return projectDirectory;
    }

    public static string GetRepoRoot(bool useCurrentDirectory = false)
    {
        var baseDir = useCurrentDirectory ? Directory.GetCurrentDirectory() : AppContext.BaseDirectory;
        var repoRoot = SearchUp(baseDir, "global.json");
        if (File.Exists(Path.Combine(repoRoot, "eng", "targets", "RazorServices.props")))
        {
            return repoRoot;
        }

        var outputRelativeRepoRoot = TryGetAppContextRepositoryRoot();
        return outputRelativeRepoRoot ?? repoRoot;
    }

    public static string GetProjectDirectory(Type type, Layer layer, bool useCurrentDirectory = false)
    {
        var baseDir = useCurrentDirectory ? Directory.GetCurrentDirectory() : AppContext.BaseDirectory;
        var layerFolderName = GetLayerFolderName(layer);
        var repoRoot = SearchUp(baseDir, "global.json");
        var razorRepoRoot = Directory.Exists(Path.Combine(repoRoot, "src", "Razor", "src"))
            ? Path.Combine(repoRoot, "src", "Razor")
            : repoRoot;
        var assemblyName = type.Assembly.GetName().Name;
        var normalizedAssemblyName = layer == Layer.Compiler && assemblyName.EndsWith(".UnitTests", StringComparison.Ordinal)
            ? assemblyName[..^".UnitTests".Length]
            : assemblyName;
        var projectDirectory = layer == Layer.Compiler
            ? Path.Combine(razorRepoRoot, "src", layerFolderName, normalizedAssemblyName, "test")
            : Path.Combine(razorRepoRoot, "src", layerFolderName, "test", assemblyName);

        if (string.Equals(assemblyName, "Microsoft.AspNetCore.Razor.Language.Test", StringComparison.Ordinal))
        {
            Debug.Assert(layer == Layer.Compiler);
            projectDirectory = Path.Combine(razorRepoRoot, "src", "Compiler", "Microsoft.AspNetCore.Razor.Language", "test");
        }
        else if (string.Equals(assemblyName, "Microsoft.AspNetCore.Razor.Language.Legacy.Test", StringComparison.Ordinal) ||
                 string.Equals(assemblyName, "Microsoft.AspNetCore.Razor.Language.Legacy.UnitTests", StringComparison.Ordinal))
        {
            Debug.Assert(layer == Layer.Compiler);
            projectDirectory = Path.Combine(razorRepoRoot, "src", "Compiler", "Microsoft.AspNetCore.Razor.Language", "legacyTest");
        }

        if (layer == Layer.Compiler &&
            !Directory.Exists(projectDirectory) &&
            assemblyName.EndsWith(".UnitTests", StringComparison.Ordinal))
        {
            var testDirectoryFirstProjectDirectory = Path.Combine(razorRepoRoot, "src", layerFolderName, "test", assemblyName);
            if (Directory.Exists(testDirectoryFirstProjectDirectory))
            {
                projectDirectory = testDirectoryFirstProjectDirectory;
            }
        }

        if (!Directory.Exists(projectDirectory))
        {
            var outputRelativeProjectDirectory = TryGetAppContextTestProjectDirectory();
            if (outputRelativeProjectDirectory is not null)
            {
                projectDirectory = outputRelativeProjectDirectory;
            }
        }

        if (!Directory.Exists(projectDirectory))
        {
            throw new InvalidOperationException(
                $@"Could not locate project directory for type {type.FullName}. Directory probe path: {projectDirectory}.");
        }

        return projectDirectory;
    }

    private static string TryGetAppContextTestProjectDirectory()
    {
        var appContextBaseDirectory = AppContext.BaseDirectory;
        if (!Directory.Exists(appContextBaseDirectory))
        {
            return null;
        }

        if (Directory.Exists(Path.Combine(appContextBaseDirectory, "TestFiles")))
        {
            return appContextBaseDirectory;
        }

        foreach (var _ in Directory.EnumerateDirectories(appContextBaseDirectory, "TestFiles", SearchOption.AllDirectories))
        {
            return appContextBaseDirectory;
        }

        return null;
    }

    private static string TryGetAppContextRepositoryRoot()
    {
        var appContextBaseDirectory = AppContext.BaseDirectory;
        return File.Exists(Path.Combine(appContextBaseDirectory, "eng", "targets", "RazorServices.props"))
            ? appContextBaseDirectory
            : null;
    }
    private static string SearchUp(string baseDirectory, string fileName)
    {
        var directoryInfo = new DirectoryInfo(baseDirectory);
        do
        {
            var fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, fileName));
            if (fileInfo.Exists)
            {
                return fileInfo.DirectoryName;
            }

            directoryInfo = directoryInfo.Parent;
        }
        while (directoryInfo.Parent != null);

        throw new Exception($"File {fileName} could not be found in {baseDirectory} or its parent directories.");
    }
}
