// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Extensions;

internal interface IExtensionAssemblyLoaderProvider : IWorkspaceService
{
    (IExtensionAssemblyLoader? assemblyLoader, Exception? extensionException) CreateNewShadowCopyLoader(
        string assemblyFolderPath, CancellationToken cancellationToken);
}

internal interface IExtensionAssemblyLoader
{
    void Unload();
}

[ExportWorkspaceServiceFactory(typeof(IExtensionAssemblyLoaderProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultExtensionAssemblyLoaderProviderFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new DefaultExtensionAssemblyLoaderProvider(workspaceServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>());

    private sealed class DefaultExtensionAssemblyLoaderProvider(IAnalyzerAssemblyLoaderProvider assemblyLoaderProvider)
        : IExtensionAssemblyLoaderProvider
    {
        public (IExtensionAssemblyLoader? assemblyLoader, Exception? extensionException) CreateNewShadowCopyLoader(
            string assemblyFolderPath, CancellationToken cancellationToken)
        {
#if NET
            var shadowCopyLoader = assemblyLoaderProvider.CreateNewShadowCopyLoader();
            try
            {
                // Allow this assembly loader to load any dll in assemblyFolderPath.
                foreach (var dll in Directory.EnumerateFiles(assemblyFolderPath, "*.dll"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        // Check if the file is a valid .NET assembly.
                        AssemblyName.GetAssemblyName(dll);
                    }
                    catch
                    {
                        // The file is not a valid .NET assembly, skip it.
                        continue;
                    }

                    shadowCopyLoader.AddDependencyLocation(dll);
                }

                return (new DefaultExtensionAssemblyLoader(shadowCopyLoader), null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return (null, ex);
            }

#else
            return default;
#endif
        }
    }

#if false


#if NET
                    var analyzerAssemblyLoader = analyzerAssemblyLoaderProvider.CreateNewShadowCopyLoader();



                    return (IAnalyzerAssemblyLoaderInternal?)analyzerAssemblyLoader;
#else
                    return (IAnalyzerAssemblyLoaderInternal?)null;
#endif
#endif

    private sealed class DefaultExtensionAssemblyLoader(
        IAnalyzerAssemblyLoaderInternal assemblyLoader) : IExtensionAssemblyLoader
    {
        public void Unload() => assemblyLoader.Dispose();
    }
}
