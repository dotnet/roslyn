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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteExportProviderBuilder : ExportProviderBuilder
{
    internal static readonly ImmutableArray<string> RemoteHostAssemblyNames =
        MefHostServices.DefaultAssemblyNames
            .Add("Microsoft.CodeAnalysis.ExternalAccess.AspNetCore")
            .Add("Microsoft.CodeAnalysis.Remote.ServiceHub")
            .Add("Microsoft.CodeAnalysis.ExternalAccess.Razor.Features")
            .Add("Microsoft.CodeAnalysis.Remote.Workspaces")
            .Add("Microsoft.CodeAnalysis.ExternalAccess.Extensions");

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

    public static async Task<string?> InitializeAsync(string localSettingsDirectory, ImmutableArray<string> oopMefComponentPaths, CancellationToken cancellationToken)
    {
        var assemblyPaths = RemoteHostAssemblyNames
            .Select(static assemblyName => MefHostServicesHelpers.TryFindNearbyAssemblyLocation(assemblyName))
            .WhereNotNull()
            .AsImmutable();

        var builder = new RemoteExportProviderBuilder(
            assemblyPaths: [.. assemblyPaths, .. oopMefComponentPaths],
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
        var expectedErrorPartsSet = new HashSet<string>(["PythiaSignatureHelpProvider", "VSTypeScriptAnalyzerService", "CodeFixService"]);
        var hasUnexpectedErroredParts = erroredParts.Any(part => !expectedErrorPartsSet.Contains(part));

        if (hasUnexpectedErroredParts)
            return true;

        return partDiscoveryExceptions.Count > 0;
    }

    private sealed class SimpleAssemblyLoader : IAssemblyLoader
    {
        public static readonly IAssemblyLoader Instance = new SimpleAssemblyLoader();

        public Assembly LoadAssembly(AssemblyName assemblyName)
            => Assembly.Load(assemblyName);

        public Assembly LoadAssembly(string assemblyFullName, string? codeBasePath)
        {
            var assemblyName = new AssemblyName(assemblyFullName);
            try
            {
                // Attempt to load the assembly by its name.
                return LoadAssembly(assemblyName);
            }
#if NET
            catch when (codeBasePath is not null)
            {
                // If that fails, try and load it from the codeBasePath if present
                var selfAlc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!;
                return selfAlc.LoadFromAssemblyPath(codeBasePath);
            }
#endif
            finally { }
        }
    }
}
