// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Host.Mef;

internal static class MefHostServicesHelpers
{
    public static ImmutableArray<Assembly> LoadNearbyAssemblies(ImmutableArray<string> assemblyNames)
    {
        var assemblies = new List<Assembly>(assemblyNames.Length);

        foreach (var assemblyName in assemblyNames)
        {
            var assembly = TryLoadNearbyAssembly(assemblyName);
            if (assembly != null)
            {
                assemblies.Add(assembly);
            }
        }

        return [.. assemblies];
    }

    private static Assembly? TryLoadNearbyAssembly(string assemblySimpleName)
    {
        var thisAssemblyName = typeof(MefHostServicesHelpers).GetTypeInfo().Assembly.GetName();
        var assemblyShortName = thisAssemblyName.Name;
        var assemblyVersion = thisAssemblyName.Version;
        var publicKeyToken = thisAssemblyName.GetPublicKeyToken()?.Aggregate(string.Empty, (s, b) => s + b.ToString("x2"));

        if (string.IsNullOrEmpty(publicKeyToken))
        {
            publicKeyToken = "null";
        }

        var assemblyName = new AssemblyName(string.Format("{0}, Version={1}, Culture=neutral, PublicKeyToken={2}", assemblySimpleName, assemblyVersion, publicKeyToken));

        try
        {
            return Assembly.Load(assemblyName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static string? TryFindNearbyAssemblyLocation(string assemblySimpleName)
    {
        // Try to find the assembly location by looking for a filename matching that assembly name
        // at the same location as this assembly.
        var thisAssemblyName = typeof(MefHostServicesHelpers).GetTypeInfo().Assembly.Location;
        var thisAssemblyFolder = Path.GetDirectoryName(thisAssemblyName);
        var potentialAssemblyPath = thisAssemblyFolder != null
            ? Path.Combine(thisAssemblyFolder, assemblySimpleName + ".dll")
            : null;

        if (File.Exists(potentialAssemblyPath))
            return potentialAssemblyPath;

        // Otherwise, fall back to loading the assembly to find the file locations
        var assembly = TryLoadNearbyAssembly(assemblySimpleName);

        return assembly?.Location;
    }
}
