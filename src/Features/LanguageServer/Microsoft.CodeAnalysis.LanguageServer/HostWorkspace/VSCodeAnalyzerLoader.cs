// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(VSCodeAnalyzerLoader)), Shared]
internal class VSCodeAnalyzerLoader
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSCodeAnalyzerLoader()
    {
    }

#pragma warning disable CA1822 // Mark members as static
    public void InitializeDiagnosticsServices()
#pragma warning restore CA1822 // Mark members as static
    {
    }

    public static IAnalyzerAssemblyLoader CreateAnalyzerAssemblyLoader(ExtensionAssemblyManager extensionAssemblyManager, ILogger logger)
    {
        return new VSCodeExtensionAssemblyAnalyzerLoader(extensionAssemblyManager, logger);
    }

    /// <summary>
    /// Analyzer loader that will re-use already loaded assemblies from the extension load context.
    /// </summary>
    private class VSCodeExtensionAssemblyAnalyzerLoader(ExtensionAssemblyManager extensionAssemblyManager, ILogger logger) : IAnalyzerAssemblyLoader
    {
        private readonly DefaultAnalyzerAssemblyLoader _defaultLoader = new();

        public void AddDependencyLocation(string fullPath)
        {
            _defaultLoader.AddDependencyLocation(fullPath);
        }

        public Assembly LoadFromPath(string fullPath)
        {
            var assembly = extensionAssemblyManager.TryLoadAssemblyInExtensionContext(fullPath);
            if (assembly is not null)
            {
                logger.LogTrace("Loaded analyzer {fullPath} from extension context", fullPath);
                return assembly;
            }

            return _defaultLoader.LoadFromPath(fullPath);
        }
    }
}
