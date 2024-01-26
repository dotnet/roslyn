// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

internal sealed class ExtensionAssemblyManager
{
    private readonly ImmutableDictionary<string, AssemblyLoadContext> _directoryLoadContexts;

    public ExtensionAssemblyManager(ImmutableDictionary<string, AssemblyLoadContext> directoryLoadContexts)
    {
        _directoryLoadContexts = directoryLoadContexts;
    }

    public static ExtensionAssemblyManager Create(ImmutableArray<string> extensionAssemblyPaths, string? devKitDependencyPath, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<ExportProviderBuilder>();
        if (devKitDependencyPath != null)
        {
            // Ensure the Roslyn DevKit assemblies are loaded into the default ALC so that extensions can use them.
            LoadDevKitAssemblies(devKitDependencyPath, logger);
        }

        var directoryLoadContexts = new Dictionary<string, AssemblyLoadContext>(StringComparer.Ordinal);
        foreach (var assemblyFilePath in extensionAssemblyPaths)
        {
            // Verify that the path is something we can load.
            // If it's not, log helpful error messages and no-op.  We do not want to take down the server if an extension fails to load.
            if (!File.Exists(assemblyFilePath))
            {
                logger.LogError("Extension path {assemblyFilePath} does not exist", assemblyFilePath);
                continue;
            }

            var directory = Path.GetDirectoryName(assemblyFilePath);
            if (directory == null)
            {
                logger.LogError("Failed to get directory from {assemblyFilePath}", assemblyFilePath);
                continue;
            }

            var fileNameNoExt = Path.GetFileNameWithoutExtension(assemblyFilePath);
            if (fileNameNoExt == null)
            {
                logger.LogError("Failed to get file name without extension from {assemblyFilePath}", assemblyFilePath);
                continue;
            }

            if (directoryLoadContexts.TryGetValue(directory, out var _))
            {
                logger.LogTrace("Reusing existing load context for {assemblyFilePath}", assemblyFilePath);
                continue;
            }

            // Create the extension assembly load context for the extension.
            logger.LogTrace("Loading extension {assemblyFilePath}", assemblyFilePath);
            var loadContext = new ExtensionAssemblyLoadContext(fileNameNoExt, directory, loggerFactory);
            directoryLoadContexts.Add(directory, loadContext);
        }

        return new ExtensionAssemblyManager(directoryLoadContexts.ToImmutableDictionary());
    }

    private static void LoadDevKitAssemblies(string devKitDependencyPath, ILogger logger)
    {
        var directoryName = Path.GetDirectoryName(devKitDependencyPath);
        Contract.ThrowIfNull(directoryName);
        logger.LogTrace("Loading DevKit assemblies from {directory}", directoryName);

        var directory = new DirectoryInfo(directoryName);
        foreach (var file in directory.GetFiles("*.dll"))
        {
            logger.LogTrace("Loading {assemblyName}", file.Name);
            // DevKit assemblies are loaded into the default load context. This allows extensions
            // to share the host's instance of these assemblies as long as they do not ship their own copy.
            AssemblyLoadContext.Default.LoadFromAssemblyPath(file.FullName);
        }
    }

    /// <summary>
    /// Loads an assembly from an assembly file path into the extension load context for the assembly's directory.
    /// If the directory containing the assembly file path is not an extension directory, this will return null.
    /// </summary>
    public Assembly? TryLoadAssemblyInExtensionContext(string assemblyFilePath)
    {
        var directory = Path.GetDirectoryName(assemblyFilePath);
        if (directory == null)
        {
            return null;
        }

        if (_directoryLoadContexts.TryGetValue(directory, out var loadContext))
        {
            return loadContext.LoadFromAssemblyPath(assemblyFilePath);
        }

        return null;
    }

    /// <summary>
    /// Loads an assembly from an assembly name in the extension load context.
    /// This will attempt to load the assembly from each extension load context and use the first one that succeeds.
    /// </summary>
    public Assembly? TryLoadAssemblyInExtensionContext(AssemblyName assemblyName)
    {
        // We don't know exactly which extension the assembly came from, so we'll try each extension load context.
        foreach (var loadContext in _directoryLoadContexts.Values)
        {
            try
            {
                return loadContext.LoadFromAssemblyName(assemblyName);
            }
            catch (FileNotFoundException)
            {
                // Ignore and try the next context.
            }
        }

        return null;
    }

    private sealed class ExtensionAssemblyLoadContext(string name, string extensionDirectory, ILoggerFactory loggerFactory)
        : AssemblyLoadContext(name)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger(name);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var simpleName = assemblyName.Name!;
            var assemblyPath = Path.Combine(extensionDirectory, simpleName + ".dll");
            if (File.Exists(assemblyPath))
            {
                _logger.LogTrace("Loading {assembly} from extension directory", simpleName);
                return LoadFromAssemblyPath(assemblyPath);
            }

            // TODO - need to handle satellite assembly loading?

            // This assembly isn't provided by this extension, continue with normal assembly loading
            // to check other extensions or the host for this assembly.
            return null;
        }
    }
}
