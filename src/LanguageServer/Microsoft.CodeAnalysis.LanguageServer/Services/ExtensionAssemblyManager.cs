// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.LanguageServer.StarredSuggestions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

/// <summary>
/// Manages extension assembly loading.  Extensions are isolated from one another and from the host
/// using assembly load contexts per extension.  This allows extensions to:
///   1.  Load their own version of a dependencies by shipping the dependent assembly inside the extension assembly folder.
///       These assemblies will not be visible to the host or other extensions.
///   2.  Load the host version of a dependency by not shipping the dependent assembly in the extension assembly folder.
///       Useful when the extension wants to use state from the assembly setup by the host (e.g. VSTelemetry).
/// 
/// The extension load contexts are defined per directory, so if two extension dlls come from the same directory,
/// they will share the same load context.
/// 
/// A couple of additional notes:
///   1.  Only the explicitly provided extension assembly paths are loaded into the MEF catalog.  If an extension wants
///       to contribute multiple assemblies to the catalog, each assembly must be passed as an extension assembly path.
///   2.  If an extension assembly contains an analyzer, we will re-use the same extension load context to load the analyzer.
/// </summary>
internal sealed class ExtensionAssemblyManager
{
    private readonly ImmutableDictionary<string, AssemblyLoadContext> _directoryLoadContexts;

    private readonly ImmutableDictionary<string, AssemblyLoadContext> _assemblyFullNameToLoadContext;

    public ImmutableArray<string> ExtensionAssemblyPaths { get; }

    public ExtensionAssemblyManager(ImmutableDictionary<string, AssemblyLoadContext> directoryLoadContexts,
        ImmutableDictionary<string, AssemblyLoadContext> assemblyFullNameToLoadContext,
        ImmutableArray<string> extensionAssemblyPaths)
    {
        _directoryLoadContexts = directoryLoadContexts;
        _assemblyFullNameToLoadContext = assemblyFullNameToLoadContext;
        ExtensionAssemblyPaths = extensionAssemblyPaths;
    }

    public static ExtensionAssemblyManager Create(ServerConfiguration serverConfiguration, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<ExportProviderBuilder>();
        if (serverConfiguration.DevKitDependencyPath is not null)
        {
            // Ensure the Roslyn DevKit assemblies are loaded into the default ALC so that extensions can use them.
            ResolveDevKitAssemblies(serverConfiguration.DevKitDependencyPath, loggerFactory);
        }

        var directoryLoadContexts = new Dictionary<string, AssemblyLoadContext>(StringComparer.Ordinal);
        var assemblyFullNameToLoadContext = new Dictionary<string, AssemblyLoadContext>(StringComparer.Ordinal);
        using var _ = ArrayBuilder<string>.GetInstance(out var validExtensionAssemblies);

        if (serverConfiguration.StarredCompletionsPath is not null)
        {
            // HACK: Load the intellicode dll as an extension, but importantly do not add it to the valid extension assemblies set.
            // While we do want to load it into its own ALC because it comes from a different ship vehicle, we do not want it
            // to contribute to the MEF catalog / analyzers as a 'normal' extension would.  Instead it gets reflection loaded elsewhere.
            //
            // We should migrate the intellicode completion provider to be a normal extension component with MEF provided parts,
            // but it requires changes to the intellicode vscode extension and here to access our IServiceBroker instance via MEF.
            var starredCompletionsComponentDll = StarredCompletionAssemblyHelper.GetStarredCompletionAssemblyPath(serverConfiguration.StarredCompletionsPath);
            Contract.ThrowIfFalse(TryGetOrCreateLoadContext(starredCompletionsComponentDll));
        }

        foreach (var assemblyFilePath in serverConfiguration.ExtensionAssemblyPaths)
        {
            if (TryGetOrCreateLoadContext(assemblyFilePath))
            {
                validExtensionAssemblies.Add(assemblyFilePath);
            }
        }

        return new ExtensionAssemblyManager(directoryLoadContexts.ToImmutableDictionary(), assemblyFullNameToLoadContext.ToImmutableDictionary(), validExtensionAssemblies.ToImmutable());

        bool TryGetOrCreateLoadContext(string assemblyFilePath)
        {
            // Verify that the path is something we can load.
            // If it's not, log helpful error messages and no-op.  We do not want to take down the server if an extension fails to load.
            if (!File.Exists(assemblyFilePath))
            {
                logger.LogError("Extension path {assemblyFilePath} does not exist", assemblyFilePath);
                return false;
            }

            var directory = Path.GetDirectoryName(assemblyFilePath);
            if (directory == null)
            {
                logger.LogError("Failed to get directory from {assemblyFilePath}", assemblyFilePath);
                return false;
            }

            var assemblyFullName = AssemblyName.GetAssemblyName(assemblyFilePath).FullName;
            if (assemblyFullNameToLoadContext.TryGetValue(assemblyFullName, out var existingContext))
            {
                // MEF relies on type full names (*without* their assembly names) to identify parts.
                // If an extension assembly is added to the catalog twice, then we will almost certainly get duplicate MEF parts
                // which breaks consumers who are only expecting one part.
                //
                // We validate this constraint here by checking for duplicate extension assembly full names.
                logger.LogError(
                    "{assemblyFilePath} with assembly name {assemblyName} conflicts with extension loaded from {existingContextName}",
                    assemblyFilePath, assemblyFullName, existingContext.Name);
                return false;
            }

            if (!directoryLoadContexts.TryGetValue(directory, out var directoryContext))
            {
                directoryContext = GetOrCreateDirectoryContext(directory, assemblyFilePath);
            }

            assemblyFullNameToLoadContext.Add(assemblyFullName, directoryContext);
            return true;
        }

        AssemblyLoadContext GetOrCreateDirectoryContext(string directory, string assemblyFilePath)
        {
            if (directoryLoadContexts.TryGetValue(directory, out var directoryContext))
            {
                logger.LogTrace("Reusing {contextName} load context for {assemblyFilePath}", directoryContext.Name, assemblyFilePath);
                return directoryContext;
            }

            // Create an extension assembly load context for the directory that the extension is in.
            logger.LogTrace("Creating {contextName} load context for {assemblyFilePath}", directory, assemblyFilePath);
            var loadContext = new ExtensionAssemblyLoadContext(directory, loggerFactory);
            directoryLoadContexts.Add(directory, loadContext);
            return loadContext;
        }
    }

