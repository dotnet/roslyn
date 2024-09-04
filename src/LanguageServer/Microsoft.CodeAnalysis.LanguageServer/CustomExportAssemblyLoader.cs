// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Defines a MEF assembly loader that knows how to load assemblies from both the default assembly load context
/// and from the assembly load contexts for any of our extensions.
/// </summary>
internal class CustomExportAssemblyLoader(ExtensionAssemblyManager extensionAssemblyManager, ILoggerFactory loggerFactory) : IAssemblyLoader
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("MEF Assembly Loader");

    /// <summary>
    /// Loads assemblies from either the host or from our extensions.
    /// If an assembly exists in both the host and an extension, we will use the host assembly for the MEF catalog.
    /// </summary>
    public Assembly LoadAssembly(AssemblyName assemblyName)
    {
        // VS-MEF generally tries to populate AssemblyName.CodeBase with the path to the assembly being loaded.
        // We need to read this in order to figure out which ALC we should load the assembly into.
#pragma warning disable SYSLIB0044 // Type or member is obsolete
        var codeBasePath = assemblyName.CodeBase;
#pragma warning restore SYSLIB0044 // Type or member is obsolete
        return LoadAssembly(assemblyName, codeBasePath);
    }

    public Assembly LoadAssembly(string assemblyFullName, string? codeBasePath)
    {
        var assemblyName = new AssemblyName(assemblyFullName);
        return LoadAssembly(assemblyName, codeBasePath);
    }

    private Assembly LoadAssembly(AssemblyName assemblyName, string? codeBasePath)
    {
        _logger.LogTrace("Loading assembly {assemblyName}", assemblyName);

        // First attempt to load the assembly from the default context.
        Exception loadException;
        try
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
        }
        catch (FileNotFoundException ex)
        {
            loadException = ex;
            // continue checking the extension contexts.
        }

        if (codeBasePath is not null)
        {
            return LoadAssemblyFromCodeBase(assemblyName, codeBasePath);
        }

        // We don't have a code base path for this assembly.  We'll look at our map of assembly name
        // to extension to see if we can find the assembly in the right context.
        var assembly = extensionAssemblyManager.TryLoadAssemblyInExtensionContext(assemblyName);
        if (assembly is not null)
        {
            _logger.LogTrace("{assemblyName} found in extension context without code base", assemblyName);
            return assembly;
        }

        _logger.LogCritical("{assemblyName} not found in any host or extension context", assemblyName);
        throw loadException;
    }

    private Assembly LoadAssemblyFromCodeBase(AssemblyName assemblyName, string codeBaseUriStr)
    {
        // CodeBase is spec'd as being a URL string.
        var codeBaseUri = ProtocolConversions.CreateAbsoluteUri(codeBaseUriStr);
        if (!codeBaseUri.IsFile)
        {
            throw new ArgumentException($"Code base {codeBaseUriStr} for {assemblyName} is not a file URI.", nameof(codeBaseUriStr));
        }

        var codeBasePath = codeBaseUri.LocalPath;

        var assembly = extensionAssemblyManager.TryLoadAssemblyInExtensionContext(codeBasePath);
        if (assembly is not null)
        {
            _logger.LogTrace("{assemblyName} with code base {codeBase} found in extension context.", assemblyName, codeBasePath);
            return assembly;
        }

        // We were given an explicit code base path, but no extension context had the assembly.
        // This is unexpected, so we'll throw an exception.
        throw new FileNotFoundException($"Could not find assembly {assemblyName} with code base {codeBasePath} in any extension context.");
    }
}
