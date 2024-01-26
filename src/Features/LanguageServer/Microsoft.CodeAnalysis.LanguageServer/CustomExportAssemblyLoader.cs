// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Defines a MEF assembly loader that knows how to load assemblies from both the default assembly load context
/// and from the assembly load contexts for any of our extensions.
/// </summary>
internal class CustomExportAssemblyLoader(ExtensionAssemblyManager extensionAssemblyManager) : IAssemblyLoader
{
    /// <summary>
    /// Loads assemblies from either the host or from our extensions.
    /// If an assembly exists in both the host and an extension, we will use the host assembly for the MEF catalog.
    /// If an assembly exists in two extensions, we use the first one we find for the MEF catalog.
    /// </summary>
    public Assembly LoadAssembly(AssemblyName assemblyName)
    {
        // First attempt to load the assembly from the default context.
        try
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
        }
        catch (FileNotFoundException) when (assemblyName.Name is not null)
        {
            // continue checking the extension contexts.
        }

        var extensionAssembly = extensionAssemblyManager.TryLoadAssemblyInExtensionContext(assemblyName);
        if (extensionAssembly != null)
        {
            return extensionAssembly;
        }

        throw new FileNotFoundException($"Could not find assembly {assemblyName.Name} in any host or extension context.");
    }

    public Assembly LoadAssembly(string assemblyFullName, string? codeBasePath)
    {
        var assemblyName = new AssemblyName(assemblyFullName);
        return LoadAssembly(assemblyName);
    }
}
