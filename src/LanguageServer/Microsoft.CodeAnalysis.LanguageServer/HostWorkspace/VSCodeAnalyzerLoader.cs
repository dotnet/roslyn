// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider), [WorkspaceKind.Host]), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSCodeAnalyzerLoaderProvider(
    ExtensionAssemblyManager extensionAssemblyManager,
    ILoggerFactory loggerFactory,
    [ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    : AbstractAnalyzerAssemblyLoaderProvider(externalResolvers.ToImmutableArray())
{
    private readonly ExtensionAssemblyManager _extensionAssemblyManager = extensionAssemblyManager;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    protected override IAnalyzerAssemblyLoaderInternal CreateShadowCopyLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
    {
        var baseLoader = base.CreateShadowCopyLoader(externalResolvers);
        return new VSCodeExtensionAssemblyAnalyzerLoader(baseLoader, _extensionAssemblyManager, _loggerFactory.CreateLogger<VSCodeExtensionAssemblyAnalyzerLoader>());
    }

    /// <summary>
    /// Analyzer loader that will re-use already loaded assemblies from the extension load context.
    /// </summary>
    private sealed class VSCodeExtensionAssemblyAnalyzerLoader(
        IAnalyzerAssemblyLoaderInternal defaultLoader,
        ExtensionAssemblyManager extensionAssemblyManager,
        ILogger logger) : IAnalyzerAssemblyLoaderInternal
    {
        public void AddDependencyLocation(string fullPath)
            => defaultLoader.AddDependencyLocation(fullPath);

        public string? GetOriginalDependencyLocation(AssemblyName assembly)
            => defaultLoader.GetOriginalDependencyLocation(assembly);

        public bool IsHostAssembly(Assembly assembly)
            => defaultLoader.IsHostAssembly(assembly);

        public Assembly LoadFromPath(string fullPath)
        {
            var assembly = extensionAssemblyManager.TryLoadAssemblyInExtensionContext(fullPath);
            if (assembly is not null)
            {
                logger.LogTrace("Loaded analyzer {fullPath} from extension context", fullPath);
                return assembly;
            }

            return defaultLoader.LoadFromPath(fullPath);
        }

        public void Dispose()
            => defaultLoader.Dispose();
    }
}
