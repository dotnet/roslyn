// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal class CustomExportAssemblyLoader : IAssemblyLoader
{
    /// <summary>
    /// Cache assemblies that are already loaded by AssemblyName comparison
    /// </summary>
    private readonly Dictionary<AssemblyName, Assembly> _loadedAssemblies = new Dictionary<AssemblyName, Assembly>(AssemblyNameComparer.Instance);

    /// <summary>
    /// Base directory to search for <see cref="Assembly.LoadFrom(string)"/> if initial load fails
    /// </summary>
    private readonly string _baseDirectory;

    public CustomExportAssemblyLoader(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    public Assembly LoadAssembly(AssemblyName assemblyName)
    {
        Assembly? value;
        lock (_loadedAssemblies)
        {
            _loadedAssemblies.TryGetValue(assemblyName, out value);
        }

        if (value == null)
        {
            // Attempt to load the assembly normally, but fall back to Assembly.LoadFrom in the base
            // directory if the assembly load fails
            try
            {
                value = Assembly.Load(assemblyName);
            }
            catch (FileNotFoundException) when (assemblyName.Name is not null)
            {
                var filePath = Path.Combine(_baseDirectory, assemblyName.Name)
                    + (assemblyName.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                        ? ""
                        : ".dll");

                value = Assembly.LoadFrom(filePath);

                if (value is null)
                {
                    throw;
                }
            }

            lock (_loadedAssemblies)
            {
                _loadedAssemblies[assemblyName] = value;
                return value;
            }
        }

        return value;
    }

    public Assembly LoadAssembly(string assemblyFullName, string? codeBasePath)
    {
        var assemblyName = new AssemblyName(assemblyFullName);
        return LoadAssembly(assemblyName);
    }

    private class AssemblyNameComparer : IEqualityComparer<AssemblyName>
    {
        public static AssemblyNameComparer Instance = new AssemblyNameComparer();

        public bool Equals(AssemblyName? x, AssemblyName? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Name == y.Name;
        }

        public int GetHashCode([DisallowNull] AssemblyName obj)
        {
            return obj.Name?.GetHashCode(StringComparison.Ordinal) ?? 0;
        }
    }
}
