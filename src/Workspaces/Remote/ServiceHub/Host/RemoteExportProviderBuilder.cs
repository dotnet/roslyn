// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

internal sealed class RemoteExportProviderBuilder : ExportProviderBuilder
{
    internal static readonly ImmutableArray<Assembly> RemoteHostAssemblies =
        MefHostServices.DefaultAssemblies
            .Add(typeof(AspNetCoreEmbeddedLanguageClassifier).Assembly)     // Microsoft.CodeAnalysis.ExternalAccess.AspNetCore
            .Add(typeof(BrokeredServiceBase).Assembly)                      // Microsoft.CodeAnalysis.Remote.ServiceHub
            .Add(typeof(IRazorLanguageServerTarget).Assembly)               // Microsoft.CodeAnalysis.ExternalAccess.Razor.Features
            .Add(typeof(RemoteWorkspacesResources).Assembly)                // Microsoft.CodeAnalysis.Remote.Workspaces
            .Add(typeof(IExtensionWorkspaceMessageHandler<,>).Assembly);    // Microsoft.CodeAnalysis.ExternalAccess.Extensions

    private static ExportProvider? s_instance;
    internal static ExportProvider ExportProvider
        => s_instance ?? throw new InvalidOperationException("Default export provider not initialized. Call InitializeAsync first.");

    private RemoteExportProviderBuilder(
        ImmutableArray<string> assemblyPaths,
        Resolver resolver,
        string cacheDirectory,
        string catalogPrefix,
        ImmutableArray<string> expectedErrorParts)
        : base(assemblyPaths, resolver, cacheDirectory, catalogPrefix, expectedErrorParts)
    {
    }

    public static async Task InitializeAsync(string localSettingsDirectory, CancellationToken cancellationToken)
    {
        var builder = new RemoteExportProviderBuilder(
            assemblyPaths: RemoteHostAssemblies.SelectAsArray(static a => a.Location),
            resolver: new Resolver(SimpleAssemblyLoader.Instance),
            cacheDirectory: Path.Combine(localSettingsDirectory, "Roslyn", "RemoteHost", "Cache"),
            catalogPrefix: "RoslynRemoteHost",
            expectedErrorParts: ["PythiaSignatureHelpProvider", "VSTypeScriptAnalyzerService", "RazorTestLanguageServerFactory", "CodeFixService"]);

        s_instance = await builder.CreateExportProviderAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void LogError(string message)
    {
    }

    protected override void LogTrace(string message)
    {
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
                // Set the codebase path, if known, as a hint for the assembly loader.
#pragma warning disable SYSLIB0044 // https://github.com/dotnet/roslyn/issues/71510
                assemblyName.CodeBase = codeBasePath;
#pragma warning restore SYSLIB0044
            }

            return LoadAssembly(assemblyName);
        }
    }
}
