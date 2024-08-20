// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Reflection;
using System.Runtime.Loader;
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

    protected override IAnalyzerAssemblyLoader CreateShadowCopyLoader(
        AssemblyLoadContext? loadContext)
    {
        var baseLoader = base.CreateShadowCopyLoader(loadContext);
        return new VSCodeExtensionAssemblyAnalyzerLoader(baseLoader, _extensionAssemblyManager, _loggerFactory.CreateLogger<VSCodeExtensionAssemblyAnalyzerLoader>());
    }

    /// <summary>
    /// Analyzer loader that will re-use already loaded assemblies from the extension load context.
    /// </summary>
    private class VSCodeExtensionAssemblyAnalyzerLoader(IAnalyzerAssemblyLoader defaultLoader, ExtensionAssemblyManager extensionAssemblyManager, ILogger logger) : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath)
        {
            defaultLoader.AddDependencyLocation(fullPath);
        }

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
    }
}
