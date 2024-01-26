// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Defines a MEF assembly loader that knows how to load assemblies from both the default assembly load context
/// and from the assembly load contexts for any of our extensions.
/// </summary>
internal class CustomExportAssemblyLoader : IAssemblyLoader
{
    private readonly ImmutableArray<AssemblyLoadContext> _extensionMefContexts;

    public CustomExportAssemblyLoader(ImmutableArray<AssemblyLoadContext> extensionMefContexts)
    {
        _extensionMefContexts = extensionMefContexts;
    }

    /// <summary>
    /// Loads assemblies from either the host or from our extensions.
    /// If an assembly exists in both the host and an extension, we will use the host assembly for the MEF catalog.
    /// If an assembly exists in two extensions, we use the first one we find for the MEF catalog.
    /// </summary>
    public Assembly LoadAssembly(AssemblyName assemblyName)
    {
        // First attempt to load the assembly from the default context.
        var assemblyInDefaultContext = LoadAssemblyInContext(assemblyName, AssemblyLoadContext.Default);
        if (assemblyInDefaultContext != null)
        {
            return assemblyInDefaultContext;
        }

        // Check if the assembly can be loaded in any of the extension contexts.
        foreach (var context in _extensionMefContexts)
        {
            var assemblyInExtensionContext = LoadAssemblyInContext(assemblyName, context);
            if (assemblyInExtensionContext != null)
            {
                return assemblyInExtensionContext;
            }
        }

        throw new FileNotFoundException($"Could not find assembly {assemblyName.Name} in any host or extension context.");
    }

    public Assembly LoadAssembly(string assemblyFullName, string? codeBasePath)
    {
        var assemblyName = new AssemblyName(assemblyFullName);
        return LoadAssembly(assemblyName);
    }

    private static Assembly? LoadAssemblyInContext(AssemblyName assemblyName, AssemblyLoadContext context)
    {
        try
        {
            return context.LoadFromAssemblyName(assemblyName);
        }
        catch (FileNotFoundException) when (assemblyName.Name is not null)
        {
            return null;
        }
    }
}