    private static void ResolveDevKitAssemblies(string devKitDependencyPath, ILoggerFactory loggerFactory)
    {
        var devKitDependencyDirectory = Path.GetDirectoryName(devKitDependencyPath);
        Contract.ThrowIfNull(devKitDependencyDirectory);
        var logger = loggerFactory.CreateLogger("DevKitAssemblyResolver");

        AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
        {
            var simpleName = assemblyName.Name;
            var assemblyPath = Path.Combine(devKitDependencyDirectory, simpleName + ".dll");
            if (File.Exists(assemblyPath))
            {
                logger.LogTrace("Loading {assembly} from DevKit directory", simpleName);
                return context.LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        };
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
    /// This will look for an extension assembly with the same assembly full name.
    /// </summary>
    public Assembly? TryLoadAssemblyInExtensionContext(AssemblyName assemblyName)
    {
        var assemblyFullName = assemblyName.FullName;
        if (_assemblyFullNameToLoadContext.TryGetValue(assemblyFullName, out var loadContext))
        {
            return loadContext.LoadFromAssemblyName(assemblyName);
        }

        return null;
    }

    /// <summary>
    /// Load context that will search the extension directory for the assembly to load.
    /// If the assembly is not found in the extension context it will continue with
    /// normal assembly loading to check the host (or potentially other extensions) for the assembly.
    /// </summary>
    private sealed class ExtensionAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _extensionDirectory;
        private readonly ILogger _logger;

        public ExtensionAssemblyLoadContext(string extensionDirectory, ILoggerFactory loggerFactory) : base(extensionDirectory)
        {
            _extensionDirectory = extensionDirectory;
            _logger = loggerFactory.CreateLogger($"ALC-{extensionDirectory}");
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var simpleName = assemblyName.Name;
            var assemblyPath = Path.Combine(_extensionDirectory, simpleName + ".dll");
            if (File.Exists(assemblyPath))
            {
                _logger.LogTrace("Loading {assembly} in this load context", simpleName);
                return LoadFromAssemblyPath(assemblyPath);
            }

            // This assembly isn't provided by this extension, continue with normal assembly loading
            // to check other extensions or the host for this assembly.
            _logger.LogTrace("{assembly} not found in this load context", simpleName);
            return null;
        }
    }
}
