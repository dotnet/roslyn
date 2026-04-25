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
        var layerFolderName = GetLayerFolderName(layer);

        Debug.Assert(!testDirectoryFirst || layer != Layer.Tooling, "If testDirectoryFirst is true and we're in the tooling layer, that means the project directory ternary needs to be updated to handle the false case");
        var projectDirectory = testDirectoryFirst || layer == Layer.Tooling
            ? Path.Combine(repoRoot, "src", layerFolderName, "test", directoryHint)
            : Path.Combine(repoRoot, "src", layerFolderName, directoryHint, "test");

        if (string.Equals(directoryHint, "Microsoft.AspNetCore.Razor.Language.Test", StringComparison.Ordinal))
        {
            Debug.Assert(!testDirectoryFirst);
            Debug.Assert(layer == Layer.Compiler);
            projectDirectory = Path.Combine(repoRoot, "src", "Compiler", "Microsoft.AspNetCore.Razor.Language", "test");
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
        return SearchUp(baseDir, "global.json");
    }

    public static string GetProjectDirectory(Type type, Layer layer, bool useCurrentDirectory = false)
    {
        var baseDir = useCurrentDirectory ? Directory.GetCurrentDirectory() : AppContext.BaseDirectory;
        var layerFolderName = GetLayerFolderName(layer);
        var repoRoot = SearchUp(baseDir, "global.json");
        var assemblyName = type.Assembly.GetName().Name;
        var projectDirectory = layer == Layer.Compiler
            ? Path.Combine(repoRoot, "src", layerFolderName, assemblyName, "test")
            : Path.Combine(repoRoot, "src", layerFolderName, "test", assemblyName);

        if (string.Equals(assemblyName, "Microsoft.AspNetCore.Razor.Language.Test", StringComparison.Ordinal))
        {
            Debug.Assert(layer == Layer.Compiler);
            projectDirectory = Path.Combine(repoRoot, "src", "Compiler", "Microsoft.AspNetCore.Razor.Language", "test");
        }
        else if (string.Equals(assemblyName, "Microsoft.AspNetCore.Razor.Language.Legacy.Test", StringComparison.Ordinal))
        {
            Debug.Assert(layer == Layer.Compiler);
            projectDirectory = Path.Combine(repoRoot, "src", "Compiler", "Microsoft.AspNetCore.Razor.Language", "legacyTest");
        }

        if (!Directory.Exists(projectDirectory))
        {
            throw new InvalidOperationException(
                $@"Could not locate project directory for type {type.FullName}. Directory probe path: {projectDirectory}.");
        }

        return projectDirectory;
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
