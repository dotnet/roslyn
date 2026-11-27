// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.IntegrationTest;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

internal static class IntegrationTestAssemblyResolver
{
    private static readonly ConcurrentDictionary<string, byte> s_codeBaseDirectories = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

    public static void AddCodeBaseDirectory(string? directory)
    {
        if (directory == null)
        {
            return;
        }

        directory = Path.GetFullPath(directory);
        if (!s_codeBaseDirectories.TryAdd(directory, 0))
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => ResolveAssembly(e.Name, directory);
    }

    public static Assembly LoadAssemblyFromPath(string path)
        => Assembly.LoadFile(Path.GetFullPath(path));

    private static Assembly? ResolveAssembly(string assemblyNameText, string codeBaseDirectory)
    {
        var assemblyName = new AssemblyName(assemblyNameText);
        var isRoslynProductAssembly = IsRoslynProductAssemblyName(assemblyName.Name);

        var loadedAssembly = isRoslynProductAssembly
            ? GetRoslynProductAssembly(assemblyName, codeBaseDirectory)
            : GetLoadedAssembly(assemblyName);
        if (loadedAssembly != null)
        {
            return loadedAssembly;
        }

        if (isRoslynProductAssembly)
        {
            return null;
        }

        var path = Path.Combine(codeBaseDirectory, assemblyName.Name + ".dll");
        return File.Exists(path)
            ? LoadAssemblyFromPath(path)
            : null;
    }

    private static Assembly? GetRoslynProductAssembly(AssemblyName requestedAssemblyName, string codeBaseDirectory)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(assembly.GetName().Name, requestedAssemblyName.Name, StringComparison.Ordinal)
                && !IsAssemblyFromDirectory(assembly, codeBaseDirectory))
            {
                return assembly;
            }
        }

        foreach (var directory in GetVisualStudioRoslynProductAssemblyDirectories(codeBaseDirectory))
        {
            var path = Path.Combine(directory, requestedAssemblyName.Name + ".dll");
            if (File.Exists(path))
            {
                return LoadAssemblyFromPath(path);
            }
        }

        return null;
    }

    private static Assembly? GetLoadedAssembly(AssemblyName requestedAssemblyName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(assembly.FullName, requestedAssemblyName.FullName, StringComparison.Ordinal))
            {
                return assembly;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetVisualStudioRoslynProductAssemblyDirectories(string codeBaseDirectory)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!IsRoslynProductAssemblyName(assembly.GetName().Name)
                || IsAssemblyFromDirectory(assembly, codeBaseDirectory))
            {
                continue;
            }

            var directory = Path.GetDirectoryName(assembly.Location);
            if (directory != null)
            {
                yield return directory;
            }
        }

        var baseDirectory = AppContext.BaseDirectory;
        if (baseDirectory != null)
        {
            yield return Path.Combine(
                baseDirectory,
                "CommonExtensions",
                "Microsoft",
                "VBCSharp",
                "LanguageServices");
        }
    }

    private static bool IsAssemblyFromDirectory(Assembly assembly, string directory)
    {
        var assemblyLocation = assembly.Location;
        if (assemblyLocation.Length == 0)
        {
            return false;
        }

        var fullAssemblyLocation = Path.GetFullPath(assemblyLocation);
        var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullAssemblyLocation.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRoslynProductAssemblyName(string? assemblyName)
    {
        if (assemblyName == null
            || assemblyName.IndexOf(".Test", StringComparison.Ordinal) >= 0
            || assemblyName.EndsWith("Tests", StringComparison.Ordinal))
        {
            return false;
        }

        return assemblyName == "Microsoft.CodeAnalysis"
            || assemblyName == "Microsoft.VisualStudio.LanguageServices"
            || assemblyName.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal)
            || assemblyName.StartsWith("Microsoft.VisualStudio.LanguageServices.", StringComparison.Ordinal);
    }
}