// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.Internal.EmbeddedLanguages;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Remote;

internal static class RemoteExportProvider
{
    internal static readonly ImmutableArray<Assembly> RemoteHostAssemblies =
        MefHostServices.DefaultAssemblies
            .Add(typeof(AspNetCoreEmbeddedLanguageClassifier).Assembly)
            .Add(typeof(BrokeredServiceBase).Assembly)
            .Add(typeof(IRazorLanguageServerTarget).Assembly)
            .Add(typeof(RemoteWorkspacesResources).Assembly)
            .Add(typeof(IExtensionWorkspaceMessageHandler<,>).Assembly);

    private static ExportProvider? s_instance;
    internal static ExportProvider ExportProvider
    {
        get
        {
            Contract.ThrowIfNull(s_instance, "Default export provider not initialized. Call InitializeAsync first.");
            return s_instance;
        }
    }

    public static async Task InitializeAsync(string localSettingsDirectory, CancellationToken cancellationToken)
    {
        var args = new ExportProviderBuilder.ExportProviderCreationArguments(
            AssemblyPaths: RemoteHostAssemblies.SelectAsArray(static a => a.Location),
            Resolver: new Resolver(SimpleAssemblyLoader.Instance),
            CacheDirectory: Path.Combine(localSettingsDirectory, "Roslyn", "RemoteHost", "Cache"),
            CatalogPrefix: "RoslynRemoteHost",
            ExpectedErrorParts: ["PythiaSignatureHelpProvider", "VSTypeScriptAnalyzerService", "RazorTestLanguageServerFactory"],
            PerformCleanup: true,
            LogError: static _ => { },
            LogTrace: static _ => { });

        s_instance = await ExportProviderBuilder.CreateExportProviderAsync(args, cancellationToken).ConfigureAwait(false);
    }

    private sealed class SimpleAssemblyLoader : IAssemblyLoader
    {
        public static readonly IAssemblyLoader Instance = new SimpleAssemblyLoader();

        public Assembly LoadAssembly(AssemblyName assemblyName)
            => Assembly.Load(assemblyName);

        public Assembly LoadAssembly(string assemblyFullName, string? codeBasePath)
        {
            var assemblyName = new AssemblyName(assemblyFullName);
            if (!string.IsNullOrEmpty(codeBasePath))
            {
#pragma warning disable SYSLIB0044 // https://github.com/dotnet/roslyn/issues/71510
                assemblyName.CodeBase = codeBasePath;
#pragma warning restore SYSLIB0044
            }

            return LoadAssembly(assemblyName);
        }
    }
}
