// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.Internal.EmbeddedLanguages;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteExportProviderBuilder : ExportProviderBuilder
{
    internal static readonly ImmutableArray<Assembly> RemoteHostAssemblies =
        MefHostServices.DefaultAssemblies
            .Add(typeof(AspNetCoreEmbeddedLanguageClassifier).Assembly)     // Microsoft.CodeAnalysis.ExternalAccess.AspNetCore
            .Add(typeof(BrokeredServiceBase).Assembly)                      // Microsoft.CodeAnalysis.Remote.ServiceHub
            .Add(typeof(IRazorLanguageServerTarget).Assembly)               // Microsoft.CodeAnalysis.ExternalAccess.Razor
            .Add(typeof(RemoteWorkspacesResources).Assembly)                // Microsoft.CodeAnalysis.Remote.Workspaces
            .Add(typeof(IExtensionWorkspaceMessageHandler<,>).Assembly);    // Microsoft.CodeAnalysis.ExternalAccess.Extensions

    private static ExportProvider? s_instance;
    internal static ExportProvider ExportProvider
        => s_instance ?? throw new InvalidOperationException($"Default export provider not initialized. Call {nameof(InitializeAsync)} first.");

    private StringBuilder? _errorMessages;

    private RemoteExportProviderBuilder(
        ImmutableArray<string> assemblyPaths,
        Resolver resolver,
        string cacheDirectory,
        string catalogPrefix)
        : base(assemblyPaths, resolver, cacheDirectory, catalogPrefix)
    {
    }

    public static async Task<string?> InitializeAsync(string localSettingsDirectory, CancellationToken cancellationToken)
    {
        var builder = new RemoteExportProviderBuilder(
            assemblyPaths: RemoteHostAssemblies.SelectAsArray(static a => a.Location),
            resolver: new Resolver(SimpleAssemblyLoader.Instance),
            cacheDirectory: Path.Combine(localSettingsDirectory, "Roslyn", "RemoteHost", "Cache"),
            catalogPrefix: "RoslynRemoteHost");

        s_instance = await builder.CreateExportProviderAsync(cancellationToken).ConfigureAwait(false);

        return builder._errorMessages?.ToString();
    }

    protected override void LogError(string message)
    {
        _errorMessages ??= new StringBuilder();
        _errorMessages.AppendLine(message);
    }

    protected override void LogTrace(string message)
    {
    }

    protected override bool ContainsUnexpectedErrors(IEnumerable<string> erroredParts, ImmutableList<PartDiscoveryException> partDiscoveryExceptions)
    {
        // Verify that we have exactly the MEF errors that we expect.  If we have less or more this needs to be updated to assert the expected behavior.
        var expectedErrorPartsSet = new HashSet<string>(["PythiaSignatureHelpProvider", "VSTypeScriptAnalyzerService", "RazorTestLanguageServerFactory", "CodeFixService", "RazorDynamicFileInfoProviderWrapper"]);
        var hasUnexpectedErroredParts = erroredParts.Any(part => !expectedErrorPartsSet.Contains(part));

        if (hasUnexpectedErroredParts)
            return true;

        return partDiscoveryExceptions.Any(partDiscoveryException => !IsKnownPartDiscoveryException(partDiscoveryException));
    }

    private static bool IsKnownPartDiscoveryException(PartDiscoveryException partDiscoveryException)
    {
        // Razor EA assembly has types that reference Microsoft.VisualStudio.LanguageServer.Client, which is not loadable OOP
        if (partDiscoveryException.AssemblyPath == typeof(IRazorLanguageServerTarget).Assembly.Location
            && partDiscoveryException.InnerException is ReflectionTypeLoadException reflectionTypeLoadException
            && reflectionTypeLoadException.LoaderExceptions.Length == 1
            && reflectionTypeLoadException.LoaderExceptions[0] is FileNotFoundException fileNotFoundException
            && fileNotFoundException.FileName is string fileNameNotFound
            && fileNameNotFound.StartsWith("Microsoft.VisualStudio.LanguageServer.Client,"))
        {
            return true;
        }

        return false;
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
