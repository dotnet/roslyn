// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
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
    Assembly LoadFromPath(string assemblyFilePath);
    void Unload();
}

[ExportWorkspaceServiceFactory(typeof(IExtensionAssemblyLoaderProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultExtensionAssemblyLoaderProviderFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new DefaultExtensionAssemblyLoaderProvider(workspaceServices);

    private sealed class DefaultExtensionAssemblyLoaderProvider(HostWorkspaceServices workspaceServices)
        : IExtensionAssemblyLoaderProvider
    {
        private readonly HostWorkspaceServices _workspaceServices = workspaceServices;

        public (IExtensionAssemblyLoader? assemblyLoader, Exception? extensionException) CreateNewShadowCopyLoader(
            string assemblyFolderPath, CancellationToken cancellationToken)
        {
#if NET
            // These lines should always succeed.  If they don't, they indicate a bug in our code that we want
            // to bubble out as it must be fixed.
            var analyzerAssemblyLoaderProvider = _workspaceServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();
            var analyzerAssemblyLoader = analyzerAssemblyLoaderProvider.CreateNewShadowCopyLoader();

            // Catch exceptions here related to working with the file system.  If we can't properly enumerate,
            // we want to report that back to the client, while not blocking the entire extension service.
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

                    analyzerAssemblyLoader.AddDependencyLocation(dll);
                }

                return (new DefaultExtensionAssemblyLoader(analyzerAssemblyLoader), null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Capture any exceptions here to be reported back in CreateAssemblyHandlersAsync.
                return (null, ex);
            }
#else
            return default;
#endif
        }
    }

    private sealed class DefaultExtensionAssemblyLoader(
        IAnalyzerAssemblyLoaderInternal assemblyLoader) : IExtensionAssemblyLoader
    {
        public void Unload() => assemblyLoader.Dispose();
    }
}
