// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
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
internal sealed class CustomExportAssemblyLoader(ExtensionAssemblyManager extensionAssemblyManager, ILoggerFactory loggerFactory) : IAssemblyLoader
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
        // CodeBase is spec'd as being a URI string - however VS-MEF doesn't always give us a URI, nor do they always give us a valid URI representation of the code base (for compatibility with clr behavior).
        // For example, when doing initial part discovery, we get a normal path as a string.  But when loading the assembly to create the graph, VS-MEF will pass us
        // a file URI with an unescaped path part.  
        // See https://github.com/microsoft/vs-mef/blob/21feb66e55cbef129801de3a6d572c087ee5b0b6/src/Microsoft.VisualStudio.Composition/Resolver.cs#L172
        //
        // This can cause issues during URI parsing, but we will handle that below.  First we try to parse the code base as a normal URI, which handles all the cases where we get
        // a non-URI string as well as the majority of the cases where we get a file URI string.
        var codeBaseUri = ProtocolConversions.CreateAbsoluteUri(codeBaseUriStr);
        if (!codeBaseUri.IsFile)
        {
            throw new ArgumentException($"Code base {codeBaseUriStr} for {assemblyName} is not a file URI.", nameof(codeBaseUriStr));
        }

        var codeBasePath = codeBaseUri.LocalPath;

        if (TryLoadAssemblyFromCodeBasePath(assemblyName, codeBasePath, out var assembly))
        {
            return assembly;
        }

        // As described above, we can get a code base URI that contains the unescaped code base file path.  This can cause issues when we parse it as a URI if the code base file path
        // contains URI reserved characters (for example '#') which are left unescaped in the URI string.  While it is a well formed URI, when System.Uri parses the code base URI
        // the path component can get mangled and longer accurately represent the actual file system path.
        //
        // A concrete example - given code base URI 'file:///c:/Learn C#/file.dll', the path component from System.Uri will be 'c:/learn c' and '#/file.dll' is parsed as part of the fragment.
        // Of course we do not find a dll at 'c:/learn c' and crash.
        //
        // Unfortunately, solving this can be difficult - there is an EscapedCodeBase property on AssemblyName, but it does not escape reserved characters.  It uses
        // the same implementation as Uri.EscapeUriString (which explicitly does not escape reserved characters as it cannot accurately do so).
        //
        // However - we do know that if we are given a file URI, the scheme and authority parts of the URI are correct (only the path can have unescaped reserved characters, which comes after both).
        // We can attempt to reconstruct the real code base file path by combining all the URI parts following the authority (the path, query, and fragment).
        // Note - System.URI returns the escaped versions of all these parts, so we unescape them first.
        var possibleCodeBasePath = Uri.UnescapeDataString(codeBaseUri.PathAndQuery) + Uri.UnescapeDataString(codeBaseUri.Fragment);
        if (TryLoadAssemblyFromCodeBasePath(assemblyName, possibleCodeBasePath, out assembly))
        {
            return assembly;
        }

        // We were given an explicit code base path, but no extension context had the assembly.
        // This is unexpected, so we'll throw an exception.
        throw new FileNotFoundException($"Could not find assembly {assemblyName} with code base {codeBasePath} in any extension context.");
    }

    private bool TryLoadAssemblyFromCodeBasePath(AssemblyName assemblyName, string codeBasePath, [NotNullWhen(true)] out Assembly? assembly)
    {
        assembly = null;
        if (!File.Exists(codeBasePath))
        {
            _logger.LogTrace("Code base {codeBase} does not exist for {assemblyName}", codeBasePath, assemblyName);
            return false;
        }

        assembly = extensionAssemblyManager.TryLoadAssemblyInExtensionContext(codeBasePath);
        if (assembly is not null)
        {
            _logger.LogTrace("{assemblyName} with code base {codeBase} found in extension context.", assemblyName, codeBasePath);
            return true;
        }

        _logger.LogTrace("Code base {codeBase} not found in any extension context for {assemblyName}", codeBasePath, assemblyName);
        return false;
    }
}
