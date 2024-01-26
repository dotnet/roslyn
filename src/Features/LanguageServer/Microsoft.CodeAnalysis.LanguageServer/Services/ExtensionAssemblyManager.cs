// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

internal sealed class ExtensionAssemblyManager(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(ExtensionAssemblyManager));

    /// <summary>
    /// Guards access to <see cref="_extensionLoadContexts"/> and <see cref="_directoryLoadContexts"/> to
    /// ensures we only create a single ExtensionAssemblyLoadContext per extension directory.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Maps the full path of the extension assembly to the load context that was used to load it.
    /// Multiple paths can map to same load context.
    /// </summary>
    private readonly Dictionary<string, ExtensionAssemblyLoadContext> _extensionLoadContexts = [];

    /// <summary>
    /// Maps a directory to the load context used to load extensions in that directory.
    /// </summary>
    private readonly Dictionary<string, ExtensionAssemblyLoadContext> _directoryLoadContexts = [];

    /// <summary>
    /// This creates a (or reuses an existing) <see cref="ExtensionAssemblyLoadContext"/> to load an extension assembly.
    /// Extensions in the same directory get the same load context.
    /// </summary>
    public AssemblyLoadContext? LoadExtension(string assemblyFilePath)
    {
        lock (_lock)
        {
            // If we've already loaded this extension, return the existing context.
            if (_extensionLoadContexts.TryGetValue(assemblyFilePath, out var existingLoadContext))
            {
                return existingLoadContext;
            }

            // We need to load this extension, verify that the path is something we can load.
            // If it's not, log helpful error messages and no-op.  We do not want to take down the server
            // if an extension fails to load.
            if (!File.Exists(assemblyFilePath))
            {
                _logger.LogError("Extension path {assemblyFilePath} does not exist", assemblyFilePath);
            }

            var dir = Path.GetDirectoryName(assemblyFilePath);
            if (dir == null)
            {
                _logger.LogError("Failed to get directory from {assemblyFilePath}", assemblyFilePath);
                return null;
            }

            var fileName = Path.GetFileName(assemblyFilePath);
            if (fileName == null)
            {
                _logger.LogError("Failed to get file name from {assemblyFilePath}", assemblyFilePath);
                return null;
            }

            var fileNameNoExt = Path.GetFileNameWithoutExtension(assemblyFilePath);
            if (fileNameNoExt == null)
            {
                _logger.LogError("Failed to get file name without extension from {assemblyFilePath}", assemblyFilePath);
                return null;
            }

            _logger.LogTrace("Loading extension {assemblyFilePath}", assemblyFilePath);

            if (_directoryLoadContexts.TryGetValue(dir, out var existingDirectoryContext))
            {
                _logger.LogTrace("Reusing existing load context for {assemblyFilePath}", assemblyFilePath);
                return existingDirectoryContext;
            }

            var loadContext = new ExtensionAssemblyLoadContext(fileNameNoExt, dir, _logger);
            _extensionLoadContexts.Add(assemblyFilePath, loadContext);
            _directoryLoadContexts.Add(dir, loadContext);
            return loadContext;
        }
    }

    private sealed class ExtensionAssemblyLoadContext(string name, string extensionDirectory, ILogger logger)
        : AssemblyLoadContext(name)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var simpleName = assemblyName.Name!;
            var assemblyPath = Path.Combine(extensionDirectory, simpleName + ".dll");
            if (File.Exists(assemblyPath))
            {
                logger.LogTrace("[{name}] Loading {assembly} from extension directory", Name, simpleName);
                return LoadFromAssemblyPath(assemblyPath);
            }

            // TODO - need to handle satellite assembly loading?

            // This assembly isn't provided by the extension, continue with normal assembly loading
            // to attempt to use the instance (if any) provided by the host.
            logger.LogTrace("[{name}] Loading {assembly} from host", Name, simpleName);
            return null;
        }
    }
}
